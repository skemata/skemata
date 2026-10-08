# ADR 0006: Format variants are separate targets

- Status: Accepted
- Date: 2026-10-08

## Context

Several formats come in variants: INI files differ between implementations, and YAML 1.1 and 1.2 resolve
values differently. A variant could be selected with a separate keyword (`$target: ini` plus
`dialect: windows`) or with its own target name.

## Decision

Every variant that matters is a **separate target name** (`ini-windows`, `yaml-1.2`). There is no dialect
keyword. A variant gets its own name when its data model, value rules or name rules differ. Variants that
only change syntax keep the name: `toml` covers TOML 1.0.0 and 1.1.0, and `json` covers JSON with comments
and trailing commas.

## Consequences

- Each target has one complete, self-contained profile and one meta-schema.
- Syntax-only variants do not multiply targets; their extra syntax is accepted implicitly.
- Where variants do differ, the list of targets grows; shared rules are stated once in the core chapters and
  referenced.
