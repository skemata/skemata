# ADR 0013: Reference tooling in .NET

- Status: Accepted
- Date: 2026-10-08

## Context

The specification needs a reference implementation (schema validator, data validator, CLI) to prove that
the rules can be implemented and to give authors a working tool. The draft-01 targets are Windows-first
(ADR 0010).

## Decision

The reference tooling will be written in **.NET (C#)**. It is not part of milestone 1, which delivers the
specification only.

## Consequences

- Native access to the Windows Registry and the INI profile API, and mature libraries for JSON Schema,
  YAML, XML and SQLite.
- Implementations in other languages remain welcome; the specification does not depend on .NET.
