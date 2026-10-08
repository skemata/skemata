# ADR 0014: Registry: a root array is a numbered-value list

- Status: Accepted
- Date: 2026-10-08

## Context

Applications often store a list in a registry key of its own, with one value per item named `1`, `2`, `3`, …
(browser policies such as `ExtensionInstallForcelist`). With one key per schema (ADR 0011), such a key gets
its own schema. It could be described as an object with a `propertyNames` pattern, but that loses list
semantics: UIs would show a name/value table instead of a list.

## Decision

The root of a `registry` schema may be an array. Item *n* is the value named by the decimal number *n*,
starting at 1. When reading, the numbered values must form a contiguous sequence; other values are
undescribed content. No new keyword is needed.

## Consequences

- Numbered-value lists keep list semantics: `uniqueItems`, `ordered`, `minItems`, list UIs.
- Lists with other naming schemes (zero-based, padded) must be described as objects in draft-01.
