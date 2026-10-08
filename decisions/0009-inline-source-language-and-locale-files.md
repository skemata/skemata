# ADR 0009: Inline source language plus locale files

- Status: Accepted
- Date: 2026-10-08

## Context

UIs generated from schemas need titles and descriptions in several languages. JSON Schema's `title` and
`description` are plain strings, so inline language maps (`title: {en: …, de: …}`) would change their
meaning. Message keys (`title: "@key"`) would make schemas unreadable without their resource files.

## Decision

- Schemas contain their texts inline in one **source language**, declared with `$locale` (default `en`).
- Translations are separate **locale files** keyed by location in the schema (JSON Pointer or `$anchor`),
  following the ADMX/ADML model. Enum labels (`oneOf` branches) and group titles are included.
- Consumers fall back per text: exact locale, then shorter tag, then source text.

## Consequences

- Schemas stay readable, and anyone can add a language without touching the schema.
- Pointer keys break when schemas are restructured; authors should use `$anchor` for stable keys.
- Schemas with locale files need an `$id`.
