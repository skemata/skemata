# Worked examples

Each folder contains, for one target:

- example schemas (`*.skemata.yaml`),
- sample target data that the schemas describe (`.reg`, `.ini`, `.config`, `.json`, `.sql`, `.yml`, `.toml`,
  `.csv`, `.psd1`),
- an `invalid/` folder with schemas that each break one primary rule. Each file name starts with the ID of
  that rule, and a header comment explains it. Where the matrices of chapter 8 also forbid the construct,
  the header notes that KW-2 is violated as well.

The examples illustrate the specification. They are modeled on publicly documented data of well-known
software, or on fictitious applications. They are not authoritative descriptions of those products, and they
are not a conformance test suite.

Several examples rely on implicit rules ([4.11](../04-keywords.md#411-implicit-rules-and-the-normalized-form)):
types are inferred, titles are derived from names, and identity values are required without being listed.

| Target | Example | Sample data | Shows |
|---|---|---|---|
| registry | [edge-policies](registry/edge-policies.skemata.yaml) | [.reg](registry/edge-policies.reg) | implicit types, `uint32` and titles; a special value next to a range; a unit; groups; [German locale file](registry/edge-policies.de.skemata-locale.yaml) |
| registry | [edge-extension-install-forcelist](registry/edge-extension-install-forcelist.skemata.yaml) | [.reg](registry/edge-extension-install-forcelist.reg) | list stored as numbered values (root array) |
| registry | [windows-service](registry/windows-service.skemata.yaml) | [.reg](registry/windows-service.reg) | REG_EXPAND_SZ, REG_MULTI_SZ, REG_BINARY, `$comment` |
| registry | [file-type-association](registry/file-type-association.skemata.yaml) | [.reg](registry/file-type-association.reg) | `(Default)` value as member `""` |
| ini-windows | [desktop-ini](ini-windows/desktop-ini.skemata.yaml) | [desktop.ini](ini-windows/desktop.ini) | sections, `1`/`0` enum, undescribed keys |
| ini-windows | [example-app](ini-windows/example-app.skemata.yaml) | [example-app.ini](ini-windows/example-app.ini) | on/off value with several spellings, units, deprecated choice, dynamic keys, implicit types and titles |
| xml | [dotnet-app-config](xml/dotnet-app-config.skemata.yaml) | [app.config](xml/app.config) | keyed `<add>` lists identified by an attribute, a nested identity, an inherited namespace |
| xml | [gui-config](xml/gui-config.skemata.yaml) | [config.xml](xml/config.xml) | `content` next to `attributes`, open attribute set |
| json | [firefox-policies](json/firefox-policies.skemata.yaml) | [policies.json](json/policies.json) | booleans, nested objects, `patternProperties`, `dependentRequired`, recursive bookmark folders |
| json | [terminal-settings](json/terminal-settings.skemata.yaml) | [terminal-settings.json](json/terminal-settings.json) | keyed list with GUID identity; data with comments and trailing commas |
| json | [geojson-linestring](json/geojson-linestring.skemata.yaml) | [geojson-linestring.json](json/geojson-linestring.json) | tuples inside a nested array (not settings data) |
| yaml-1.2 | [dependabot](yaml-1.2/dependabot.skemata.yaml) | [dependabot.yml](yaml-1.2/dependabot.yml) | two-member identity |
| toml | [backup-agent](toml/backup-agent.skemata.yaml) | [.toml](toml/backup-agent.toml) | array of tables, native local date-time and time, textual date, discriminated union |
| sqlite | [app-settings-db](sqlite/app-settings-db.skemata.yaml) | [.sql](sqlite/app-settings-db.sql) | key/value table, two-column identity, nullable and textual columns |
| csv | [user-import](csv/user-import.skemata.yaml) | [user-import.csv](csv/user-import.csv) | keyed records, empty fields as absent values, spellings, a unit |
| csv-semicolon | [printer-mapping](csv/printer-mapping.skemata.yaml) | [printer-mapping.csv](csv/printer-mapping.csv) | delimiter stated by the target, two-column identity |
| psd1 | [module-manifest](psd1/module-manifest.skemata.yaml) | [ExampleModule.psd1](psd1/ExampleModule.psd1) | case-insensitive keys, nested hashtables, derived and explicit titles |

Invalid examples:

| Target | Rule violated |
|---|---|
| registry | [REG-2](registry/invalid/REG-2-nested-object.skemata.yaml) nested object, [TYP-3](registry/invalid/TYP-3-boolean.skemata.yaml) boolean, [REG-5](registry/invalid/REG-5-number-without-textual.skemata.yaml) number without `textual`, [MOD-2](registry/invalid/MOD-2-case-insensitive-clash.skemata.yaml) case-insensitive name clash, [KW-14](registry/invalid/KW-14-overlapping-choices.skemata.yaml) overlapping choices |
| ini-windows | [INI-1](ini-windows/invalid/INI-1-global-key.skemata.yaml) global key, [INI-2](ini-windows/invalid/INI-2-array-in-section.skemata.yaml) array in a section, [TYP-3](ini-windows/invalid/TYP-3-boolean.skemata.yaml) boolean, [KW-2](ini-windows/invalid/KW-2-textual.skemata.yaml) `textual` |
| xml | [KW-23](xml/invalid/KW-23-content-with-members.skemata.yaml) content next to members, [XML-1](xml/invalid/XML-1-two-document-elements.skemata.yaml) two document elements, [XML-1](xml/invalid/XML-1-pattern-at-root.skemata.yaml) a pattern for further document elements, [KW-22](xml/invalid/KW-22-array-attribute.skemata.yaml) array as attribute, [XML-3](xml/invalid/XML-3-colon-in-name.skemata.yaml) colon in a name, [KW-2](xml/invalid/KW-2-tuple.skemata.yaml) tuple |
| json | [KW-31](json/invalid/KW-31-declared-name-matches-pattern.skemata.yaml) declared name matches a pattern, [JSN-1](json/invalid/JSN-1-scalar-root.skemata.yaml) scalar root, [MOD-3](json/invalid/MOD-3-empty-name.skemata.yaml) empty name, [KW-2](json/invalid/KW-2-attributes.skemata.yaml) `attributes`, [KW-2](json/invalid/KW-2-expandable.skemata.yaml) `expandable`, [KW-6](json/invalid/KW-6-endless-recursion.skemata.yaml) endless recursion, [KW-29](json/invalid/KW-29-undeclared-dependency.skemata.yaml) undeclared dependency |
| yaml-1.2 | [DOC-3](yaml-1.2/invalid/DOC-3-unquoted-yes.skemata.yaml) unquoted `yes`/`no`, [KW-16](yaml-1.2/invalid/KW-16-type-not-inferable.skemata.yaml) type not inferable |
| toml | [TML-2](toml/invalid/TML-2-nullable.skemata.yaml) nullable type, [UI-8](toml/invalid/UI-8-unknown-unit.skemata.yaml) unknown unit |
| sqlite | [TYP-3](sqlite/invalid/TYP-3-boolean-column.skemata.yaml) boolean column, [SQL-3](sqlite/invalid/SQL-3-required-on-rows.skemata.yaml) `required` on rows, [SQL-4](sqlite/invalid/SQL-4-table-without-identity.skemata.yaml) table without identity |
| csv | [CSV-1](csv/invalid/CSV-1-nested-column.skemata.yaml) nested column |
| psd1 | [PSD-1](psd1/invalid/PSD-1-array-root.skemata.yaml) array as root |
