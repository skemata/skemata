# Target `registry`

Describes the values of **one Windows Registry key**.

## 1 Native structure

A registry key contains named **values** and named **subkeys**. Each value has a type (REG_SZ, REG_DWORD, …)
and data. One value per key may be unnamed; Windows tools show it as `(Default)`.

Value types, from the Windows documentation:

| Type | Data |
|---|---|
| REG_SZ | a string |
| REG_EXPAND_SZ | a string with unexpanded environment-variable references such as `%PATH%` |
| REG_MULTI_SZ | a sequence of non-empty strings |
| REG_DWORD | a 32-bit number |
| REG_QWORD | a 64-bit number |
| REG_BINARY | binary data in any form |
| REG_NONE, REG_LINK, REG_DWORD_BIG_ENDIAN, resource lists | not represented by Skemata (section 7) |

## 2 Scope: one key per schema

A `registry` schema describes **exactly one key**: its root node is the key, and the key's values are the
root's members. Subkeys are **not** part of the schema. Each subkey that needs a description gets its own
schema, and the consuming tool decides which schema applies to which key path.

This keeps every registry schema flat, avoids name clashes between a subkey and a value with the same name,
and keeps key paths, hives and registry views (32/64-bit) out of the schema, where they belong
([1.3](../01-introduction.md#13-non-goals)).

**[REG-1]** The root schema MUST describe an object (the key's values are its members) or an array
(the key's values form a numbered list, section 4).

**[REG-2]** Members of a root object MUST be scalars or arrays of strings (REG-7). Objects MUST NOT appear
anywhere except at the root, and `items` MUST NOT appear except on a root array and on string arrays.

Subkeys of the key are outside the data model. A data validator neither checks them nor reports them as
undescribed content.

## 3 Mapping

| Data model | Registry |
|---|---|
| root object | the key |
| member of the root object | a value of the key |
| member with the empty name `""` | the key's unnamed `(Default)` value |
| root array | the key's values named `1`, `2`, `3`, … (section 4) |
| member array of strings | a REG_MULTI_SZ value |
| scalar | value data; type per section 5 |

## 4 Lists stored as numbered values

Many applications store a list as a key of its own, with one value per item named `1`, `2`, `3`, …. Browser
policies such as `ExtensionInstallForcelist` are typical. One-key-per-schema means such a key gets its own
schema, and its root is the list:

```yaml
$schema: https://skemata.github.io/skemata/draft-01/schema
$target: registry
title: Extensions installed by force
type: array
uniqueItems: true
items:
  type: string
  pattern: "^[a-p]{32}(;https?://.+)?$"
```

**[REG-3]** If the root is an array, its items MUST be scalars of the kinds allowed for values (section 5):
strings with or without `format` (REG_SZ), `expandable` strings (REG_EXPAND_SZ), binary strings (REG_BINARY),
integers (REG-4) or textual numbers. Item *n* is stored as the value named by the decimal number *n* (`1`,
`2`, …, without leading zeros), starting at 1.

When reading data, the values whose names are decimal numbers ≥ 1 without leading zeros are the items. They
MUST form a contiguous sequence starting at `1`; a gap makes the data invalid. Values with other names are
undescribed content.

Lists with another naming scheme, such as zero-based or padded names, cannot be described as arrays in
draft-01. Describe them as a root object with `propertyNames` and `additionalProperties`.

## 5 Value types

| Schema | Stored as |
|---|---|
| `type: string` | REG_SZ |
| `type: string`, `expandable: true` | REG_EXPAND_SZ |
| `type: string`, `contentEncoding: base64` | REG_BINARY |
| `type: string` with a `format` | REG_SZ in the format's syntax |
| `type: integer` without `format` (implicit `uint32`, REG-4), or `format` `int8`, `int16`, `int32`, `uint8`, `uint16` or `uint32` | REG_DWORD |
| `type: integer`, `format` `int64` or `uint64` | REG_QWORD |
| `type: integer` or `number`, `textual: true` | REG_SZ in the lexical form |
| `type: array`, items `type: string` (member) | REG_MULTI_SZ |

**[REG-4]** A subschema of type `integer` without `format` and without `textual: true` is implicitly
`format: uint32` and stored as REG_DWORD ([4.11](../04-keywords.md#411-implicit-rules-and-the-normalized-form)).
QWORD values and signed values declare their `format` explicitly. For signed formats, the 32 or 64 bits of
the stored value are read as a two's-complement number. For unsigned formats, they are read as an unsigned
number.

**[REG-5]** A subschema of type `number` MUST declare `textual: true`. The registry has no floating-point
type.

**[REG-6]** `expandable` MUST only be used on strings that would otherwise be stored as plain REG_SZ text: not
together with `format`, `contentEncoding` or `textual`. Such strings are stored as REG_EXPAND_SZ.

**[REG-7]** A member array MUST have items of `type: string` without `expandable`, `contentEncoding` or
`textual`. It is stored as REG_MULTI_SZ. Because REG_MULTI_SZ cannot contain empty strings, every item is
implicitly constrained to a minimum length of 1. An empty array is a REG_MULTI_SZ value with no strings,
which differs from an absent value.

There is no boolean and no null in the registry ([TYP-3](../05-types-and-formats.md#54-booleans), 8.5).
On/off values are labeled enums of their stored values, typically DWORD `1` and `0`.

**[REG-8]** A data validator MUST report a declared value whose registry type differs from the type that the
schema determines (for example REG_SZ where REG_DWORD is expected).

## 6 Names

**[REG-9]** Value names are compared case-insensitively ([3.3.2](../03-data-model.md#332-names)). A value name
MUST NOT exceed 16 383 UTF-16 code units (what Windows calls characters). The empty name `""` denotes the
`(Default)` value ([MOD-3](../03-data-model.md#332-names)).

Value names may contain any characters, including spaces and backslashes. In YAML, quote names that contain
special characters or look like numbers ([DOC-3](../02-document-structure.md#211-quoting-in-yaml)).

## 7 Representability limits

- Subkeys, security descriptors, key paths, hives and registry views are outside the data model.
- Values of type REG_NONE, REG_LINK, REG_DWORD_BIG_ENDIAN and the resource-list types cannot be described.
  If a declared member has such a type, REG-8 applies.
- REG_MULTI_SZ arrays of non-strings and lists with non-standard numbering need workarounds (sections 4 and 5).
- Very long values are discouraged by Windows itself. Skemata imposes no size limit beyond `maxLength`.

## 8 Examples

| Example | Shows |
|---|---|
| [edge-policies](../examples/registry/edge-policies.skemata.yaml) | DWORD on/off and choice enums, a URL, groups, a locale file |
| [edge-extension-install-forcelist](../examples/registry/edge-extension-install-forcelist.skemata.yaml) | a root array stored as numbered values |
| [windows-service](../examples/registry/windows-service.skemata.yaml) | REG_EXPAND_SZ, REG_MULTI_SZ, REG_BINARY, DWORD enums |
| [file-type-association](../examples/registry/file-type-association.skemata.yaml) | the `(Default)` value as member `""` |
| [invalid/](../examples/registry/invalid/) | nested object, boolean type, integer without format, case-insensitive name clash |
