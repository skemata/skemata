# ADR 0003: JSON Schema style, own language

- Status: Accepted
- Date: 2026-10-08

## Context

Skemata needs a base syntax that schema authors already know and that can describe the widest range of formats.
A custom grammar (DSL) was ruled out early: it requires a parser, formatter and editor support of its own,
and it raises the entry barrier for authors. The following existing technologies were evaluated:

- **XML Schema (XSD)** has the richest scalar type library and identity constraints (`xs:key`). However, it
  is XML-centric: JSON maps, registry values and SQLite rows map awkwardly. Everything specific to other
  formats would end up in `xs:appinfo` annotations, which would be a new language hidden in annotations.
- **ASN.1** separates abstract structure from per-format encoding rules (BER/DER/PER, XER for XML, JER for
  JSON), much like Skemata's target profiles. But its notation is its own grammar (a DSL), standard encodings
  exist only for binary, XML and JSON, and it lacks maps with dynamic keys, item identity for merging,
  and UI or translation annotations.
- **JSON Schema with prefixed extensions** (`x-…` keywords, format-named keywords such as `xml:`) would
  reuse JSON Schema directly. But prefixes and format-named keywords produce a vocabulary that grows per
  format and is hard to read.

## Decision

Skemata is its **own language in the style of JSON Schema 2020-12**:

1. Keywords that exist in JSON Schema keep their meaning. Skemata may tighten them (for example a closed,
   asserted set of `format` values), but never changes what they mean.
2. New concepts get **generic, unprefixed names** that describe the concept, never a format: `attributes`
   rather than `xml-attributes`, `expandable` rather than `reg-expand-sz`.
3. JSON Schema keywords that make structure ambiguous (`allOf`, `anyOf`, `not`, `if`/`then`/`else`, …) are
   excluded.
4. Schemas are YAML or JSON documents, so existing parsers and editors work.

## Consequences

- JSON Schema users can read Skemata schemas immediately; a schema for a JSON target looks almost like a JSON
  Schema.
- The Skemata meta-schemas can themselves be written in JSON Schema, which gives editor support without a
  custom language server.
- Skemata schemas are not JSON Schemas: JSON Schema validators cannot validate target data in other formats,
  and Skemata-specific keywords are unknown to them.
- Ideas from XSD (type library, identity constraints) and ASN.1 (structure vs. encoding) are adopted as
  concepts.
