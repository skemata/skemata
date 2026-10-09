# ADR 0019: Implicit rules and the normalized form

- Status: Accepted; the consequence on the authored form is superseded by ADR 0030
- Date: 2026-10-08

## Context

Early example schemas repeated what their structure already said: `type: string` next to `format: uri`,
identity members listed again in `required`, titles that only re-spelled member names, and `format: uint32`
on nearly every registry integer. Repetition makes schemas longer and gives authors more ways to make mistakes.

## Decision

Skemata infers what follows from the structure. Every implicit rule is defined exactly, all processors apply it
in the same way, and the result is the **normalized form** of the schema, which UIs and data validators work
on. A written keyword always takes precedence over an implied one.

Draft-01 adopts these rules:

- **Presence by structure:** identity values, union discriminators and the XML document element are
  required. This is a constraint of `identity` and of unions; it does not change shared subschemas.
- **Type inference:** `type` may be omitted when other keywords determine it (`properties` → object,
  `items` → array, `format` → its type, uniform `const`/`enum` values → their type, and so on).
- **Derived titles:** a missing `title` of a member, attribute or group is derived from its name by a
  normative algorithm (acronym-aware; targets name their unnamed member, such as the registry `(Default)`).
- **Registry integers:** `type: integer` without `format` is `uint32`, stored as REG_DWORD.
- **Closed tuples:** next to `prefixItems`, `items: false` and a `minItems` equal to the number of positions
  are implied.

## Consequences

- Schemas become noticeably shorter, especially for flat stores such as the registry and INI files.
- Type inference tightens JSON Schema, where a missing `type` accepts everything; this is allowed by the
  "same name, same meaning" principle (ADR 0003).
- Generated meta-schemas validate the authored form; type-specific checks for subschemas without `type` move
  to the schema validator.
- Derived titles need no translation in the schema; locale files can still translate them.
