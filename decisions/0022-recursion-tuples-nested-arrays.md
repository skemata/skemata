# ADR 0022: Recursion, tuples and nested arrays

- Status: Accepted
- Date: 2026-10-08

## Context

Draft-01 excluded recursive schemas, tuples (`prefixItems`) and arrays of arrays, because settings files
rarely need them. With Skemata's scope widened to describing any data, they are real gaps: bookmark trees and
menus are recursive, GeoJSON positions are tuples inside nested arrays, and KDL, nginx and GVariant values have
positional arguments.

## Decision

- **Recursion:** `$ref` may form cycles, as long as every recursion can end: it passes through a member or
  attribute that may be absent, an array that may be empty, or a union with a branch outside the recursion,
  so that data stays finite. Loops of `$ref` alone stay invalid.
- **Tuples:** `prefixItems` is allowed, with its JSON Schema meaning. Implicitly, no items beyond the
  positions are allowed and every position is present, unless `items` or `minItems` say otherwise.
- **Nested arrays:** `items` may describe an array.

Tuples and nested arrays are allowed only in targets whose arrays can hold them: `json`, `yaml-1.2`, `toml`
and `psd1`.

## Consequences

- Trees, coordinate lists and positional argument lists can be described.
- UI generators must render recursive structures on demand rather than all at once.
- Targets such as XML, the registry and INI keep rejecting these constructs, because they have no
  representation for them.
