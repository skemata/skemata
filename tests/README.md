# Conformance test suite

Test cases for implementations of the specification. `schema/` holds cases for **schema validators**
([spec 9.3](../spec/09-versioning-and-conformance.md#93-conformance-classes), CNF-2): schema documents and
locale files that a schema validator must accept or reject. Cases for data validators (schema and target data)
are planned for `data/`.

## Layout

| File | Covers |
|---|---|
| `schema/document.yaml` | document structure (chapter 2, TGT-2) |
| `schema/data-model.yaml` | names and node kinds (chapter 3) |
| `schema/keywords.yaml` | keywords (chapter 4), including type inference |
| `schema/types.yaml` | types and formats (chapter 5) |
| `schema/annotations.yaml` | UI annotations (chapter 6) |
| `schema/localization.yaml` | locale files (chapter 7) |
| `schema/targets/<target>.yaml` | the rules of one target chapter |

## Format

Each file is a YAML list of cases:

```yaml
- description: column with type object
  valid: false
  rules: [CSV-1]
  schema:
    $schema: https://skemata.github.io/skemata/draft-01/schema
    $target: csv
    items:
      properties:
        a:
          type: object
```

| Key | Meaning |
|---|---|
| `description` | what the case shows |
| `valid` | `true`: a conforming schema validator accepts the document; `false`: it rejects it |
| `rules` | for invalid cases, every rule the document breaks (validators SHOULD report them); for valid cases, the rules the case exercises |
| `schema` | the document, written as YAML within the test file |
| `document` | instead of `schema`: the document as text, for rules about the text itself (DOC-1 to DOC-3); read it as a YAML 1.2 document |
| `kind` | `locale` for a locale file; the default is a schema document |
| `metaSchemas` | `false`: the meta-schemas of this repository cannot detect the case, because spec 9.4.1 leaves its rules to a schema validator. Irrelevant for other implementations |

The test files follow DOC-2 and DOC-3 themselves, so that YAML 1.1 and 1.2 parsers read them alike; only the
texts of `document` break them on purpose.

## Running

`dotnet run --project tools/Skemata.MetaSchemas -- verify` runs the suite against the generated meta-schemas
(run `generate` first). It fails if:

- a valid case is rejected, or an invalid case is accepted although it is not marked `metaSchemas: false`;
- a case marked `metaSchemas: false` is caught after all, or names a rule that 9.4.1 does not leave to a
  schema validator;
- a case names no rule or a rule that the spec does not define;
- a test file or worked example breaks DOC-2 or DOC-3.

It also lists the rules that have no invalid case or example. Many of them concern target data or processors
and cannot be tested with schema documents.

Which keys editors offer with the meta-schemas is tested separately, in
[`tools/completion-tests`](../tools/completion-tests/).
