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

The `$id` values of the examples are identifiers only; nothing is published at them.

Each target chapter lists its examples, their sample data and its invalid examples:
[registry](../targets/registry.md#8-examples), [ini-windows](../targets/ini-windows.md#7-examples),
[xml](../targets/xml.md#9-examples), [json](../targets/json.md#6-examples),
[yaml-1.2](../targets/yaml-1.2.md#5-examples), [toml](../targets/toml.md#5-examples),
[sqlite](../targets/sqlite.md#6-examples), [csv](../targets/csv.md#7-examples),
[psd1](../targets/psd1.md#6-examples).
