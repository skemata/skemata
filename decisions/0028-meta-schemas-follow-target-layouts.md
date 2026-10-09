# ADR 0028: Meta-schemas follow the layout of each target

- Status: Accepted
- Date: 2026-10-08

## Context

The first generated meta-schemas (ADR 0026) used one definition for every subschema of a target. That
definition had to allow every node kind and keyword that the target allows anywhere, so a `csv` schema could
declare a column of `type: object`, a `sqlite` table without `identity`, or an `ini-windows` key outside a
section. The target chapters forbid all of these (CSV-1, SQL-4, INI-1), and matrix 8.5 states where node
kinds may appear ("root only: the rows"), but the meta-schemas left these checks to a schema validator.
Editors offered keywords where they are not allowed, and some rules that need no more than one subschema,
such as KW-21 or UI-3, were checked nowhere in the documented architecture.

## Decision

- The source definition describes each target's **layout**: its subschema positions (for example the root,
  records and fields of `csv`), the node kinds allowed at each, the positions of their members and items, and
  keywords that a position forbids or requires. Each target meta-schema has a definition per position, so it
  checks node kinds and keywords where a subschema is written, also where `type` is inferred.
- `$defs` entries can be referenced from any position. The meta-schemas check them against the rules of any
  position, and a schema validator checks the rules of the position where they are referenced (spec 9.4.1).
- Rules between the keywords of one subschema (for example KW-21, KW-27 and UI-3) are part of the source
  definition and of the meta-schemas.
- The generator compares the root of each layout with table 8.3. `verify` fails if an invalid example is not
  caught although spec 9.4.1 does not leave its rule to a schema validator.

## Consequences

- Most invalid examples are caught by the meta-schemas. Those that are not break rules listed in 9.4.1.
- The meta-schemas are larger, with one definition per position.
- A new target declares its layout together with its matrix columns.
