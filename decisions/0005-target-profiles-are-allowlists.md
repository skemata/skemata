# ADR 0005: Target profiles are allowlists

- Status: Accepted
- Date: 2026-10-08

## Context

With generic keywords (ADR 0003) and one target per schema (ADR 0004), each target must limit the usable
keywords and values to what it can represent. New targets will add new generic keywords over time.

A denylist ("all keywords except …") would silently make every new keyword valid for every existing target,
including targets that cannot represent it.

## Decision

Each target profile is an **allowlist** of keywords, `type` values and `format` values, kept in the matrices
of chapter 8. A new core keyword is valid for an existing target only after an explicit matrix entry.

## Consequences

- The meaning of existing schemas never changes when keywords are added.
- Adding a target means filling a whole matrix column deliberately.
- The matrices are the single source of truth for generated per-target meta-schemas.
