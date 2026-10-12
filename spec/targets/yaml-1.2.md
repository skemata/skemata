# Target `yaml-1.2`

Describes a **YAML 1.2 document** whose scalars are resolved with the YAML 1.2 **core schema**.

## 1 Data

**[YML-1]** The data of a `yaml-1.2` document is the result of loading a single YAML document with the YAML
1.2 core schema: anchors and aliases resolved, mapping keys as strings. The root schema MUST describe an
object or an array.

Under the core schema, only `true`/`false` (in any of three capitalizations) are booleans, and `yes`, `no`,
`on` and `off` are strings. Documents written for YAML 1.1 tools may rely on the old rules; they would need a
separate `yaml-1.1` target ([8.7](../08-target-profiles.md#87-candidate-targets-non-normative)).

## 2 Mapping

| Data model | YAML |
|---|---|
| object | mapping |
| array, tuple (`prefixItems`), nested arrays | sequence |
| `string` | string (`!!str`) |
| `integer` | integer (`!!int`) |
| `number` | float (`!!float`); integers are accepted, as in JSON Schema |
| `boolean` | `!!bool` |
| null (nullable types) | `!!null` |
| binary | base64 string |
| string formats | string |

`textual: true` stores an integer or number as a string in its lexical form, such as `port: "8080"`.

A type union ([KW-32](../04-keywords.md#oneof)) tells its branches apart by the core-schema type of the node,
as in `json`: `integer` and `number` MUST NOT both be branch types, and at most one branch MAY be nullable.

## 3 Names

**[YML-2]** Mapping keys are compared case-sensitively. The empty name is not allowed
([MOD-3](../03-data-model.md#332-names)). Merge keys (`<<`) have no special meaning in the YAML 1.2 core
schema; a data validator MUST treat `<<` as an ordinary key.

## 4 Representability limits

- Nodes with tags outside the core schema, such as `!!binary`, `!!timestamp` or application tags, cannot be
  described. **[YML-3]** If a declared member's node carries such a tag, a data validator MUST report the
  data as invalid.
- Non-string mapping keys and multi-document streams cannot be described.
- Comments, styles (block or flow, quoting) and anchors are syntax and outside the data model.

## 5 Examples

| Example | Sample data | Shows |
|---|---|---|
| [dependabot](../examples/yaml-1.2/dependabot.skemata.yaml) | [dependabot.yml](../examples/yaml-1.2/dependabot.yml) | a keyed list with a two-member identity, enums, nested objects |

Invalid: [DOC-3](../examples/yaml-1.2/invalid/DOC-3-unquoted-yes.skemata.yaml) unquoted `yes`/`no`,
[KW-16](../examples/yaml-1.2/invalid/KW-16-type-not-inferable.skemata.yaml) type not inferable.
