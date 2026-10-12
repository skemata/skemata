# Target `toml`

Describes a **TOML document**, version 1.0.0 or 1.1.0. Both versions share the same data model; 1.1.0 only
adds syntax ([8.2](../08-target-profiles.md#82-target-names)).

## 1 Mapping

| Data model | TOML |
|---|---|
| root object | the root table |
| object | table: standard (`[a]`), inline (`{ … }`) or defined by dotted keys |
| array, tuple (`prefixItems`), nested arrays | array; an array of objects may be written as an array of tables (`[[a]]`) |
| `string` | string |
| `integer` | integer (64-bit signed) |
| `number` | float; integers are accepted, as in JSON Schema |
| `boolean` | boolean |
| binary | base64 string |
| `date-time` | offset date-time |
| `date-time-local` | local date-time |
| `date` | local date |
| `time-local` | local time |
| `time`, `duration`, other string formats | string |

Which syntax a document uses for a table or array (standard or inline, `[[…]]` or inline array) is outside the
data model.

**[TML-1]** The root schema MUST describe an object.

A type union ([KW-32](../04-keywords.md#oneof)) tells its branches apart by the TOML value type: string,
integer, float, boolean, offset date-time, local date-time, local date, local time, array or table. A string
with a temporal format is a native date or time unless it is `textual`. `integer` and `number` MUST NOT both
be branch types, because a number branch accepts integers.

## 2 Values

**[TML-2]** TOML has no null and no unsigned 64-bit integers. Nullable types and `format: uint64` MUST NOT be
used (see also [8.5](../08-target-profiles.md#85-type-and-format--target-matrix)).

**[TML-3]** For the temporal formats that TOML stores natively, a data validator MUST check the TOML value
type: a string where a local date is expected is invalid, and so is a local date where a string is expected.
`textual: true` declares that the value is a TOML string in the format's syntax.

TOML 1.1.0 allows omitting the seconds of a time; the logical value then has zero seconds. The infinities and
NaN that TOML floats allow are outside the Skemata data model ([TYP-2](../05-types-and-formats.md#53-numbers)).

## 3 Names

**[TML-4]** Keys are compared case-sensitively. The empty key is not allowed
([MOD-3](../03-data-model.md#332-names)), although TOML permits it, discouraged, as a quoted key `""`.

## 4 Representability limits

- Mixed-type arrays: arrays with a fixed layout are tuples (`prefixItems`), and arrays of differently shaped
  tables can use a discriminated union. Arrays whose items vary freely in type cannot be described.
- Empty keys (TML-4), NaN and infinities.
- Comments, key order and syntax choices are outside the data model.

## 5 Examples

| Example | Sample data | Shows |
|---|---|---|
| [backup-agent](../examples/toml/backup-agent.skemata.yaml) | [.toml](../examples/toml/backup-agent.toml) | an array of tables with identity, native local date-time and local time, a textual date, a discriminated union |

Invalid: [TML-2](../examples/toml/invalid/TML-2-nullable.skemata.yaml) nullable type,
[UI-8](../examples/toml/invalid/UI-8-unknown-unit.skemata.yaml) unknown unit.
