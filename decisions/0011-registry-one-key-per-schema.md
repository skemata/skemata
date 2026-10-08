# ADR 0011: Registry: one key per schema

- Status: Accepted
- Date: 2026-10-08

## Context

A registry key holds values and subkeys in separate namespaces: a subkey and a value may have the same name.
Describing a key tree in one schema would mix both in one object and create name clashes. Deep trees also
make schemas hard to read and hard to bind to locations.

## Decision

A `registry` schema describes **exactly one key**: its root is the key, its members are the key's values.
Objects appear only at the root. Subkeys are described by schemas of their own; the consuming tool binds
schemas to key paths.

## Consequences

- No subkey/value name clashes; every registry schema is flat.
- Data spread over several keys needs several schemas.
- Lists stored as subkeys with numbered values are schemas of their own (ADR 0014).
