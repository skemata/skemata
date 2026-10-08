# ADR 0024: CSV and PowerShell data files as targets

- Status: Accepted
- Date: 2026-10-08

## Context

A walk through further data formats showed two common formats that fit the existing data model without core
changes: CSV files (import lists, inventories, exports) and PowerShell data files (`.psd1`, including module
manifests).

## Decision

- **`csv`, `csv-semicolon`, `csv-tab`:** CSV files with a header row, following RFC 4180, separated by
  commas, semicolons or tabs. The root is an array of records whose members are the columns. Each delimiter
  is a separate target, because a file does not reliably reveal its delimiter, so the schema must state it
  (ADR 0006). An empty field in a non-string column means the value is absent.
- **`psd1`:** a PowerShell data file holding one hashtable. The data model matches JSON's, with
  case-insensitive keys, `$true`/`$false` booleans and `$null`. A single scalar is accepted where an array of
  scalars is declared, as data files commonly write one-element lists.

## Consequences

- Both targets confirm that new formats need target chapters, not core changes (ADR 0010).
- CSV files with locale-specific number forms (decimal comma) need a separate target later.
