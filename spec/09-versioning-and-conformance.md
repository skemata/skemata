# 9. Versioning and conformance

## 9.1 Versions of this specification

Skemata is published as numbered drafts (`draft-01`, `draft-02`, …) until version 1.0. Each version has its own
`$schema` URI ([2.3](02-document-structure.md#23-version-uris)). Drafts may change incompatibly.

From version 1.0 on:

- **Minor versions** (1.1, 1.2, …) only add: new targets, new formats, new keywords, and new ✓ entries in
  the matrices of [chapter 8](08-target-profiles.md). A schema that is valid under 1.x stays valid, with the
  same meaning, under every later 1.y.
- **Major versions** (2.0) may change or remove rules. They get a new `$schema` URI, so existing schemas keep
  their meaning.

Because target profiles are allowlists, adding a keyword never changes the meaning of existing schemas.
Existing targets gain the keyword only through an explicit new matrix entry.

## 9.2 Versioning schemas (guidance for authors)

- Identify each schema with a stable `$id`. If an incompatible change is needed, publish a new `$id`, for
  example with a version segment: `…/policies/v2`.
- Compatible changes include adding optional members, adding enum values, widening ranges and adding texts.
- Incompatible changes include removing members, narrowing types or ranges, changing identity, renaming
  members, and making members required.
- Mark members and enum values that are being phased out with `deprecated: true` before removing them.

Which versions of the producing software a schema applies to is outside Skemata's scope.

## 9.3 Conformance classes

### Schema documents

**[CNF-1]** A conforming schema document satisfies every MUST-level rule of chapters 2–8 and of its target's
chapter.

### Schema validators

A schema validator checks schema documents.

**[CNF-2]** A schema validator MUST accept every conforming schema document and MUST reject every
non-conforming one for the versions and targets it claims to support. It SHOULD report the IDs of the
violated rules.

The repository's [conformance test suite](../tests/) contains schema documents that a schema validator must
accept or reject, with the rules each one breaks.

### Data validators

A data validator checks target data against a schema: it reads the data, maps it to the data model as
defined by the target chapter, and applies the keywords.

**[CNF-3]** A data validator MUST state which targets it supports. For each, it MUST implement the target
chapter's mapping and rules, and MUST apply reused JSON Schema keywords with their JSON Schema 2020-12
semantics on the mapped data.

### Consumers

Consumers are all other tools that use schemas, such as documentation and UI generators, editors, and tools
that process target data.

**[CNF-4]** Consumers MUST NOT show `$comment` to end users, MUST honour
[LOC-6](07-localization.md#74-resolution)
when they show translated texts, and SHOULD follow the SHOULD-level guidance for annotations
([chapter 6](06-ui-annotations.md)). Consumers that modify target data SHOULD preserve content that the schema
does not describe ([3.3.1](03-data-model.md#331-open-content)).

## 9.4 Meta-schema architecture

The checks in chapters 2–8 are published as JSON Schema 2020-12 **meta-schemas**, so that editors can
offer completion and validation for Skemata documents, and validators can perform the structural checks with
existing JSON Schema libraries. This section fixes their architecture. In this repository, the meta-schemas
are generated from [`schemas/source/draft-01.yaml`](../schemas/source/draft-01.yaml) by
`tools/Skemata.MetaSchemas` and published at their URIs; the generated files are not committed.

1. **One source, generated output.** The meta-schemas are generated from a single source definition of the
   keywords and the matrices in chapter 8. They are not maintained by hand.
2. **One meta-schema per column of the matrices**, for example
   `https://skemata.github.io/skemata/draft-01/targets/registry`. Targets that share a column share its
   meta-schema: `…/targets/csv` serves `csv`, `csv-semicolon` and `csv-tab`. Each one is self-contained and
   enforces the allowlist recursively, at every subschema position.
3. **A dispatching meta-schema** at the version URI (`https://skemata.github.io/skemata/draft-01/schema`) selects the
   per-target meta-schema by the value of `$target`. It refers to the per-target meta-schemas by their URIs.
   A **bundled** copy at `https://skemata.github.io/skemata/draft-01/schema.bundled` contains all of them, for
   tools that load a single file or work without network access. Both accept the same documents.
4. **A locale meta-schema** at `https://skemata.github.io/skemata/draft-01/locale` checks locale files.
5. **Editor compatibility.** Generated meta-schemas use only keywords of JSON Schema 2020-12, no
   editor-specific extensions, and of those only plain `$ref` and `additionalProperties: false` to close
   objects (no `$dynamicRef`, no `unevaluatedProperties`), so that editors with partial 2020-12 support work
   too.
6. **Type inference.** Meta-schemas check schemas as written and infer types as
   [4.11.1](04-keywords.md#4111-type-inference) does. At every position, a subschema must match one of a set of
   closed alternatives: one for `$ref` (KW-5), and for each node kind allowed there, alternatives that list only
   the keywords allowed together for that kind and require `type` or a keyword that implies the kind (KW-16,
   KW-17). Keywords that exclude each other, such as `prefixItems` and `uniqueItems`, are in separate
   alternatives. Editors that offer the keywords of the alternatives that still match therefore offer only
   keywords that are valid together. The other implicit rules of 4.11 are applied by the schema validator.
7. **Positions.** A target's chapter fixes which node kinds may appear where: the root of a `csv` schema is an
   array of records, and the members of a record are scalars. Each per-target meta-schema has a definition for
   every position with rules of its own (for example the root, records and fields of `csv`, or the tables,
   rows and columns of `sqlite`) and allows there only the node kinds, keywords and member names valid at that
   position. A subschema in `$defs` can be referenced from any position, so the meta-schemas check it against
   the rules of any position (9.4.1).

### 9.4.1 Checks beyond meta-schemas

Some rules cannot be expressed in JSON Schema. A schema validator MUST check them separately:

| Rule | Check |
|---|---|
| DOC-1, DOC-2, DOC-3 | the document is JSON or YAML 1.2 without tags, anchors, aliases or merge keys, and quotes strings that YAML 1.1 reads differently (checks on the text, before it is read as data) |
| MOD-2 | declared member names unique under the target's name comparison |
| KW-4, KW-6 | `$ref` resolves locally; every recursion can end |
| KW-7 | `$anchor` values unique |
| KW-9, KW-29 | names in `required` and `dependentRequired` declared in `properties` |
| KW-13 | identity references resolve through members and attributes to non-nullable scalars |
| KW-14, KW-15 | labeled-enum choices disjoint; exactly one union discriminator, with distinct values |
| KW-17 | annotations next to `$ref`, such as `unit`, match the type of the referenced subschema |
| KW-18, KW-19, UI-1, INI-3 | `enum`, `const`, `default`, `examples` and the values of choices meet the other constraints of their subschema (`pattern`, lengths, bounds, string formats, and the implicit constraints of `ini-windows` strings); choice values and bounds lie within the range of the format |
| KW-31 | `patternProperties` keys are valid patterns; no declared name matches a pattern |
| UI-5, KW-24 | `group` values refer to existing groups; `group` and `namespace` only on values of `properties` |
| LOC-3 | locale file keys resolve |

The meta-schemas check the rules of each position (9.4 item 7) where a subschema is written in place. A
subschema in `$defs` can be referenced from any position, so for every `$ref`, a schema validator MUST check
that the referenced subschema meets the rules of the position where it is used: the node kinds and keywords
that the target chapter allows there (for example CSV-1, INI-1, REG-2, SQL-2 to SQL-4, XML-1), nested arrays
(KW-11), the items of sets (KW-12), and attributes and content (KW-22, KW-23).

## 9.5 Rule identifiers

Normative requirements carry identifiers such as `[KW-13]`. Identifiers are stable within a version and are
never reused for a different rule. Numbers reflect the order in which rules were added, so they do not
always follow the document order. Prefixes:

| Prefix | Chapter |
|---|---|
| DOC | [2 Document structure](02-document-structure.md) |
| MOD | [3 Data model](03-data-model.md) |
| KW | [4 Keywords](04-keywords.md) |
| TYP | [5 Types and formats](05-types-and-formats.md) |
| UI | [6 UI annotations](06-ui-annotations.md) |
| LOC | [7 Localization](07-localization.md) |
| TGT | [8 Target profiles](08-target-profiles.md) |
| CNF | 9 Versioning and conformance |
| REG, INI, XML, JSN, YML, TML, SQL, CSV, PSD | [target chapters](targets/) |
