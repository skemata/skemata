# Design decisions

Architecture decision records (ADRs) for Skemata. Each record states the context, the decision and its
consequences. Records are not edited after acceptance except for status changes; a changed decision gets a
new record that supersedes the old one.

| ADR | Decision | Status |
|---|---|---|
| [0001](0001-scope-structure-only.md) | Skemata describes data structure only | Accepted |
| [0002](0002-public-vendor-neutral-specification.md) | Public, vendor-neutral specification | Accepted |
| [0003](0003-json-schema-style-own-language.md) | JSON Schema style, own language | Accepted |
| [0004](0004-one-target-per-schema.md) | One schema describes exactly one target | Accepted |
| [0005](0005-target-profiles-are-allowlists.md) | Target profiles are allowlists | Accepted |
| [0006](0006-separate-target-names-for-variants.md) | Format variants are separate targets | Accepted |
| [0007](0007-logical-numbers-no-implicit-booleans.md) | Logical numbers, no implicit booleans | Accepted |
| [0008](0008-structural-identity-for-item-level-management.md) | Item identity is part of the structure | Accepted |
| [0009](0009-inline-source-language-and-locale-files.md) | Inline source language plus locale files | Accepted |
| [0010](0010-windows-style-targets-first.md) | Windows-style targets first, open target set | Accepted |
| [0011](0011-registry-one-key-per-schema.md) | Registry: one key per schema | Accepted |
| [0012](0012-empty-name-for-unnamed-members.md) | The empty name denotes an unnamed member | Accepted |
| [0013](0013-reference-tooling-in-dotnet.md) | Reference tooling in .NET | Accepted |
| [0014](0014-registry-lists-as-numbered-values.md) | Registry: a root array is a numbered-value list | Accepted |
| [0015](0015-ini-windows-without-global-keys.md) | `ini-windows` has no global keys | Accepted |
| [0016](0016-attributes-and-content.md) | Attributes and content are separate from members | Accepted |
| [0017](0017-document-keywords-start-with-dollar.md) | Document keywords start with `$` | Accepted |
| [0018](0018-identity-references-are-pointers.md) | Identity references are JSON Pointers relative to the item schema | Accepted |
| [0019](0019-implicit-rules-and-normalized-form.md) | Implicit rules and the normalized form | Accepted, partly superseded by 0030 |
| [0020](0020-labeled-enum-choices.md) | Labeled enums have value choices and one range choice | Accepted |
| [0021](0021-units.md) | Units of measurement as an annotation | Accepted |
| [0022](0022-recursion-tuples-nested-arrays.md) | Recursion, tuples and nested arrays | Accepted |
| [0023](0023-dependent-required.md) | `dependentRequired` | Accepted |
| [0024](0024-csv-and-psd1-targets.md) | CSV and PowerShell data files as targets | Accepted |
| [0025](0025-pattern-properties.md) | `patternProperties` with at most one matching pattern | Accepted |
| [0026](0026-meta-schema-generation.md) | Meta-schemas generated from a YAML source checked against the spec | Accepted, partly superseded by 0029 |
| [0027](0027-project-name-and-hosting.md) | Project name Skemata, hosted on GitHub Pages | Accepted |
| [0028](0028-meta-schemas-follow-target-layouts.md) | Meta-schemas follow the layout of each target | Accepted |
| [0029](0029-referencing-and-bundled-dispatcher.md) | A referencing dispatcher and a bundled copy | Accepted |
| [0030](0030-meta-schemas-infer-types.md) | Meta-schemas infer types and offer only valid keywords | Accepted |
