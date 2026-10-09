# 1. Introduction

## 1.1 Purpose

Data is stored in many formats: Windows Registry keys, INI files, XML and JSON documents, SQLite databases,
YAML and TOML files. To validate, document or display such data, a tool needs a precise description of it:
which values exist, what types and ranges they have, how the items of a list are identified, and how each
value is stored in its format.

Skemata is a schema language for exactly that. A Skemata schema **describes and validates
data of any kind in one storage format**. It is written in YAML or JSON and looks like JSON Schema.

## 1.2 Design goals

1. **Familiar.** Anyone who knows JSON Schema can read a Skemata schema. Keywords that exist in JSON Schema
   keep their meaning.
2. **Precise per format.** A schema states how values are stored in its format: a REG_DWORD, an XML
   attribute, the `(Default)` value of a key, a TOML local date. Nothing is left to guesswork.
3. **Generic.** New concepts get generic names, so the same keyword serves every format that needs the
   concept. Each format allows only what it can represent.
4. **Addressable.** Items of lists, rows of tables and entries of maps can be identified individually, so
   tools can work with one value without treating its neighbours as changed.
5. **Presentable.** Schemas carry titles, descriptions, labeled values, groups and translations, so
   documentation and UIs can be generated from them.
6. **Concise.** What follows from the structure is implied: types, titles derived from names, and the
   presence of identity values. Only what is not obvious needs to be written.
7. **Open.** Skemata is a public, vendor-neutral specification: no rule or text assumes a particular product
   that consumes schemas. Anyone can write schemas for any data.

## 1.3 Non-goals

Skemata describes structure. It does **not** describe:

- **Location:** which file, registry key path, hive or database a schema applies to. A registry schema
  describes "a key with these values", not `HKLM\SOFTWARE\…`.
- **Processing:** how data is created, changed or removed; when; and how conflicts are resolved.
- **Applicability:** which versions of the producing software, or which platforms, a schema applies to.
- **Secrets handling:** where sensitive values are stored. A schema can only mark them (`writeOnly`).
- **Syntax and formatting:** comments, whitespace, quoting style and member order where it carries no meaning.

These concerns belong to the tools that use Skemata schemas. They change at a different pace and are decided
by different people than the structure of the data; leaving them out lets one schema serve every location,
for example the same registry key under `HKLM` and `HKCU`.

## 1.4 Conventions

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED",
"NOT RECOMMENDED", "MAY" and "OPTIONAL" in this specification are to be interpreted as described in BCP 14
(RFC 2119, RFC 8174) when, and only when, they appear in all capitals.

Normative requirements carry rule identifiers in brackets, such as `[KW-13]`
([9.5](09-versioning-and-conformance.md#95-rule-identifiers)). Examples, notes and sections marked
"non-normative" are informative.

## 1.5 Terminology

| Term | Meaning |
|---|---|
| **schema** (schema document) | a YAML or JSON document that conforms to this specification |
| **subschema** | any schema object within a schema document, including the root |
| **target** | the storage format a schema describes, identified by a target name such as `registry` |
| **target profile** | the normative definition of a target: mapping, allowlist and rules ([chapter 8](08-target-profiles.md)) |
| **target data** | the actual data in the target format: a registry key, a file, a database |
| **data model** | Skemata's tree view of target data ([chapter 3](03-data-model.md)) |
| **node** | an element of the data model: object, array or scalar |
| **member** | a named child of an object node |
| **item** | a child of an array node |
| **processor** | any software that reads Skemata schemas |
| **schema validator** | a processor that checks schema documents against this specification |
| **data validator** | a processor that checks target data against a schema |
| **consumer** | any other software that uses schemas, such as a documentation or UI generator, or an editor |

## 1.6 Relation to JSON Schema

Skemata is **styled after** JSON Schema 2020-12, but it is its own language:

- It reuses JSON Schema keywords with the same meaning. It may tighten them, for example by allowing `format`
  values only from a closed set and checking them.
- It excludes keywords that make structure ambiguous, such as `allOf`, `anyOf`, `not` and `if`/`then`/`else`
  ([4.10](04-keywords.md#410-excluded-json-schema-keywords)).
- It adds keywords that JSON Schema lacks, with generic names: `$target`, `identity`, `ordered`, `attributes`,
  `textual`, `expandable` and others.
- Every schema has exactly one target, which limits the usable keywords and types. There is no
  format-independent schema: the details that matter, such as a REG_DWORD or an XML attribute, belong to one
  format, and mappings between formats would be ambiguous. Data stored in two formats needs two schemas.

As a result, a typical Skemata schema for a `json` target looks almost exactly like the equivalent JSON Schema.
Skemata schemas are nevertheless not JSON Schemas: their targets are mostly not JSON, so JSON Schema validators
cannot validate the data they describe.

## 1.7 Prior art

Skemata builds on ideas from several existing technologies:

| Technology | What Skemata takes from it |
|---|---|
| **JSON Schema 2020-12** | keyword names and meanings, `$ref`/`$defs`/`$anchor`, annotation keywords |
| **XML Schema (XSD)** | a rich scalar type library; identity constraints (`xs:key`) as a model for `identity` |
| **ASN.1** (ITU-T X.680, X.693 XER, X.697 JER) | the separation of an abstract structure from per-format encoding rules, which Skemata's target profiles mirror; encoding instructions (such as XER's `ATTRIBUTE`) as precedent for `attributes` and `content` |
| **Administrative templates** (ADMX/ADML) | registry values described for UIs; language resources in separate files |
| **OpenAPI 3.2** | the XML Object's `nodeType` (element, attribute, text) as precedent for `attributes` and `content`; format names from the OpenAPI Format Registry |
| **Kubernetes structural schemas** | a structural subset of JSON Schema; list types and list-map keys as precedent for `identity` and set semantics |
| **PowerShell DSC v3** | JSON Schema used to describe resources, with documents written in YAML or JSON |

None of them is adopted directly. XSD is centred on XML, so the concepts of other formats would end up in
`xs:appinfo`. ASN.1 is a notation of its own whose encodings cover binary, XML and JSON, but not maps, item
identity or UI annotations. JSON Schema extended with prefixed keywords (`x-…`, `xml:…`) would grow a
vocabulary per format. A new notation would need its own parsers and editor support. Skemata keeps the syntax
and meaning of JSON Schema and adds generic keywords instead (1.6).

## 1.8 How to read this specification

| Chapter | Content |
|---|---|
| [2 Document structure](02-document-structure.md) | file format, root keywords, references, literals |
| [3 Data model](03-data-model.md) | nodes, members, identity, attributes and content, absent/null/empty |
| [4 Keywords](04-keywords.md) | every keyword and its rules |
| [5 Types and formats](05-types-and-formats.md) | scalar types, formats, binary data, lexical forms |
| [6 UI annotations](06-ui-annotations.md) | titles, descriptions, defaults, labeled enums, groups |
| [7 Localization](07-localization.md) | source language and locale files |
| [8 Target profiles](08-target-profiles.md) | target names and the keyword and type matrices |
| [9 Versioning and conformance](09-versioning-and-conformance.md) | compatibility, conformance classes, meta-schemas |
| [targets/](targets/) | one chapter per target |
| [examples/](examples/) | worked examples per target, including invalid ones |

Readers who want to write schemas can start with chapter 3, the chapter of their target, and its examples.
