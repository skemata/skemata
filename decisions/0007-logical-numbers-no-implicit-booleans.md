# ADR 0007: Logical numbers, no implicit booleans

- Status: Accepted
- Date: 2026-10-08

## Context

Many targets have no native type for some values: INI values and XML attributes are text, and the registry
has no boolean. A schema could either use the logical type (and define an encoding) or describe only the
physical type.

Numbers convert to and from text unambiguously with a defined lexical form. Booleans do not: stores encode
them as `1`/`0`, `yes`/`no`, `true`/`false`, `on`/`off` and more, and no encoding can be predicted.

## Decision

- **Numbers are logical.** `integer` and `number` may be used in text-based targets; values are stored in a
  canonical lexical form, so ranges validate. Where a target has both a native and a text form,
  `textual: true` selects the text form.
- **Booleans are never encoded implicitly.** `type: boolean` is allowed only in targets with a native
  boolean. Elsewhere, on/off values are labeled enums of their actual stored values.
- A later draft may allow a boolean with an explicit, author-declared pair of stored values. It will
  never have a default encoding.

## Consequences

- Range validation and numeric UI controls work in every target.
- On/off values in text-based targets and the registry show as two-value choices; UIs may render them as
  switches.
