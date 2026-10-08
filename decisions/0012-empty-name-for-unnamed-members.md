# ADR 0012: The empty name denotes an unnamed member

- Status: Accepted
- Date: 2026-10-08

## Context

Every registry key may have one unnamed value, shown as `(Default)`. The data model must represent it
without a format-specific keyword.

## Decision

The member with the **empty name** `""` is the node's unnamed member. It is allowed only where a target
profile needs it. In draft-01 that is only `registry`, where `""` is the `(Default)` value. All other
targets reject empty names (rule MOD-3).

## Consequences

- No extra keyword is needed; the `(Default)` value is declared like any other value.
- Formats that technically allow empty names (JSON, TOML) cannot describe such members in draft-01.
- XML text content is a different concept (the element's own text, with no members next to it) and uses the
  `content` keyword.
