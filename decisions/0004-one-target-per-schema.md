# ADR 0004: One schema describes exactly one target

- Status: Accepted
- Date: 2026-10-08

## Context

A schema could describe abstract structure that is mapped to several formats (as ASN.1 does), or the
structure of data in one concrete format. Data is almost always stored in one concrete format at a time,
and the details that matter (a REG_DWORD, an XML attribute, a TOML local date) are format-specific.

## Decision

Every schema declares exactly one target with the core keyword `$target`. Keywords stay generic; the target
determines which of them may be used and how they are stored.

## Consequences

- A schema states precisely how its data is stored; there are no ambiguous cross-format mappings.
- Data that exists in several formats (for example the same policy stored in the registry and in a JSON file)
  needs one schema per format.
- Validators know from `$target` which rules and mapping to apply.
