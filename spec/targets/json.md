# Target `json`

Describes a **JSON text** (RFC 8259). Comments and trailing commas, as in "JSON with Comments" (JSONC), are
accepted implicitly.

## 1 Mapping

JSON's data model is the model JSON Schema was made for, and Skemata's data model follows it closely:

| Data model | JSON |
|---|---|
| object | object |
| array, tuple (`prefixItems`), nested arrays | array |
| `string` | string |
| `integer`, `number` | number |
| `boolean` | `true` / `false` |
| null (nullable types) | `null` |
| binary (`contentEncoding: base64`) | string with base64 text |
| string formats | string |

**[JSN-1]** The root schema MUST describe an object or an array.

A typical Skemata schema for a `json` target therefore looks almost exactly like a JSON Schema for the same
data.

## 2 Comments and trailing commas

**[JSN-2]** A `json` document MAY contain comments (`// …` to the end of the line, and `/* … */`) and trailing
commas after the last member of an object or the last item of an array. Both are syntax: the data is the JSON
text without them. All other rules apply to that data.

JSON with comments does not need a target of its own, because its data model, value rules and name rules are
those of JSON ([8.2](../08-target-profiles.md#82-target-names)). Whether a tool that rewrites a file keeps
its comments is outside Skemata's scope.

## 3 Numbers

**[JSN-3]** As in JSON Schema, a JSON number with a zero fractional part (such as `1.0`) is a valid `integer`.

`textual: true` declares an integer or number that is stored as a JSON string in its lexical form
([5.8](../05-types-and-formats.md#58-lexical-forms)), such as `"port": "8080"`.

Many JSON implementations represent numbers as IEEE 754 doubles and lose precision for integers beyond
±(2⁵³−1) (see RFC 7493, I-JSON). Data that contains larger 64-bit integers often stores them textually.

## 4 Names

**[JSN-4]** Member names are compared case-sensitively. The empty name is not allowed
([MOD-3](../03-data-model.md#332-names)). If a declared member's name occurs more than once in one JSON
object, a data validator MUST report the data as ambiguous.

## 5 Representability limits

- Objects with duplicate member names (JSN-4).
- Members with the empty name `""`. They are legal JSON, but Skemata reserves the empty name for targets with
  unnamed values.
- Comments, whitespace, member order and number notation (`1e2` vs `100`) are outside the data model.

## 6 Examples

| Example | Sample data | Shows |
|---|---|---|
| [firefox-policies](../examples/json/firefox-policies.skemata.yaml) | [policies.json](../examples/json/policies.json) | native booleans, nested objects, a dynamic-key map, `patternProperties`, `dependentRequired`, a labeled enum, recursive bookmark folders |
| [terminal-settings](../examples/json/terminal-settings.skemata.yaml) | [terminal-settings.json](../examples/json/terminal-settings.json) | a keyed list with GUID identity, `$ref`; data with comments and trailing commas |
| [geojson-linestring](../examples/json/geojson-linestring.skemata.yaml) | [geojson-linestring.json](../examples/json/geojson-linestring.json) | tuples inside a nested array (not settings data) |

Invalid: [KW-31](../examples/json/invalid/KW-31-declared-name-matches-pattern.skemata.yaml) declared name matches a pattern,
[JSN-1](../examples/json/invalid/JSN-1-scalar-root.skemata.yaml) scalar root,
[MOD-3](../examples/json/invalid/MOD-3-empty-name.skemata.yaml) empty name,
[KW-2](../examples/json/invalid/KW-2-attributes.skemata.yaml) `attributes`,
[KW-2](../examples/json/invalid/KW-2-expandable.skemata.yaml) `expandable`,
[KW-6](../examples/json/invalid/KW-6-endless-recursion.skemata.yaml) endless recursion,
[KW-29](../examples/json/invalid/KW-29-undeclared-dependency.skemata.yaml) undeclared dependency.
