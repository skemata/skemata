# Target `ini-windows`

Describes an **INI file as read and written by the Windows profile API** (`GetPrivateProfileString`,
`WritePrivateProfileString` and related functions).

INI is not standardised; implementations differ in comments, quoting, escapes, duplicate keys and keys
outside sections. This target follows the Windows API, the most widely deployed behaviour on Windows. Other
INI variants would be separate targets ([8.2](../08-target-profiles.md#82-target-names)).

## 1 Native structure

```ini
; comment
[Section]
Key=value
Other Key = another value
```

An INI file consists of **sections**. Each section contains **keys** with text values. The Windows API
addresses every key through its section: it reads and writes keys by section name and key name, and treats
both case-insensitively. When a value is enclosed in single or double quotation marks, the API discards the
marks when reading.

## 2 Mapping

| Data model | INI file |
|---|---|
| root object | the file |
| member of the root (an object) | a section `[name]` |
| member of a section (a scalar) | a key `name=value` in that section |

**[INI-1]** The root schema MUST describe an object, and every member of the root MUST be an object
(a section). Keys before the first section ("global keys") cannot be described: the Windows API cannot
address them.

**[INI-2]** Every member of a section MUST be a scalar. Arrays and nested objects are not allowed.

An empty value (`Key=`) is the empty string; a missing key is absent. A section without keys is an empty
object.

## 3 Values

All values are text. Integers and numbers use the lexical forms of
[5.8](../05-types-and-formats.md#58-lexical-forms), binary values use base64, and strings with a `format` use
the format's syntax. There is no boolean and no null. On/off values are labeled enums of their stored text,
such as `"1"`/`"0"` or `"yes"`/`"no"` ([TYP-3](../05-types-and-formats.md#54-booleans)).

Because every value is text, `textual` is not used ([8.4](../08-target-profiles.md#84-keyword--target-matrix)):
a `type: integer` key is simply stored as its decimal text.

**[INI-3]** A string value MUST NOT contain line breaks, MUST NOT begin or end with whitespace, and MUST NOT
both begin and end with the same quotation mark (`"` or `'`). These constraints apply implicitly to every
value. Literals in the schema (`const`, `enum`, `default`, `examples`) MUST satisfy them.

The reasons: line breaks end a key; surrounding whitespace is not preserved reliably by INI readers; and
the API removes surrounding quotation marks, so such a value would not read back as written.

## 4 Names

**[INI-4]** Section and key names are compared case-insensitively ([3.3.2](../03-data-model.md#332-names)).
Names MUST NOT be empty, MUST NOT contain
line breaks, and MUST NOT begin or end with whitespace. Section names MUST NOT contain `]`. Key names MUST NOT
contain `=` and MUST NOT begin with `;` or `[`.

## 5 Duplicates

INI files sometimes contain the same section or key more than once. The data model has unique names.

**[INI-5]** If a declared section, or a declared key within a declared section, occurs more than once in the
file (compared case-insensitively), a data validator MUST report the data as ambiguous.

## 6 Representability limits

- Comments, blank lines, the order of sections and keys, and the file encoding are outside the data model.
- Keys outside sections cannot be described (INI-1).
- Lists, whether as repeated keys or numbered keys, cannot be described as arrays. Numbered keys can be
  described as dynamic members with `propertyNames`.
- Values with surrounding quotation marks or whitespace, or with line breaks, cannot be described (INI-3).
- Windows can redirect profile API access for some files to the registry (`IniFileMapping`). Which store
  is actually used is a matter of location and outside Skemata's scope.

## 7 Examples

| Example | Sample data | Shows |
|---|---|---|
| [desktop-ini](../examples/ini-windows/desktop-ini.skemata.yaml) | [desktop.ini](../examples/ini-windows/desktop.ini) | sections, a `1`/`0` enum, an integer, a choice enum, undescribed keys |
| [example-app](../examples/ini-windows/example-app.skemata.yaml) | [example-app.ini](../examples/ini-windows/example-app.ini) | an on/off value with several spellings, integer and number ranges, units, a deprecated choice, dynamic keys, implicit types and titles |

Invalid: [INI-1](../examples/ini-windows/invalid/INI-1-global-key.skemata.yaml) global key,
[INI-2](../examples/ini-windows/invalid/INI-2-array-in-section.skemata.yaml) array in a section,
[TYP-3](../examples/ini-windows/invalid/TYP-3-boolean.skemata.yaml) boolean,
[KW-2](../examples/ini-windows/invalid/KW-2-textual.skemata.yaml) `textual`.
