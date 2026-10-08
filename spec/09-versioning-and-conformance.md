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
are in [`schemas/draft-01/`](../schemas/draft-01/), generated from
[`schemas/source/draft-01.yaml`](../schemas/source/draft-01.yaml) by `tools/Skemata.MetaSchemas`.

1. **One source, generated output.** The meta-schemas are generated from a single source definition of the
   keywords and the matrices in chapter 8. They are not maintained by hand.
2. **One meta-schema per target**, for example `https://skemata.github.io/skemata/draft-01/targets/registry`. Each one is
   self-contained and enforces that target's allowlist recursively, at every subschema position.
3. **A dispatching meta-schema** at the version URI (`https://skemata.github.io/skemata/draft-01/schema`) selects the
   per-target meta-schema by the value of `$target`.
4. **A locale meta-schema** at `https://skemata.github.io/skemata/draft-01/locale` checks locale files.
5. **Editor compatibility.** Generated meta-schemas use only plain `$ref` and `additionalProperties: false`
   (no `$dynamicRef`, no `unevaluatedProperties`), so that editors with partial 2020-12 support work too.
6. **Authored form.** Meta-schemas check schemas as written, not their normalized form
   ([4.11](04-keywords.md#411-implicit-rules-and-the-normalized-form)). Where `type` is written, they select
   the allowlist for that type. Where `type` is inferred, they check only the keywords allowed for any type,
   and the schema validator performs the type-specific checks (KW-17) on the normalized form.

### 9.4.1 Checks beyond meta-schemas

Some rules cannot be expressed in JSON Schema. A schema validator MUST check them separately:

| Rule | Check |
|---|---|
| MOD-2 | declared member names unique under the target's name comparison |
| KW-4, KW-6 | `$ref` resolves locally; every recursion can end |
| KW-7 | `$anchor` values unique |
| KW-9, KW-29 | names in `required` and `dependentRequired` declared in `properties` |
| KW-13 | identity references resolve through members and attributes to non-nullable scalars |
| KW-14, KW-15, KW-30 | `oneOf` form recognisable; labeled-enum choices disjoint, at most one range choice; exactly one union discriminator, with distinct values |
| KW-16, KW-17 | type determinable (written or inferred); value keywords match the normalized type |
| KW-18, KW-19, UI-1 | `enum`, `const`, `default`, `examples` and bounds valid against their subschema |
| KW-22, KW-23 | attributes are scalars; no members next to `content` |
| KW-27 | `prefixItems` not combined with `uniqueItems`, `identity` or `ordered` |
| KW-31 | `patternProperties` keys are valid patterns; no declared name matches a pattern |
| UI-5, UI-8 | `group` values refer to existing groups; `unit` values from the list |
| LOC-3 | locale file keys resolve |
| target rules | position-specific rules of each target chapter |

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
