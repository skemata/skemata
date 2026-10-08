# ADR 0015: `ini-windows` has no global keys

- Status: Accepted
- Date: 2026-10-08

## Context

Some INI files contain keys before the first section ("global keys"). An earlier plan mapped root scalars
to such keys. The `ini-windows` target, however, follows the Windows profile API, and that API addresses
every key through a section name. Keys outside sections cannot be read or written through it.

## Decision

In `ini-windows`, every member of the root must be a section (rule INI-1). Root scalars are rejected.
An INI variant that supports global keys would be a separate target (ADR 0006), which may map root scalars
to them.

## Consequences

- `ini-windows` schemas match what the Windows API can actually access.
- Files whose applications read global keys with their own parsers need a different INI target in a later
  draft.
