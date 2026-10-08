# 8. Target profiles

Every Skemata schema has exactly one **target**: the storage format of the data it describes. A **target
profile** is the normative definition of a target. The matrices in this chapter are the single source of
truth for what each target allows; the target chapters in [`targets/`](targets/) refer to them.

## 8.1 What a target profile defines

Each target chapter defines:

1. **Native structure:** the target's own concepts (keys and values, sections, elements, tables…).
2. **Root node:** which node kinds the root may have and what it corresponds to.
3. **Mapping:** how native structures correspond to objects, arrays and scalars.
4. **Allowlist:** its column in the matrices 8.4 and 8.5, plus position-specific restrictions.
5. **Names:** comparison (case-sensitive or not), allowed characters and lengths, and whether the empty
   name is allowed ([MOD-3](03-data-model.md#332-names)).
6. **Stored representation:** native or textual, for each type and format.
7. **Lexical rules:** constraints on text values that the target imposes.
8. **Representability limits:** native constructs outside the data model.
9. **Rules:** numbered requirements, prefixed with the target's rule ID prefix.

## 8.2 Target names

**[TGT-1]** A target name MUST match `^[a-z][a-z0-9]*(-[a-z0-9][a-z0-9.]*)*$`. It consists of the format name,
optionally followed by a variant or version (`ini-windows`, `yaml-1.2`).

Format variants are separate targets; there is no dialect keyword. A variant gets its own target name when
its data model, value rules or name rules differ from an existing target, or when a reader cannot tell the
variants apart from the data. For example, YAML 1.1 resolves `yes` to a boolean, but YAML 1.2 does not, and a
CSV file does not reliably reveal its delimiter (`csv`, `csv-semicolon`, `csv-tab`). Variants that only add
syntax that a reader recognises do not get a new name: `toml` covers TOML 1.0.0 and 1.1.0, and `json` covers
JSON with comments.

## 8.3 Draft-01 targets

| Target | Describes | Root node | Name comparison | Rule prefix | Chapter |
|---|---|---|---|---|---|
| `registry` | one Windows Registry key | object or array | case-insensitive | REG | [registry](targets/registry.md) |
| `ini-windows` | an INI file read with the Windows profile API | object | case-insensitive | INI | [ini-windows](targets/ini-windows.md) |
| `xml` | an XML 1.0 document | object (the document) | case-sensitive | XML | [xml](targets/xml.md) |
| `json` | a JSON text (RFC 8259), comments allowed | object or array | case-sensitive | JSN | [json](targets/json.md) |
| `yaml-1.2` | a YAML 1.2 document (core schema) | object or array | case-sensitive | YML | [yaml-1.2](targets/yaml-1.2.md) |
| `toml` | a TOML document (1.0.0 or 1.1.0) | object (the root table) | case-sensitive | TML | [toml](targets/toml.md) |
| `sqlite` | tables in one SQLite database | object (the database) | case-insensitive (ASCII) | SQL | [sqlite](targets/sqlite.md) |
| `csv` | a comma-separated file with a header row (RFC 4180) | array (the rows) | case-sensitive | CSV | [csv](targets/csv.md) |
| `csv-semicolon` | the same, separated by semicolons | array (the rows) | case-sensitive | CSV | [csv](targets/csv.md) |
| `csv-tab` | the same, separated by tabs | array (the rows) | case-sensitive | CSV | [csv](targets/csv.md) |
| `psd1` | a PowerShell data file | object (the hashtable) | case-insensitive | PSD | [psd1](targets/psd1.md) |

**[TGT-2]** The `$target` value of a schema MUST be one of the names in this table, or a target defined by a
later version of this specification.

## 8.4 Keyword × target matrix

✓ = allowed, — = not allowed (KW-2). Numbers refer to the notes below the table.

| Keyword | registry | ini-windows | xml | json | yaml-1.2 | toml | sqlite | csv ⁴ | psd1 |
|---|---|---|---|---|---|---|---|---|---|
| `$schema`, `$id`, `$target`, `$locale`, `groups`, `$defs` (root only) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `$ref`, `$anchor`, `$comment` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `title`, `description`, `default`, `examples`, `deprecated`, `readOnly`, `writeOnly`, `unit`, `group` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `type` (values: 8.5) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `properties`, `additionalProperties`, `patternProperties`, `propertyNames`, `minProperties`, `maxProperties` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ ¹ | ✓ | ✓ |
| `required` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ ² | ✓ | ✓ |
| `dependentRequired` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | — | ✓ | ✓ |
| `items`, `minItems`, `maxItems` | ✓ | — | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `prefixItems` | — | — | — | ✓ | ✓ | ✓ | — | — | ✓ |
| `uniqueItems` | ✓ | — | ✓ | ✓ | ✓ | ✓ | — | — | ✓ |
| `identity` | — | — | ✓ | ✓ | ✓ | ✓ | ✓ ³ | ✓ | ✓ |
| `ordered` on arrays | ✓ | — | ✓ | ✓ | ✓ | ✓ | — | ✓ | ✓ |
| `ordered` on objects | — | — | ✓ | — | — | — | — | — | — |
| `oneOf` as labeled enum | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `oneOf` as discriminated union | — | — | ✓ | ✓ | ✓ | ✓ | — | — | ✓ |
| `enum`, `const` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `minimum`, `maximum`, `exclusiveMinimum`, `exclusiveMaximum`, `multipleOf` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `minLength`, `maxLength`, `pattern` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `format` (values: 8.5) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `contentEncoding` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `attributes`, `content`, `namespace` | — | — | ✓ | — | — | — | — | — | — |
| `textual` | ✓ | — | — | ✓ | ✓ | ✓ | ✓ | — | ✓ |
| `expandable` | ✓ | — | — | — | — | — | — | — | — |
| empty member name `""` (MOD-3) | ✓ | — | — | — | — | — | — | — | — |

Notes:

1. In `sqlite`, tables MUST be declared members of the root: `additionalProperties` on the root MAY only be
   `false` or `true`, and `patternProperties` and `propertyNames` MUST NOT be used there (SQL-1).
2. In `sqlite`, `required` MUST NOT be used on row objects, because every declared column is present in every
   row (SQL-3).
3. In `sqlite`, every table MUST declare `identity` (SQL-4).
4. The `csv` column applies to `csv`, `csv-semicolon` and `csv-tab` alike.

Target chapters add position-specific restrictions. For example, `registry` allows `items` only on the root
array and on string arrays (REG-2, REG-7).

## 8.5 Type and format × target matrix

Each cell shows the stored representation; — means not allowed. "text" means the lexical form
([5.8](05-types-and-formats.md#58-lexical-forms)) in a target where all values are text.

| Type / format | registry | ini-windows | xml | json | yaml-1.2 | toml | sqlite | csv ⁴ | psd1 |
|---|---|---|---|---|---|---|---|---|---|
| `object` | root only: the key | root: the file; its members: sections | element; root: the document | object | mapping | table | root: the database; table items: rows | the rows | hashtable `@{}` |
| `array` | root: numbered values; member: REG_MULTI_SZ | — | repeated child elements | array | sequence | array | root members: tables | root only: the rows | array `@()` |
| nested arrays (items that are arrays) | — | — | — | array | sequence | array | — | — | array |
| `string` | REG_SZ; with `expandable`: REG_EXPAND_SZ | text | text | string | string | string | TEXT | text | string |
| `integer` with `int8`…`int32`, `uint8`…`uint32` | REG_DWORD | text | text | number | integer | integer | INTEGER | text | integer |
| `integer` with `int64` | REG_QWORD | text | text | number | integer | integer | INTEGER | text | integer |
| `integer` with `uint64` | REG_QWORD | text | text | number | integer | — | — | text | — |
| `integer` without `format` | REG_DWORD, implicit `uint32` (REG-4) | text | text | number | integer | integer | INTEGER | text | integer |
| `number` (`float`, `double`) | — unless `textual` (REG-5) | text | text | number | float | float | REAL (INTEGER accepted, SQL-7) | text | double |
| `integer`, `number` with `textual: true` | REG_SZ | — | — | string | string | string | TEXT | — | string |
| `boolean` | — | — | — | `true` / `false` | bool | boolean | — | — | `$true` / `$false` |
| nullable (`[T, "null"]`) | — | — | — | `null` | null | — | NULL | — | `$null` |
| binary (`contentEncoding: base64`) | REG_BINARY | text | text | string | string | string | BLOB | text | string |
| `date-time` | REG_SZ | text | text | string | string | offset date-time | TEXT | text | string |
| `date-time-local` | REG_SZ | text | text | string | string | local date-time | TEXT | text | string |
| `date` | REG_SZ | text | text | string | string | local date | TEXT | text | string |
| `time-local` | REG_SZ | text | text | string | string | local time | TEXT | text | string |
| `time`, `duration`, `uuid`, `uri`, `hostname`, `ipv4`, `ipv6`, `email` | REG_SZ | text | text | string | string | string | TEXT | text | string |
| `date-time`, `date-time-local`, `date`, `time-local` with `textual: true` | — | — | — | — | — | string | — | — | — |

`textual: true` on a temporal format is allowed only in `toml`, the only draft-01 target that stores
temporal values natively (KW-25).

## 8.6 Adding a target

A new target is added by a revision of this specification:

1. Write a target chapter covering every item in 8.1.
2. Add a column to 8.4 and 8.5. New targets allow nothing by default; every ✓ is a deliberate decision.
3. If the target needs a concept the core lacks, add a keyword to [chapter 4](04-keywords.md) with a generic
   name that describes the concept, never the format. Existing targets do not gain the new keyword
   automatically: their columns in 8.4 stay — unless deliberately changed.
4. Add worked examples, including negative examples, under `spec/examples/<target>/`.

## 8.7 Candidate targets (non-normative)

Possible future targets include `plist` (Apple property lists and configuration profiles), `yaml-1.1`,
further INI variants (for example systemd unit files, Git configuration, or INI files with keys before the
first section), Java `.properties` and `.env` files, HCL, KDL, and `dconf`. Their known needs are recorded in
the [design decisions](../decisions/README.md).
