# ADR 0030: Meta-schemas infer types and offer only valid keywords

- Status: Accepted
- Date: 2026-10-09

## Context

Schema authors use the meta-schemas in their editor for completion and validation. Each position of a
meta-schema listed every keyword of every node kind in one `properties` map and forbade keywords per written
`type` with `if`/`then`. Editors offer the union of the keys named by all matching schemas and never remove a
key because a branch sets it to `false`, so after `type: string` they still offered `minimum`, `items` and
`properties`. Keywords whose value schema was `true` (`const`, `default`) were not offered at all. Where `type`
was not written, the meta-schemas did not infer it (ADR 0019), so KW-16 and the type checks on inferred types
were left to a schema validator.

## Decision

- Every position is an `anyOf` of closed alternatives: one for `$ref` with its siblings, and for each node
  kind one or more alternatives that list only keywords allowed together. Rules of the source definition that
  only name keywords that must be absent (for example KW-21, KW-27, the forms of `oneOf`) split a kind into
  several alternatives instead of forbidding keywords with `if`/`then`.
- Each kind alternative requires `type` or a keyword from which spec 4.11.1 infers the kind. A subschema
  whose type can be neither read nor inferred matches no alternative (KW-16).
- Literal values are restricted to the type of their alternative, `null` only next to a nullable type, and
  numbers to the range of their format (spec 5.2, compared with the source definition by the generator).
- Only keywords of JSON Schema 2020-12 are used; no editor-specific keywords and no texts written for the
  meta-schemas.
- A rule that restricts the items of `oneOf` with `properties` would make editors offer those keys in every
  choice, so the values of choices are not restricted beyond their type; a schema validator checks them.

## Consequences

- Editors offer only the keywords that are valid at the position, for the type that is written or inferred,
  and together with the keywords already written.
- KW-16 and the type checks on inferred types move from the schema validator to the meta-schemas (spec 9.4,
  9.4.1). The "authored form" consequence of ADR 0019 no longer applies.
- The meta-schemas are larger: several alternatives per position. Identical alternatives and rule lists are
  emitted once.
