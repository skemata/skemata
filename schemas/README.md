# Meta-schemas

JSON Schema 2020-12 meta-schemas for Skemata documents, as described in
[spec 9.4](../spec/09-versioning-and-conformance.md#94-meta-schema-architecture). Editors use them for
completion and validation of `*.skemata.yaml` files, and validators use them for the structural checks. They
check every rule of the specification except those that spec 9.4.1 leaves to a schema validator.

They are generated from [source/draft-01.yaml](source/draft-01.yaml) into `draft-01/`, which is not committed,
and published at their `$id` URLs:

| File | `$id` | Checks |
|---|---|---|
| `schema.json` | `https://skemata.github.io/skemata/draft-01/schema` | any Skemata schema; selects the target's meta-schema by `$target` and refers to it by its `$id` |
| `schema.bundled.json` | `https://skemata.github.io/skemata/draft-01/schema.bundled` | the same, with all target meta-schemas included: one file, no further downloads |
| `targets/<target>.json` | `https://skemata.github.io/skemata/draft-01/targets/<target>` | a schema for one target; `csv` also serves `csv-semicolon` and `csv-tab` |
| `locale.json` | `https://skemata.github.io/skemata/draft-01/locale` | a locale file |

## How the meta-schemas are built

Every position of a target (`root`, `schema`, and the positions of the layout in the source definition, such
as `record` and `field` for `csv`) is an `anyOf` of closed alternatives: `<position>.ref` for a `$ref` with its
siblings, and `<position>.<type>` (numbered where keywords that exclude each other split a type) listing only
the keywords allowed together. Choices of a labeled enum are `choice.<type>`, union branches `unionBranch`.
Rules shared by several alternatives are in `<alternative>.rules`; each rule names its rule IDs in `$comment`.
Identical alternatives are emitted once.

Closed alternatives are used instead of `if`/`then` because editors offer the union of the keys that all
matching schemas name, and never drop a key that a branch sets to `false`. With alternatives, editors offer
only the keys of the alternatives that still match: after `type: string` only string keywords, after
`prefixItems` no `uniqueItems`. For the same reason, the values of labeled-enum choices are restricted only by
their type: a rule that named `enum` or `minimum` for the items of `oneOf` would make editors offer them in
every choice. The meta-schemas use only keywords of JSON Schema 2020-12, without editor-specific extensions
or texts written for them; descriptions belong in the specification.

## Generating and verifying

The generator is a .NET 10 console application:

```
dotnet run --project tools/Skemata.MetaSchemas -- generate   # writes schemas/draft-01/
dotnet run --project tools/Skemata.MetaSchemas -- verify     # fails on outdated files or rejected examples
```

Both commands first compare the source definition with tables of the specification (5.2, 8.3, 8.4, 8.5) and
stop if they disagree. The rest of the source definition (the positions below the root, name patterns, and
the rules between keywords) is guarded by the worked examples: `verify` validates every example, valid ones
must pass, and invalid ones must be caught unless spec 9.4.1 leaves the rule they break (the start of their
file name) to a schema validator. The publishing workflow (`.github/workflows/pages.yml`) runs both.

## Using the meta-schemas in an editor

The Red Hat YAML extension for VS Code uses a document's own `$schema` before any workspace setting, so
schemas that declare `$schema` get completion and validation from the published meta-schemas. The editor
downloads `schema` and the target meta-schemas it refers to. Every file is published under its `$id` and under
its `.json` name; GitHub Pages serves the names without extension with a generic content type, so tools that
require `application/json` can use the `.json` URLs.

To use local files instead (offline, or to try changes before they are published), run `generate` and add a
modeline with the bundled meta-schema as the first line of a schema file. For a schema in
`spec/examples/json/`:

```yaml
# yaml-language-server: $schema=../../../schemas/draft-01/schema.bundled.json
```

The path is relative to the schema file. The modeline takes precedence over `$schema`. Use the bundled file
here, not `schema.json`: the references in `schema.json` lead to the published target meta-schemas, not to the
local files. A modeline can also name one target's meta-schema directly, such as
`../../../schemas/draft-01/targets/json.json`.
