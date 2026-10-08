# Skemata

Skemata is a schema language for **describing and validating data of any kind**, in whatever format it is
stored: the Windows Registry, INI files, XML, JSON, SQLite databases, YAML, TOML, and more.

It looks and feels like JSON Schema. Schemas are plain YAML or JSON documents. Each schema describes data in
exactly one storage format, its **target**, and each target allows exactly the keywords it can represent.

> **Status: Draft 01, work in progress.** Nothing here is stable yet. Feedback is welcome.

## Why

Data lives in many formats. To validate, document or display it, tools need a precise description: which
values exist, their types and ranges, how list items are identified, and how each value is stored in its
format.

JSON Schema describes JSON-shaped data well. It cannot say "this integer is a REG_DWORD", "this is the
registry key's `(Default)` value", "this member is an XML attribute", or "rows of this table are identified
by these columns". Skemata adds exactly these concepts, with generic names, and restricts each storage format to
what it can actually represent.

## A first example

```yaml
$schema: https://skemata.github.io/skemata/draft-01/schema
$id: https://skemata.github.io/skemata/examples/registry/edge-policies
$target: registry
title: Microsoft Edge policies (excerpt)
properties:
  HomepageLocation:
    format: uri               # type string, inferred from the format
    title: Home page URL
  ShowHomeButton:             # title "Show home button", derived from the name
    oneOf:                    # the registry has no boolean: describe the stored values
      - { const: 1, title: Show }
      - { const: 0, title: Hide }
  DiskCacheSize:              # an integer without format is a REG_DWORD
    unit: byte
    oneOf:
      - { const: 0, title: Let the browser decide }
      - { minimum: 1048576, title: Fixed size }
```

Obvious things are implied: types are inferred, titles are derived from names, and identity values are
required. The same keywords describe XML attributes (`attributes`), a keyed list of `<add key="…">` elements
(`identity: [/attributes/properties/key]`), a REG_EXPAND_SZ path (`expandable: true`) or a TOML local date
(`format: date`).

## What Skemata is, and is not

Skemata describes **structure**: members, types, ranges, item identity, how values are stored within the
format, and how to present them (titles, descriptions, labeled values, groups, translations).

Skemata does **not** describe where data lives (file paths, registry key paths), nor how data is created,
changed or removed. Tools that use Skemata schemas handle those concerns.

## Targets in Draft 01

| Target | Describes |
|---|---|
| `registry` | the values of one Windows Registry key |
| `ini-windows` | an INI file as read by the Windows profile API |
| `xml` | an XML 1.0 document |
| `json` | JSON, with or without comments |
| `sqlite` | rows of tables in a SQLite database |
| `yaml-1.2` | a YAML 1.2 document |
| `toml` | a TOML 1.0 / 1.1 document |
| `csv`, `csv-semicolon`, `csv-tab` | a CSV file with a header row, separated by commas, semicolons or tabs |
| `psd1` | a PowerShell data file |

## Reading the specification

| | |
|---|---|
| [1 Introduction](spec/01-introduction.md) | goals, non-goals, terminology, prior art |
| [2 Document structure](spec/02-document-structure.md) | file format, root keywords, references |
| [3 Data model](spec/03-data-model.md) | nodes, members, identity, attributes and content |
| [4 Keywords](spec/04-keywords.md) | every keyword and its rules |
| [5 Types and formats](spec/05-types-and-formats.md) | scalar types, formats, lexical forms |
| [6 UI annotations](spec/06-ui-annotations.md) | titles, labeled enums, groups |
| [7 Localization](spec/07-localization.md) | locale files |
| [8 Target profiles](spec/08-target-profiles.md) | keyword and type matrices per target |
| [9 Versioning and conformance](spec/09-versioning-and-conformance.md) | compatibility, conformance, meta-schemas |
| [Targets](spec/targets/) | one chapter per target |
| [Examples](spec/examples/) | worked examples, including invalid ones |

To write a schema, start with [chapter 3](spec/03-data-model.md), then read the chapter of your target and
its examples.

## Repository layout

```
spec/          the specification: core chapters, targets/, examples/
schemas/       meta-schemas for Skemata documents (generated) and their source definition
tools/         Skemata.MetaSchemas: generates and verifies the meta-schemas
decisions/     design decisions (architecture decision records)
```

## Roadmap

1. **Specification** (Draft 01)
2. **Meta-schemas** for editor support and schema validation ([schemas/](schemas/))
3. Conformance test suite
4. Reference validator and CLI in .NET

## Name

**Skemata** is a respelling of *schemata*, the plural of *schema*: a language for describing the structure of
many kinds of data.

## License

To be decided.
