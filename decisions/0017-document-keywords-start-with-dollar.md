# ADR 0017: Document keywords start with `$`

- Status: Accepted
- Date: 2026-10-08

## Context

JSON Schema marks the keywords of its core vocabulary with `$`: `$schema`, `$id`, `$ref`, `$defs`, `$anchor`,
`$comment`. They concern the schema document itself (dialect, identity, references, notes), not the data it
describes. Skemata's root keywords for the target and for the language of the inline texts were first named
`target` and `locale`, which made them look like keywords that describe data.

## Decision

Keywords that concern the schema document itself start with `$`. Skemata's document keywords are `$target`
(which target profile applies, like `$schema` decides which dialect applies) and `$locale` (the language of
the document's texts). Locale files use `$locale` for their language as well.

`groups` keeps its name: it describes how the data is presented, like `title`, not the document.

## Consequences

- The root of a schema reads as two clear parts: document keywords with `$`, data keywords without.
- `$` is not a vendor prefix (ADR 0003): it follows JSON Schema's own convention.
- A future JSON Schema core keyword with the same name is unlikely, but would be a conflict to resolve in a
  later Skemata version.
