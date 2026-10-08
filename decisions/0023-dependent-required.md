# ADR 0023: `dependentRequired`

- Status: Accepted
- Date: 2026-10-08

## Context

Draft-01 excluded JSON Schema's conditional keywords. The simplest of them, `dependentRequired`, covers a
frequent case without making structure ambiguous: "if this member is present, those members must be present
too", for example a locked home page that needs a URL.

## Decision

`dependentRequired` is part of Skemata with its JSON Schema meaning. The names must be declared in `properties`;
inside `attributes` they refer to attributes. It is allowed in every target except on SQLite rows, where every
column is always present.

`if`, `then`, `else` and `dependentSchemas` remain excluded.

## Consequences

- Simple dependencies between members can be validated and shown in UIs (fields become mandatory once their
  trigger is set).
- The node kind of every subschema is still determined without looking at data.
