# 7. Localization

Schemas are written in one **source language**. Translations live in separate **locale files**, so anyone
can add a language without changing the schema. This follows the model of Windows administrative templates
(ADMX for structure, ADML for language resources). Texts are neither maps of languages inline, which would
change the meaning of JSON Schema's `title` and `description`, nor message keys, which would make schemas
unreadable without their resources.

## 7.1 Source language and identity

The root keyword `$locale` declares the language of the inline texts as a BCP 47 tag. The default is `en`.

**[LOC-1]** A schema that has locale files MUST have an `$id`. Locale files refer to the schema by it.

## 7.2 Translatable texts

Translatable texts are the values of `title` and `description` in:

- any subschema, including the root and `$defs` entries,
- `oneOf` branches (labeled enum values),
- entries of the root `groups`.

This includes titles derived from names ([6.2.1](06-ui-annotations.md#621-derived-titles)): a locale file can
give a translated title to a subschema that has no inline `title`.

`examples`, `default` and `const` values are data, not text, and are not translated.

## 7.3 Locale files

A locale file is a YAML or JSON document. The same serialization rules as for schemas apply
([DOC-1 to DOC-3](02-document-structure.md#21-serialization)).

```yaml
$schema: https://skemata.github.io/skemata/draft-01/locale
schema: https://skemata.github.io/skemata/examples/registry/edge-policies
$locale: de
texts:
  "#":
    title: Microsoft Edge-Richtlinien (Auszug)
  "#/properties/ShowHomeButton":
    title: Startseiten-Schaltfläche anzeigen
  "#/properties/ShowHomeButton/oneOf/0":
    title: Anzeigen
  "#home-button-hide":
    title: Ausblenden
  "#/groups/startup":
    title: Start
```

| Key | Required | Value |
|---|---|---|
| `$schema` | yes | the Skemata locale URI of the version: `https://skemata.github.io/skemata/draft-01/locale` |
| `schema` | yes | the `$id` of the translated schema |
| `$locale` | yes | BCP 47 tag of the translation |
| `texts` | yes | translated texts, keyed by location in the schema |

**[LOC-2]** A locale file MUST contain exactly the keys `$schema`, `schema`, `$locale` and `texts`. `$locale`
MUST be a well-formed BCP 47 tag and SHOULD differ from the schema's `$locale`.

### 7.3.1 Keys

Each key of `texts` identifies a location in the schema as a URI fragment, the same way `$ref` does:

- `#` is the root schema;
- `#/…` is a JSON Pointer (RFC 6901) to a subschema, a `oneOf` branch or a `groups` entry;
- `#name` refers to the subschema with `$anchor: name`.

JSON Pointer keys are written in URI-fragment form (RFC 6901, sections 3 and 6): `~` becomes `~0`, `/`
becomes `~1`, and characters not allowed in URI fragments, such as spaces, are percent-encoded in UTF-8. For
example, the member `Content Type` is `#/properties/Content%20Type`, and the member with the empty name is
`#/properties/` (a pointer ending with an empty segment).

JSON Pointers break when a schema is restructured. Anchors stay stable. Authors who expect translations
SHOULD give translated subschemas an `$anchor`, especially members and enum values.

**[LOC-3]** Every key of `texts` MUST resolve to a subschema, a `oneOf` branch or a `groups` entry of the
schema identified by `schema`.

**[LOC-4]** Every value in `texts` MUST be an object containing only `title`, `description`, or both, as
strings that follow [UI-2](06-ui-annotations.md#62-title-and-description).

A locale file changes only texts. It cannot change types, values or structure.

### 7.3.2 File naming

**[LOC-5]** Locale files SHOULD be named `<schema name>.<locale>.skemata-locale.yaml` (or `.skemata-locale.json`)
and stored next to the schema, for example `edge-policies.skemata.yaml` and `edge-policies.de.skemata-locale.yaml`.

## 7.4 Resolution

To show a text in a requested language, a consumer:

1. uses the locale file whose `$locale` matches the requested tag exactly, if it has an entry for the text;
2. otherwise, removes the last subtag of the requested tag and repeats (for example `de-AT` → `de`);
3. otherwise, uses the inline text from the schema.

**[LOC-6]** Consumers MUST resolve each text individually, so that a partial translation falls back to the
source language per text, not per file.
