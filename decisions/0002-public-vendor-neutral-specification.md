# ADR 0002: Public, vendor-neutral specification

- Status: Accepted
- Date: 2026-10-08

## Context

Schemas are most useful when software vendors and the community write them for their own and others'
software. That requires a specification anyone can read, implement and rely on.

## Decision

Skemata is developed as a public specification:

- written in English, using RFC 2119 / RFC 8174 key words and numbered rule identifiers;
- versioned, with an explicit compatibility promise from 1.0 on;
- vendor-neutral: no rule, example or text assumes a particular product or tool that consumes Skemata.

## Consequences

- Examples use publicly documented software or fictitious applications and are marked illustrative.
- Rule identifiers let validators report violations precisely and let discussions refer to rules.
- A license for the specification text and a permanent domain for `$schema` URIs must be chosen before 1.0.
