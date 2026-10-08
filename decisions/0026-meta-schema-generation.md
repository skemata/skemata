# ADR 0026: Meta-schemas generated from a YAML source checked against the spec

- Status: Accepted
- Date: 2026-10-08

## Context

Spec 9.4 requires the meta-schemas to be generated from a single source definition of the keywords and the
chapter 8 matrices, not maintained by hand. The matrices themselves are written for people and must stay in
the spec.

## Decision

- The source definition is a YAML file, `schemas/source/draft-01.yaml`. It holds the keyword value shapes,
  the applicable types, the special positions, and the rows of the chapter 8 matrices.
- The generator, `tools/Skemata.MetaSchemas`, is a .NET console project (ADR 0013). Before generating, it parses
  the matrices in `spec/08-target-profiles.md` and fails if any row differs from the source, so spec and
  meta-schemas cannot drift apart.
- It writes one self-contained meta-schema per target, a dispatcher that bundles all targets and selects one
  by `$target`, and a locale meta-schema. The generated files are committed, so users do not need to build.
- `$id` values originally used a placeholder domain until a permanent home was chosen (see ADR 0027).

## Consequences

- Changing a matrix means changing the spec and the source together; the build rejects one without the other.
- A `verify` command detects hand-edited or outdated files and validates every worked example.
- Editors need a modeline to use the local files until the meta-schemas are published at their `$id` URLs.
