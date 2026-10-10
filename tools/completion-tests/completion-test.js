// Checks which keys an editor offers with the generated meta-schemas: talks to the YAML language server that
// the Red Hat YAML extension for VS Code uses, requests completions in the documents of scenarios.js at the
// position marked "|", and checks the keys that must be offered ("expect") and those that must not ("forbid").
//
//   npm install && node completion-test.js                     the server from npm (package.json pins it)
//   node completion-test.js <server.js> [schema] [scenarios]   another server, for example the one in VS Code:
//                                                              <extension>/dist/languageserver.js
// Run `generate` first; the default schema is schemas/draft-01/schema.bundled.json.
const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
const { pathToFileURL } = require('url');

const [serverPath = path.join(__dirname, 'node_modules/yaml-language-server/out/server/src/server.js'),
  schemaPath = path.join(__dirname, '../../schemas/draft-01/schema.bundled.json'),
  scenarioFile] = process.argv.slice(2);
// The server bundled in VS Code keeps its texts next to it; the npm package finds its own.
const l10nPath = path.join(path.dirname(serverPath), 'l10n');
const scenarios = require(path.resolve(scenarioFile || path.join(__dirname, 'scenarios.js')));
const schemaUrl = pathToFileURL(path.resolve(schemaPath)).href;

const server = spawn(process.execPath, [serverPath, '--stdio'], { env: { ...process.env, ELECTRON_RUN_AS_NODE: '1' } });
let buffer = Buffer.alloc(0);
let nextId = 1;
const pending = new Map();
const settings = { yaml: { validate: true, completion: true, hover: true, schemaStore: { enable: false }, format: { enable: false } } };

function send(msg) {
  const body = Buffer.from(JSON.stringify({ jsonrpc: '2.0', ...msg }), 'utf8');
  server.stdin.write(`Content-Length: ${body.length}\r\n\r\n`);
  server.stdin.write(body);
}
function request(method, params) {
  const id = nextId++;
  send({ id, method, params });
  return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
}
function notify(method, params) { send({ method, params }); }

server.stdout.on('data', chunk => {
  buffer = Buffer.concat([buffer, chunk]);
  for (;;) {
    const header = buffer.indexOf('\r\n\r\n');
    if (header < 0) return;
    const length = Number(/Content-Length: (\d+)/i.exec(buffer.slice(0, header).toString())[1]);
    if (buffer.length < header + 4 + length) return;
    const msg = JSON.parse(buffer.slice(header + 4, header + 4 + length).toString('utf8'));
    buffer = buffer.slice(header + 4 + length);
    if (msg.id !== undefined && msg.method === undefined) {
      const p = pending.get(msg.id); pending.delete(msg.id);
      msg.error ? p.reject(new Error(JSON.stringify(msg.error))) : p.resolve(msg.result);
    } else if (msg.id !== undefined) {
      // server -> client request
      const result = msg.method === 'workspace/configuration' ? msg.params.items.map(i => i.section === 'yaml' ? settings.yaml : i.section ? undefined : settings) : null;
      send({ id: msg.id, result });
    }
  }
});
server.stderr.on('data', d => process.stderr.write(d));

(async () => {
  await request('initialize', {
    processId: process.pid, rootUri: null, workspaceFolders: null,
    initializationOptions: fs.existsSync(l10nPath) ? { l10nPath } : {},
    capabilities: {
      workspace: { configuration: true, didChangeConfiguration: { dynamicRegistration: true } },
      textDocument: { completion: { completionItem: { snippetSupport: true, documentationFormat: ['plaintext'] } } },
    },
  });
  notify('initialized', {});
  notify('workspace/didChangeConfiguration', { settings });

  let failures = 0, n = 0;
  for (const s of scenarios) {
    n++;
    const marked = `# yaml-language-server: $schema=${schemaUrl}\n` + s.yaml.replace(/^\n/, '');
    const offset = marked.indexOf('|');
    const text = marked.slice(0, offset) + marked.slice(offset + 1);
    const before = text.slice(0, offset).split('\n');
    const position = { line: before.length - 1, character: before[before.length - 1].length };
    const uri = pathToFileURL(path.join(__dirname, `scenario-${n}.skemata.yaml`)).href;
    notify('textDocument/didOpen', { textDocument: { uri, languageId: 'yaml', version: 1, text } });
    const result = await request('textDocument/completion', { textDocument: { uri }, position });
    const labels = new Set((Array.isArray(result) ? result : result?.items ?? []).map(i => i.label));
    const missing = (s.expect ?? []).filter(l => !labels.has(l));
    const offered = (s.forbid ?? []).filter(l => labels.has(l));
    const ok = missing.length === 0 && offered.length === 0;
    if (!ok) failures++;
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${s.name}`);
    if (!ok || process.env.SHOW_LABELS) {
      if (missing.length) console.log(`        not offered: ${missing.join(', ')}`);
      if (offered.length) console.log(`        wrongly offered: ${offered.join(', ')}`);
      console.log(`        offered: ${[...labels].sort().join(', ')}`);
    }
    notify('textDocument/didClose', { textDocument: { uri } });
  }
  console.log(`\n${n - failures}/${n} completion scenarios pass`);
  await request('shutdown', null);
  notify('exit', null);
  setTimeout(() => process.exit(failures ? 1 : 0), 200);
})().catch(e => { console.error(e); process.exit(2); });
