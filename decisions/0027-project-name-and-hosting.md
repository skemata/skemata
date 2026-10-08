# ADR 0027: Project name Skemata, hosted on GitHub Pages

- Status: Accepted
- Date: 2026-10-08

## Context

The project was developed under the working name LSYN ("Last Schema You Need"). That name was a placeholder:
the `$id` values used the reserved domain `lsyn.example`, so editors could not download the meta-schemas.
The specification is public and vendor-neutral (ADR 0002), so it needs a neutral name and a home that
does not depend on a company domain.

## Decision

- The project is named **Skemata**, a respelling of *schemata*, the plural of *schema*. The name is short,
  available and says what the project is. It is not an acronym.
- The repository is `skemata/skemata` on GitHub and is published with GitHub Pages at
  `https://skemata.github.io/skemata/`.
- The `$id` of every meta-schema is a URL under that site, for example
  `https://skemata.github.io/skemata/draft-01/schema`. The `$id` values of the worked examples use the same
  base (`…/examples/…`); they are identifiers only and are not published.
- Schema files are named `*.skemata.yaml` (or `.yml`, `.json`) and locale files `*.skemata-locale.yaml`
  (or `.json`).
- The reference tool is `tools/Skemata.MetaSchemas` (namespace `Skemata.MetaSchemas`).
- A workflow, `.github/workflows/pages.yml`, verifies the meta-schemas and publishes `schemas/draft-01/` so
  that each `$id` is the exact URL that serves the file. Because the `$id` values have no file extension,
  the site contains an extensionless copy of each file next to the `.json` one.

## Consequences

- The `$id` values are permanent identifiers. Moving to a custom domain later means new `$id` values (a new
  draft or a documented alias), because published schemas refer to the old ones.
- GitHub Pages serves extensionless files with a generic content type. Editors and validators that read the
  body regardless of the content type work; those that insist on `application/json` can use the `.json` copy.
- Earlier ADRs were written under the old name and have been renamed in place.
