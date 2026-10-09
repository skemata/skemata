# 5. Types and formats

## 5.1 Overview

`type` declares a node's logical type ([3.2](03-data-model.md#32-scalar-values)). For scalars, `format`
narrows the type (for example `uint32` or `date-time`), and `contentEncoding` marks binary data. The set of
`format` values is **closed**: only the values in this chapter are allowed, and validators check them.

Format names match those in JSON Schema and the
[OpenAPI Format Registry](https://spec.openapis.org/registry/format/), so authors who know either will
recognise them.

Each target defines how a type and format are stored: natively or as text
([8.5](08-target-profiles.md#85-type-and-format--target-matrix)).

## 5.2 Integers

| `format` | Range |
|---|---|
| `int8` | −128 … 127 |
| `int16` | −32 768 … 32 767 |
| `int32` | −2 147 483 648 … 2 147 483 647 |
| `int64` | −9 223 372 036 854 775 808 … 9 223 372 036 854 775 807 |
| `uint8` | 0 … 255 |
| `uint16` | 0 … 65 535 |
| `uint32` | 0 … 4 294 967 295 |
| `uint64` | 0 … 18 446 744 073 709 551 615 |

**[TYP-1]** An integer value MUST lie within the range of its `format`. Without `format`, the range is that of
`int64`.

Some targets also use the format to choose a native type: in `registry`, 8-, 16- and 32-bit formats are
stored as REG_DWORD and 64-bit formats as REG_QWORD.

## 5.3 Numbers

| `format` | Meaning |
|---|---|
| `double` (default) | IEEE 754 binary64 |
| `float` | IEEE 754 binary32 |

**[TYP-2]** Number values MUST be finite. NaN and the infinities are outside the Skemata data model in draft-01.
A `float` value MUST lie within the finite range of binary32.

## 5.4 Booleans

Stores without a native boolean encode on/off values in many ways: `1`/`0`, `yes`/`no`, `true`/`false`,
`on`/`off`, `Y`/`N`, `enabled`/`disabled`, and more. No encoding can be assumed reliably. Numbers differ: their
lexical form (5.8) is canonical, so a number stored as text is still described as a number.

**[TYP-3]** `type: boolean` MUST only be used where the target has a native boolean (`json`,
`yaml-1.2`, `toml`, `psd1`). Elsewhere, an on/off value is described by its actual stored values, using a
labeled enum ([4.4](04-keywords.md#oneof)). Where the software that reads the data accepts several spellings,
a choice lists them all, and the first one is written:

```yaml
# registry: a DWORD that is 1 or 0
ShowHomeButton:
  format: uint32
  oneOf:
    - { const: 1, title: Show }
    - { const: 0, title: Hide }

# ini-windows: text that the application reads in several spellings
AutoCheck:
  oneOf:
    - { enum: ["yes", "true", "1"], title: Enabled }
    - { enum: ["no", "false", "0"], title: Disabled }
```

UIs MAY render a labeled enum with exactly two value choices as a switch
([6.7](06-ui-annotations.md#67-labeled-enums)). The author declares every stored value; Skemata never assumes
an encoding.

## 5.5 Strings and string formats

A string is a sequence of Unicode scalar values. The following formats refine it:

| `format` | Meaning | Reference | Example |
|---|---|---|---|
| `date` | calendar date | RFC 3339 `full-date` | `2026-10-08` |
| `time` | time of day with UTC offset | RFC 3339 `full-time` | `14:30:00+02:00` |
| `date-time` | date and time with UTC offset | RFC 3339 `date-time` | `2026-10-08T14:30:00Z` |
| `date-time-local` | date and time without offset | RFC 3339 `full-date "T" partial-time` | `2026-10-08T14:30:00` |
| `time-local` | time of day without offset | RFC 3339 `partial-time` | `14:30:00` |
| `duration` | duration | RFC 3339 Appendix A `duration` | `P1DT2H` |
| `uuid` | UUID | RFC 9562 string representation | `1b4e28ba-2fa1-11d2-883f-0016d3cca427` |
| `uri` | absolute URI | RFC 3986 `URI` | `https://example.org/start` |
| `hostname` | host name | RFC 1123 | `intranet.example.org` |
| `ipv4` | IPv4 address | RFC 2673 §3.2 dotted-quad | `192.0.2.1` |
| `ipv6` | IPv6 address | RFC 4291 §2.2 | `2001:db8::1` |
| `email` | mailbox | RFC 5321 `Mailbox` | `admin@example.org` |

**[TYP-4]** A string with a `format` MUST conform to the referenced syntax.

Windows GUIDs written with braces (`{1B4E28BA-…}`) do not match `uuid`; describe them with `pattern`.

Some targets store temporal formats natively (TOML has local dates and times). Section 8.5 lists them.

## 5.6 Binary data

Binary data is a sequence of octets. It is declared with `type: string` and `contentEncoding: base64`, as in
JSON Schema. In the schema document, binary literals (`const`, `enum`, `default`, `examples`) are written as
base64 text.

**[TYP-5]** Binary literals MUST use the base64 alphabet of RFC 4648 §4, with padding and without line breaks.

Targets with a native binary type store the octets (`registry`: REG_BINARY, `sqlite`: BLOB). Other targets
store the base64 text.

## 5.7 Environment-variable references

`expandable: true` ([4.7](04-keywords.md#expandable)) declares that a string contains references to
environment variables that consumers expand when reading the value. The target defines the reference syntax.
In `registry`, references look like `%SystemRoot%`, and the value is stored as REG_EXPAND_SZ.

Constraints such as `pattern` and `maxLength` apply to the unexpanded text as stored.

## 5.8 Lexical forms

Where a value is stored as text (in text-only targets such as `ini-windows` and `xml`, or with
`textual: true`), it uses the following lexical forms:

| Value | Lexical form |
|---|---|
| integer | `0` or `-?[1-9][0-9]*`, ASCII digits only. No `+` sign, leading zeros, whitespace or digit separators. |
| number | the JSON number grammar (RFC 8259 §6): `-?(0\|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?`. The decimal separator is always `.`, regardless of locale. |
| string with `format` | the syntax of the format (5.5) |
| binary | base64 (TYP-5) |
| plain string | the string itself |

**[TYP-6]** A data validator MUST accept a textually stored integer or number only if it matches the lexical
form above. It then checks the value against the subschema's constraints.

Values in other notations, such as hexadecimal integers, are described as strings with a `pattern`.

## 5.9 Format and type compatibility

**[TYP-7]** `format` MUST only be used with the type listed here:

| `format` values | `type` |
|---|---|
| `int8`, `int16`, `int32`, `int64`, `uint8`, `uint16`, `uint32`, `uint64` | `integer` |
| `float`, `double` | `number` |
| `date`, `time`, `date-time`, `date-time-local`, `time-local`, `duration`, `uuid`, `uri`, `hostname`, `ipv4`, `ipv6`, `email` | `string` |

Which formats a target allows is listed in [8.5](08-target-profiles.md#85-type-and-format--target-matrix).

## 5.10 Literal values in schema documents

How `const`, `enum`, `default` and `examples` values are written in YAML and JSON, including 64-bit integers,
is defined in [2.5](02-document-structure.md#25-literal-values).
