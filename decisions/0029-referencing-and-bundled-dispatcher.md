# ADR 0029: A referencing dispatcher and a bundled copy

- Status: Accepted
- Date: 2026-10-08

## Context

ADR 0026 made the dispatching meta-schema at `…/draft-01/schema` self-contained: it bundled the definitions
of every target, so that editors needed a single file and worked before the site was published. With one
definition per position (ADR 0028), the bundle grew to more than 500 KB, all of it a copy of the per-target
meta-schemas, and readers found the same rules twice under different names (`csv.field` and `field`).
The site is now published, so the per-target meta-schemas can be downloaded by their `$id`.

## Decision

- The dispatcher at `…/draft-01/schema` refers to the per-target meta-schemas by their `$id`, relative to
  its own (`targets/<target>`). It contains no copies.
- A bundled copy, `…/draft-01/schema.bundled`, contains the definitions of all targets, as the dispatcher
  did before. It is meant for tools that load a single file or work without network access, and for local
  modelines.
- Documents keep `$schema: …/draft-01/schema`. Both dispatchers accept exactly that value.
- `verify` resolves the references from a local registry of the generated files and fails if the
  dispatcher, the bundled copy and a target's meta-schema disagree on any example.

## Consequences

- The dispatcher is small, and each rule is published once, in its target's meta-schema.
- Editors that follow `$schema` download the dispatcher and the target meta-schemas.
- A local modeline must name `schema.bundled.json` (or one target's file), because the references in
  `schema.json` lead to the published site.
