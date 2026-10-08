# ADR 0010: Windows-style targets first, open target set

- Status: Accepted
- Date: 2026-10-08

## Context

Data is stored in many formats across platforms. The first draft has to cover a useful set without
committing the core to one platform.

## Decision

Draft 01 covers formats common on Windows: `registry`, `ini-windows`, `xml`, `json` and `sqlite`, plus
`yaml-1.2` and `toml`. The core data model and keywords must not contain anything that blocks other
platforms. Candidates such as `plist`, `dconf` or Java `.properties` must be addable as new targets without
core changes.

## Consequences

- Platform-specific behaviour lives in target chapters only.
- Each new target is checked against the core data model; a missing concept leads to a new generic keyword
  (ADR 0003, ADR 0005).
