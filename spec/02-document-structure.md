# 2. Document structure

## 2.1 Serialization

A Skemata schema is a single YAML or JSON document. YAML is recommended for hand-written schemas because it
supports comments.

**[DOC-1]** A schema document MUST be either JSON (RFC 8259) or YAML 1.2, encoded in UTF-8. YAML documents
MUST be parsed with the YAML 1.2 **core schema**.

**[DOC-2]** A YAML schema document MUST contain exactly one YAML document and MUST NOT use explicit tags
(`!…`, `!!…`), anchors, aliases or merge keys (`<<`). All mapping keys MUST be strings.

These restrictions keep every YAML schema convertible to JSON without loss, so that tools built for either
format see the same document.

### 2.1.1 Quoting in YAML

Many YAML tools still follow YAML 1.1, which reads plain `yes`, `no`, `on`, `off`, `y` and `n` as booleans,
`010` as the octal number 8, and `1:30` as the base-60 number 90. YAML 1.2 reads the words and `1:30` as
strings and `010` as the decimal number 10. `~` is null in both. A schema whose meaning depends on the YAML
version would be read differently by different tools.

**[DOC-3]** In YAML schema documents, a string value MUST be quoted if, unquoted, it would resolve to a
non-string under either YAML 1.1 or YAML 1.2. In particular: `yes`, `no`, `on`, `off`, `y`, `n`, `true`,
`false`, `null` and `~` in any capitalization, and anything that looks like a number, date or time. Mapping
keys that look like numbers (for example property names `"1"`, `"2"`) MUST be quoted as well.

```yaml
oneOf:
  - { const: "yes", title: Enabled }     # correct
  - { const: no, title: Disabled }       # wrong: boolean false in YAML 1.1
```

## 2.2 Root keywords

The root of a schema document is a subschema that describes the root node of the target data
([3.1](03-data-model.md#31-nodes)). It also carries the following document keywords:

| Keyword | Required | Value |
|---|---|---|
| `$schema` | yes | the Skemata version URI (2.3) |
| `$target` | yes | a target name ([8.3](08-target-profiles.md#83-draft-01-targets)) |
| `$id` | recommended | an absolute URI without fragment that identifies this schema |
| `$locale` | no | BCP 47 language tag of the inline texts; default `en` ([chapter 7](07-localization.md)) |
| `groups` | no | UI groups ([6.8](06-ui-annotations.md#68-groups)) |
| `$defs` | no | reusable subschemas ([4.3](04-keywords.md#43-document-and-reference-keywords)) |

**[DOC-4]** The root schema MUST contain `$schema` and `$target`.

**[DOC-5]** `$id`, if present, MUST be an absolute URI without a fragment. A schema that has localization
files MUST have an `$id` ([LOC-1](07-localization.md#71-source-language-and-identity)).

**[DOC-6]** `$locale`, if present, MUST be a well-formed BCP 47 language tag.

`title` and `description` on the root describe the schema as a whole.

## 2.3 Version URIs

`$schema` identifies the version of Skemata that the document uses, and thereby its keywords, rules and
targets.

| Version | `$schema` |
|---|---|
| Draft 01 | `https://skemata.github.io/skemata/draft-01/schema` |

**[DOC-7]** A processor MUST reject a schema whose `$schema` it does not recognise.

> The project's own identifiers are hosted on GitHub Pages under `https://skemata.github.io/skemata/`.
> Meta-schemas for editor support are published under the same URIs
> ([9.4](09-versioning-and-conformance.md#94-meta-schema-architecture)). These URIs are permanent; moving to
> another domain would need new URIs in a new version.

## 2.4 References

Subschemas can be reused with `$defs` and `$ref`, and named with `$anchor`. Draft-01 allows only local
references within the same document ([KW-4](04-keywords.md#ref)). References may be recursive, as long as
every recursion can end ([KW-6](04-keywords.md#ref)).

## 2.5 Literal values

`const`, `enum`, `default` and `examples` contain **literal values** written in YAML or JSON:

| Type | Written as |
|---|---|
| `string` | a string |
| `integer` | an integer number |
| `number` | a number |
| `boolean` | `true` / `false` (only where allowed, TYP-3) |
| null | `null` (only for nullable types) |
| binary | a base64 string ([TYP-5](05-types-and-formats.md#56-binary-data)) |
| temporal formats | a string in the format's syntax |

A literal is always written as its **logical** value, even if it is stored textually. For example, a port
number stored as INI text is written as `8080`, not `"8080"`.

**[DOC-8]** Processors MUST read integer literals exactly over the whole range −2⁶³ … 2⁶⁴−1.

JSON and YAML allow integers of any size, but some tools, notably JavaScript-based editors, convert numbers
to IEEE 754 doubles and lose precision above 2⁵³. Authors should be aware that such tools may display large
64-bit literals incorrectly. The document itself remains correct.

## 2.6 File naming

**[DOC-9]** Schema files SHOULD be named `<name>.skemata.yaml` (or `.skemata.yml`) for YAML and `<name>.skemata.json`
for JSON. Editors and tools can associate the Skemata meta-schemas with these names.

## 2.7 Example

```yaml
$schema: https://skemata.github.io/skemata/draft-01/schema
$id: https://example.org/schemas/example-app/settings
$target: json
$locale: en
title: Example App settings
description: User settings of Example App, stored in settings.json.
type: object
properties:
  theme:
    type: string
    title: Color theme
    default: system
    oneOf:
      - { const: light, title: Light }
      - { const: dark, title: Dark }
      - { const: system, title: Follow the system }
  autoUpdate:
    type: boolean
    title: Install updates automatically
    default: true
  proxy:
    $ref: "#/$defs/proxy"
$defs:
  proxy:
    type: object
    title: Proxy server
    properties:
      host: { type: string, format: hostname }
      port: { type: integer, format: uint16, minimum: 1 }
    required: [host]
```
