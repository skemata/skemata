# 6. UI annotations

Skemata schemas can be turned into documentation and user interfaces: forms, editors, reference pages.
Annotation keywords carry the information needed for that. They do not change which data is valid, except
that their own values must be valid (UI-1).

## 6.1 Overview

| Keyword | Where | Value |
|---|---|---|
| `title` | any subschema, `oneOf` branches, groups | short label; derived from the name if absent (6.2.1) |
| `description` | any subschema, `oneOf` branches, groups | explanatory text |
| `default` | any subschema | the value that applies when the member is absent |
| `examples` | any subschema | sample values |
| `deprecated` | any subschema, `oneOf` branches | boolean |
| `readOnly` | any subschema | boolean |
| `writeOnly` | any subschema | boolean |
| `unit` | integers, numbers | unit of measurement (6.9) |
| `groups` | root | group definitions |
| `group` | declared members | group name |

All except `unit`, `groups` and `group` come from JSON Schema 2020-12 and keep their meaning.

**[UI-1]** Every `default` value and every `examples` entry MUST be valid against the subschema it appears in.

## 6.2 `title` and `description`

`title` is a short label, such as a field name in a form. `description` explains the value: what it means,
when to use it, and what side effects it has.

**[UI-2]** `title` MUST be a single line of plain text. `description` MUST be plain text and MAY contain line
breaks, with paragraphs separated by blank lines. Consumers MUST NOT interpret markup in either.

Both are written in the schema's source language and can be translated ([chapter 7](07-localization.md)).

### 6.2.1 Derived titles

Most names are readable on their own, so `title` is optional. When a member, an attribute or a group has no
`title`, its title is **derived** from its name and becomes part of the normalized form
([4.11](04-keywords.md#411-implicit-rules-and-the-normalized-form)).

**[UI-7]** Processors that derive titles MUST use this algorithm:

1. The empty name `""` gets the target's display name for unnamed members (`registry`: `(Default)`).
2. The name is split into words:
   - at `_`, `-`, `.` and spaces, which are removed;
   - between a lowercase letter or a digit and a following uppercase letter;
   - inside a run of uppercase letters, before its last letter, if that letter is followed by a lowercase
     letter.
3. If the name contains at least one lowercase letter, words of two or more letters that are entirely
   uppercase are kept as they are (acronyms). All other words are converted to lowercase. A name without
   lowercase letters (`MAX_CONNECTIONS`) therefore has no acronyms.
4. The words are joined with single spaces, and the first letter of the result is converted to uppercase.

| Name | Derived title |
|---|---|
| `ShowHomeButton` | Show home button |
| `HomepageURL` | Homepage URL |
| `HTTPProxyServer` | HTTP proxy server |
| `package-ecosystem` | Package ecosystem |
| `keep_versions` | Keep versions |
| `server.port` | Server port |
| `Content Type` | Content type |
| `Win32Path` | Win32 path |
| `MAX_CONNECTIONS` | Max connections |
| `""` (registry) | (Default) |

Where a derived title reads poorly, the author writes `title` explicitly. A choice of a labeled enum without
`title` is shown with its value, unchanged. Array items and the root have no name, so nothing is derived for
them.

Derived titles belong to no language. Locale files can still translate them, because their keys address the
subschema, not the title ([7.3.1](07-localization.md#731-keys)).

## 6.3 `default`

`default` documents the value that applies **when the member is absent**, typically the built-in value of the
software that reads the data. It is information for people and UIs, for example "Default: 30 minutes" or a
placeholder in an input field.

`default` does not mean "write this value". Skemata describes structure; whether and when a value is written is
decided by the consuming tool (see [1.3](01-introduction.md#13-non-goals)).

## 6.4 `examples`

An array of sample values that UIs and documentation may show.

## 6.5 `deprecated`

`deprecated: true` marks a member or enum value that still works but should no longer be used. UIs SHOULD
show it as deprecated and SHOULD NOT offer it for new configurations.

## 6.6 `readOnly` and `writeOnly`

- `readOnly: true`: the software that owns the data manages the value, for example a "last run" timestamp.
  UIs SHOULD show it without allowing edits, and tools SHOULD NOT change it.
- `writeOnly: true`: the value is sensitive, for example a password or token. UIs SHOULD mask it, and tools
  SHOULD NOT display or log values read from the target.

**[UI-3]** A subschema MUST NOT set both `readOnly: true` and `writeOnly: true`.

## 6.7 Labeled enums

A labeled enum ([4.4](04-keywords.md#oneof)) gives each choice its own `title` and optional `description`
and `deprecated`. UIs SHOULD render it as a choice (drop-down, radio buttons) showing the titles:

- A **value choice** selects its value. If it lists several spellings (`enum`), UIs show it once, and tools
  that write it SHOULD write the first spelling.
- A **range choice** selects "any other allowed value"; UIs show it as a choice with an input field for the
  value, for example "Fixed size: [ 100 ] MB".

A labeled enum with exactly two value choices and no range choice MAY be rendered as a switch. Plain `enum`
values have no titles, so UIs show the values themselves.

## 6.8 Groups

Groups organize members into sections of a UI, independent of the data structure. This is useful where the
structure is flat, such as the values of one registry key.

```yaml
groups:
  startup: {}                    # title "Startup", derived from the key
  privacy:
    description: Settings that control data sent to the vendor.
properties:
  HomepageLocation:
    type: string
    group: startup
  MetricsReportingEnabled:
    format: uint32
    group: privacy
```

**[UI-4]** `groups` MUST be an object. Each key MUST match the `$anchor` syntax
([KW-7](04-keywords.md#anchor)), and each value MUST be an object that contains only `title` and
`description`, both optional.

**[UI-5]** `group` MUST only appear on subschemas that are values of `properties`, and its value MUST be a
key of the root `groups`.

Members without `group` belong to no group. UIs typically show them first or under a general heading.

## 6.9 Units

`unit` states the unit of measurement of an integer or number, so that UIs can label and convert values, for
example show `104857600` bytes as "100 MiB". It does not change the stored value: tools that write the value
MUST write it in the declared unit.

| Category | Units |
|---|---|
| Data size | `bit`, `byte`, `kilobyte`, `megabyte`, `gigabyte`, `terabyte` (powers of 1000), `kibibyte`, `mebibyte`, `gibibyte`, `tebibyte` (powers of 1024) |
| Time | `nanosecond`, `microsecond`, `millisecond`, `second`, `minute`, `hour`, `day`, `week` |
| Proportion | `percent` |
| Display | `pixel`, `point` |

**[UI-8]** `unit` MUST be one of the values in this table and MUST only be used on subschemas of type
`integer` or `number` (including textual ones).

Further units can be added by later versions of this specification.

## 6.10 Presentation order

**[UI-6]** Processors MUST preserve the document order of the entries in `properties`, `groups` and `oneOf`.
UIs SHOULD present members, groups and choices in that order.

Most YAML and JSON parsers preserve mapping order even though the formats do not guarantee it. Skemata relies
on it, so authors can control the order in a UI without an extra keyword.

## 6.11 Rendering guidance (non-normative)

| Schema | Typical control |
|---|---|
| `boolean` | checkbox or switch |
| labeled enum / `enum` | drop-down or radio buttons; a range choice with an input field |
| `integer`, `number` with bounds | numeric field, spinner or slider, labeled with the `unit` |
| `string` with `format` | specialised input (date picker, URL field…) |
| `string` with `writeOnly` | password field |
| array of scalars | editable list; for sets, without ordering controls if `ordered: false` |
| keyed array | table whose identity values form the key column(s) |
| tuple (`prefixItems`) | a fixed row of fields, one per position |
| nested arrays | nested lists or a grid |
| object | section or nested panel |
| object with `additionalProperties` | name/value table |
| object with `attributes` or `content` | fields for the attributes and the content, next to those of the members |
| `dependentRequired` | dependent fields become mandatory once their trigger is filled in |
| discriminated union | choice of the discriminator, then that branch's fields |
| recursive structure | tree view, expanded on demand |
