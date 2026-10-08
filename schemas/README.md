# Meta-schemas

JSON Schema 2020-12 meta-schemas for Skemata documents, as described in
[spec 9.4](../spec/09-versioning-and-conformance.md#94-meta-schema-architecture). Editors use them for
completion and validation of `*.skemata.yaml` files, and validators use them for the structural checks.

| File | `$id` | Checks |
|---|---|---|
| [draft-01/schema.json](draft-01/schema.json) | `https://skemata.github.io/skemata/draft-01/schema` | any Skemata schema; selects the target's rules by `$target` (bundles all targets) |
| [draft-01/targets/](draft-01/targets/) `<target>.json` | `https://skemata.github.io/skemata/draft-01/targets/<target>` | a schema for one target |
| [draft-01/locale.json](draft-01/locale.json) | `https://skemata.github.io/skemata/draft-01/locale` | a locale file |

The files in `draft-01/` are **generated**. Do not edit them; edit
[source/draft-01.yaml](source/draft-01.yaml) and regenerate.

## What the meta-schemas check

- Only known keywords, and only those the target allows (KW-1, KW-2, 8.4), in their allowed positions
  (root-only keywords, `$ref` siblings, labeled-enum choices, union branches, `attributes`, `propertyNames`).
- The shape of every keyword's value, including `type`, `format` and `unit` values allowed by the target (8.5).
- Where `type` is written: keywords that do not match it (KW-17), `format` values of other types, and nested
  arrays in targets that do not allow them.
- Member names that the target forbids: the empty name outside the registry (MOD-3), and XML names (XML-3).

Rules that need more than the shape of one subschema are left to a schema validator (spec 9.4.1): type
inference, identity references, recursion, name collisions, disjoint choices, and position-specific target
rules such as REG-2 or SQL-4.

## Generating and verifying

The generator is a .NET 10 console application:

```
dotnet run --project tools/Skemata.MetaSchemas -- generate   # writes schemas/draft-01/
dotnet run --project tools/Skemata.MetaSchemas -- verify     # fails on outdated files or rejected examples
```

Both commands first compare the source definition with the matrices in
[spec/08-target-profiles.md](../spec/08-target-profiles.md) and stop if they disagree, so the spec and the
meta-schemas cannot drift apart. `verify` also validates every worked example: valid examples must pass, and
invalid ones are reported as caught by the meta-schemas or left to a schema validator.

## Using the meta-schemas in an editor

The `$id` values are URLs under the project's GitHub Pages site, `https://skemata.github.io/skemata/`, where the
meta-schemas are published (see `.github/workflows/pages.yml`). The Red Hat YAML extension for VS Code uses a
document's own `$schema` before any workspace setting, so schemas that declare `$schema` get completion and
validation once the site is live. To use the local files instead (offline, or before the site is published),
add a modeline as the first line of a schema file. For a schema in `spec/examples/json/`:

```yaml
# yaml-language-server: $schema=../../../schemas/draft-01/schema.json
```

The path is relative to the schema file. The modeline takes precedence over `$schema`.
