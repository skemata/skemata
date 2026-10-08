# ADR 0008: Item identity is part of the structure

- Status: Accepted
- Date: 2026-10-08

## Context

Tools that process stored data often handle only some of its keys and must leave the rest untouched.
To change one entry of a list, such as one `<add key="…">` element or one table row, they must know how
entries are identified. Whether a list is ordered, whether it is a set, and whether a value is absent, null
or empty also matter.

## Decision

The data model declares:

- **item identity** for arrays: positional (default), set (`uniqueItems`) or keyed
  (`identity: [references]`, ADR 0018), modelled on Kubernetes list-map keys and XSD `xs:key`;
- **order significance** with `ordered`;
- the distinction between **absent, null and empty**;
- **open content**: undescribed members are valid, and tools should preserve them.

How tools compare, merge or apply data remains out of scope (ADR 0001).

## Consequences

- Individual list items and rows can be addressed in every target.
- Every SQLite table must declare its identity.
