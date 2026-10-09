# Meta-schemas

JSON Schema 2020-12 meta-schemas for Skemata documents, as described in
[spec 9.4](../spec/09-versioning-and-conformance.md#94-meta-schema-architecture). Editors use them for
completion and validation of `*.skemata.yaml` files, and validators use them for the structural checks.

| File | `$id` | Checks |
|---|---|---|
| [draft-01/schema.json](draft-01/schema.json) | `https://skemata.github.io/skemata/draft-01/schema` | any Skemata schema; selects the target's meta-schema by `$target` and refers to it by its `$id` |
| [draft-01/schema.bundled.json](draft-01/schema.bundled.json) | `https://skemata.github.io/skemata/draft-01/schema.bundled` | the same, with all target meta-schemas included: one file, no further downloads |
| [draft-01/targets/](draft-01/targets/) `<target>.json` | `https://skemata.github.io/skemata/draft-01/targets/<target>` | a schema for one target |
| [draft-01/locale.json](draft-01/locale.json) | `https://skemata.github.io/skemata/draft-01/locale` | a locale file |

The files in `draft-01/` are **generated**. Do not edit them; edit
[source/draft-01.yaml](source/draft-01.yaml) and regenerate.

## What the meta-schemas check

- Only known keywords, and only those the target allows (KW-1, KW-2, 8.4), in their allowed positions
  (root-only keywords, `$ref` siblings, labeled-enum choices, union branches, `attributes`, `propertyNames`).
- The target's structure (spec 9.4 item 7): each position allows only its node kinds and their keywords. In
  a `csv` schema, for example, the root is an array of records and a record's members are scalars, so
  `type: object` or `properties` in a column is rejected even where `type` is not written. Positions also
  forbid or require keywords, such as `required` on `sqlite` rows (SQL-3) or `identity` on tables (SQL-4).
- The type of every subschema (spec 9.4 item 6): `type`, or a keyword that implies it as in spec 4.11.1
  (KW-16), and only keywords of that type, whether `type` is written or inferred (KW-17, TYP-7, REG-5). The
  form of `oneOf` follows the type: choices for scalars (KW-14), union branches for objects (KW-15, KW-30).
- The shape of every keyword's value, including `type`, `format` and `unit` values allowed by the target (8.5).
- Literal values: `const`, `enum`, `default` and `examples` of the subschema's type, `null` only next to a
  nullable type, numbers within the range of the format (TYP-1, TYP-2, KW-19, REG-4), binary values in base64
  (TYP-5).
- Keywords that exclude or require each other (KW-11 to KW-15, KW-21, KW-23, KW-25 to KW-27, UI-3).
- Member names that the target forbids: the empty name outside the registry (MOD-3), XML names (XML-3) and
  INI names (INI-4).

Rules that need more than the shape of the schema as written are left to a schema validator (spec 9.4.1):
identity references, recursion, name collisions, disjoint choices, literals against `pattern` and similar
constraints, and the rules of the position where a `$defs` entry is referenced. The meta-schemas check
`$defs` entries against the rules of any position, because they cannot know where an entry is used.

### How the meta-schemas are built

Every position of a target (`root`, `schema`, and the positions of the layout in the source definition, such
as `record` and `field` for `csv`) is an `anyOf` of closed alternatives: `<position>.ref` for a `$ref` with its
siblings, and `<position>.<type>` (numbered where keywords that exclude each other split a type) listing only
the keywords allowed together. Editors offer the keywords of the alternatives that still match, so after
`type: string` they offer only string keywords, and after `prefixItems` no `uniqueItems`. Choices of a labeled
enum are `choice.<type>`, union branches `unionBranch`. Rules shared by several alternatives are in
`<alternative>.rules`; each rule names its rule IDs in `$comment`. Identical alternatives are emitted once.

## Generating and verifying

The generator is a .NET 10 console application:

```
dotnet run --project tools/Skemata.MetaSchemas -- generate   # writes schemas/draft-01/
dotnet run --project tools/Skemata.MetaSchemas -- verify     # fails on outdated files or rejected examples
```

Both commands first compare the source definition with the matrices in
[spec/08-target-profiles.md](../spec/08-target-profiles.md), and the root of each layout with table 8.3, and
stop if they disagree, so the matrices and the meta-schemas cannot drift apart. The rest of the source
definition (the positions below the root, name patterns, and the rules between keywords) is not compared
with the target chapters. The worked examples guard it instead: `verify` validates every example, valid ones
must pass, and invalid ones must be caught by the meta-schemas unless spec 9.4.1 leaves the rule they break
(the start of their file name) to a schema validator.

## Using the meta-schemas in an editor

The `$id` values are URLs under the project's GitHub Pages site, `https://skemata.github.io/skemata/`, where the
meta-schemas are published (see `.github/workflows/pages.yml`). The Red Hat YAML extension for VS Code uses a
document's own `$schema` before any workspace setting, so schemas that declare `$schema` get completion and
validation once the site is live. The editor then downloads `schema` and the target meta-schemas it refers to.

To use the local files instead (offline, or to try changes before they are published), add a modeline with
the bundled meta-schema as the first line of a schema file. For a schema in `spec/examples/json/`:

```yaml
# yaml-language-server: $schema=../../../schemas/draft-01/schema.bundled.json
```

The path is relative to the schema file. The modeline takes precedence over `$schema`. Use the bundled file
here, not `schema.json`: the references in `schema.json` lead to the published target meta-schemas, not to the
local files. A modeline can also name one target's meta-schema directly, such as
`../../../schemas/draft-01/targets/json.json`.
