// Completion scenarios for completion-test.js: "|" marks the cursor; expect = keys that must be offered,
// forbid = keys that must not be offered.
const head = target => `$schema: https://skemata.github.io/skemata/draft-01/schema\n$target: ${target}\n`;
const stringKeys = ['minLength', 'maxLength', 'pattern', 'format'];
const numberKeys = ['minimum', 'maximum', 'multipleOf'];
const objectKeys = ['properties', 'required', 'additionalProperties'];
const arrayKeys = ['items', 'minItems', 'maxItems'];

module.exports = [
  {
    name: 'root before $target: only $schema/$target',
    yaml: `$schema: https://skemata.github.io/skemata/draft-01/schema\n|`,
    expect: ['$target'], forbid: [...objectKeys, ...arrayKeys],
  },
  {
    name: 'json root (object or array): structure keys, no scalar keys',
    yaml: head('json') + '|',
    expect: ['properties', 'items', 'title', 'type'], forbid: [...stringKeys, ...numberKeys, 'const', 'enum'],
  },
  {
    name: 'csv root (array of records): array keys only',
    yaml: head('csv') + '|',
    expect: ['items', 'identity', 'minItems'], forbid: [...objectKeys, ...stringKeys, ...numberKeys],
  },
  {
    name: 'json member after type: string -> string keys only, const and default offered',
    yaml: head('json') + `properties:\n  a:\n    type: string\n    |`,
    expect: [...stringKeys, 'const', 'enum', 'default', 'oneOf', 'contentEncoding'], forbid: [...numberKeys, ...objectKeys, ...arrayKeys, 'unit'],
  },
  {
    name: 'json member untyped with pattern -> string keys only',
    yaml: head('json') + `properties:\n  a:\n    pattern: "^x"\n    |`,
    expect: ['minLength', 'maxLength', 'format'], forbid: [...numberKeys, ...objectKeys, ...arrayKeys],
  },
  {
    name: 'json member after type: integer -> number keys only',
    yaml: head('json') + `properties:\n  a:\n    type: integer\n    |`,
    expect: [...numberKeys, 'format', 'unit', 'const'], forbid: ['pattern', 'minLength', ...objectKeys, ...arrayKeys],
  },
  {
    name: 'json member after type: object -> object keys only',
    yaml: head('json') + `properties:\n  a:\n    type: object\n    |`,
    expect: objectKeys, forbid: [...stringKeys, ...numberKeys, ...arrayKeys, 'const', 'enum'],
  },
  {
    name: 'json member after type: array -> array keys only',
    yaml: head('json') + `properties:\n  a:\n    type: array\n    |`,
    expect: [...arrayKeys, 'uniqueItems', 'prefixItems'], forbid: [...stringKeys, ...numberKeys, ...objectKeys, 'const'],
  },
  {
    name: 'csv field after type: integer -> number keys, nothing structural',
    yaml: head('csv') + `items:\n  properties:\n    quota:\n      type: integer\n      |`,
    expect: [...numberKeys, 'format', 'unit'], forbid: ['pattern', ...objectKeys, ...arrayKeys],
  },
  {
    name: 'registry value after type: integer -> number keys',
    yaml: head('registry') + `properties:\n  Size:\n    type: integer\n    |`,
    expect: [...numberKeys, 'format'], forbid: ['pattern', ...objectKeys, 'items'],
  },
  {
    name: 'labeled-enum value choice after const -> only annotations',
    yaml: head('json') + `properties:\n  a:\n    type: integer\n    oneOf:\n      - const: 1\n        |`,
    expect: ['title', 'description'], forbid: ['enum', 'minimum', 'maximum'],
  },
  {
    name: 'json array after prefixItems -> no uniqueItems/identity/ordered',
    yaml: head('json') + `properties:\n  p:\n    prefixItems:\n      - type: number\n    |`,
    expect: ['items', 'minItems'], forbid: ['uniqueItems', 'identity', 'ordered'],
  },
  {
    name: 'json string after contentEncoding -> no format',
    yaml: head('json') + `properties:\n  b:\n    type: string\n    contentEncoding: base64\n    |`,
    expect: ['minLength'], forbid: ['format'],
  },
  {
    name: 'json new member, nothing written yet -> every key of the position',
    yaml: head('json') + `properties:\n  a:\n    title: A\n    |`,
    expect: [...stringKeys, ...numberKeys, ...objectKeys, ...arrayKeys, 'type', 'const', '$ref'], forbid: [],
  },
  {
    name: 'json string after format -> no contentEncoding',
    yaml: head('json') + `properties:\n  a:\n    type: string\n    format: uri\n    |`,
    expect: ['pattern', 'minLength'], forbid: ['contentEncoding', ...numberKeys],
  },
  {
    name: 'labeled enum (oneOf) next to type: string -> no enum/const',
    yaml: head('json') + `properties:\n  a:\n    type: string\n    oneOf:\n      - const: x\n    |`,
    expect: ['format', 'pattern', 'default'], forbid: ['enum', 'const', ...numberKeys],
  },
  {
    name: 'range choice after minimum -> no const/enum/string keys',
    yaml: head('json') + `properties:\n  a:\n    type: integer\n    oneOf:\n      - const: 0\n      - minimum: 1\n        |`,
    expect: ['maximum', 'title'], forbid: ['const', 'enum', 'pattern', 'minLength'],
  },
  {
    name: 'string choice after const -> only annotations',
    yaml: head('yaml-1.2') + `properties:\n  a:\n    type: string\n    oneOf:\n      - const: x\n        |`,
    expect: ['title', 'deprecated'], forbid: ['enum', 'pattern', 'minimum'],
  },
  {
    name: 'union branch -> object keys only',
    yaml: head('json') + `properties:\n  u:\n    type: object\n    oneOf:\n      - properties:\n          k: { const: a }\n        |`,
    expect: ['required', 'additionalProperties'], forbid: ['const', 'enum', 'items', 'minimum', 'oneOf'],
  },
  {
    name: 'sqlite column (scalar) -> no structure keys, no required',
    yaml: head('sqlite') + `properties:\n  t:\n    identity: [k]\n    items:\n      properties:\n        k:\n          type: string\n          |`,
    expect: ['pattern', 'maxLength'], forbid: [...objectKeys, ...arrayKeys, ...numberKeys],
  },
  {
    name: 'sqlite row -> object keys without required',
    yaml: head('sqlite') + `properties:\n  t:\n    identity: [k]\n    items:\n      properties:\n        k: { type: string }\n      |`,
    expect: ['additionalProperties', 'title'], forbid: ['required', 'items', 'pattern'],
  },
  {
    name: 'ini key after type: integer -> no textual (not in ini)',
    yaml: head('ini-windows') + `properties:\n  S:\n    properties:\n      K:\n        type: integer\n        |`,
    expect: ['minimum', 'format'], forbid: ['textual', 'pattern', ...objectKeys],
  },
  {
    name: 'registry number -> textual offered',
    yaml: head('registry') + `properties:\n  Z:\n    type: number\n    |`,
    expect: ['textual', 'minimum'], forbid: ['pattern', 'items'],
  },
  {
    name: 'value completion: csv field type',
    yaml: head('csv') + `items:\n  properties:\n    a:\n      type: |`,
    expect: ['string', 'integer', 'number'], forbid: ['object', 'array', 'boolean'],
  },
  {
    name: 'value completion: csv root type',
    yaml: head('csv') + `type: |`,
    expect: ['array'], forbid: ['object', 'string'],
  },
  {
    name: 'value completion: integer format after type: integer',
    yaml: head('json') + `properties:\n  a:\n    type: integer\n    format: |`,
    expect: ['int32', 'uint16'], forbid: ['date', 'float', 'uri'],
  },
  {
    name: 'xml element with content -> no members',
    yaml: head('xml') + `properties:\n  doc:\n    properties:\n      e:\n        content:\n          type: string\n        |`,
    expect: ['attributes', 'additionalProperties'], forbid: ['properties', 'required', 'patternProperties'],
  },
];
