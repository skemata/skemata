# ADR 0020: Labeled enums have value choices and one range choice

- Status: Accepted
- Date: 2026-10-08

## Context

Two common patterns could not be described with labeled enums of single constants:

- Software often accepts several spellings for the same value: `yes`, `true`, `on` and `1` in INI and
  systemd files, or `True` and `true` in XML.
- A numeric setting often has a special value next to a range, such as "0 = let the browser decide,
  otherwise the cache size in bytes".

ADR 0007 also left a path open for on/off values in targets without booleans, with no assumed encoding.

## Decision

A labeled-enum branch is a **choice**:

- A **value choice** has `const`, or `enum` with several spellings of the same value. The first spelling is
  canonical: tools write it.
- A **range choice** has no fixed value, only constraints such as `minimum`. A labeled enum has at most one.

Choices must be disjoint. All of this remains valid JSON Schema (`oneOf` with `const`, `enum` and constraint
branches).

## Consequences

- On/off values in text formats are declared with all their accepted spellings. Skemata still never assumes an
  encoding; the author states it.
- Special values next to ranges get titles, and UIs can render them as "choice + input field".
- Validators must check that choices do not overlap.
