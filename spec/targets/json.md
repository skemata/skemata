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

| Example | Shows |
|---|---|
| [firefox-policies](../examples/json/firefox-policies.skemata.yaml) | nested objects, native booleans, a dynamic-key map, a labeled enum |
| [terminal-settings](../examples/json/terminal-settings.skemata.yaml) | a keyed list with GUID identity, `$ref`; sample data with comments and trailing commas |
| [invalid/](../examples/json/invalid/) | scalar root, empty member name, `attributes`, `expandable` |
