# ADR 0025: `patternProperties` with at most one matching pattern

- Status: Accepted
- Date: 2026-10-08

## Context

Some objects hold dynamic members of different kinds, told apart by their names: Firefox's
`ExtensionSettings` has a default entry `"*"` and entries keyed by extension ID, and editor settings mix
plain keys with `[language]` overrides. `additionalProperties` can describe only one kind of dynamic member.
JSON Schema's `patternProperties` covers this, but in JSON Schema a name that matches several patterns must
satisfy all of their subschemas. That leaves a member without a single subschema, node kind and storage
mapping.

## Decision

`patternProperties` is part of Skemata with its JSON Schema meaning, tightened so that every member has
exactly one subschema:

- a name declared in `properties` must not match any pattern (checked on the schema);
- a member whose name matches more than one pattern makes the data invalid (checked on the data);
- `additionalProperties` describes the members that match neither a declared name nor a pattern.

Patterns follow the portability guidance for `pattern` (KW-20).

## Consequences

- Objects with several kinds of dynamic members can be described, and UIs know which fields to offer for a
  new member from its name.
- Schemas that rely on overlapping patterns cannot be described; they must make their patterns disjoint.
- SQLite databases keep declaring every table by name.
