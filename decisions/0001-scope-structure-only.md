# ADR 0001: Skemata describes data structure only

- Status: Accepted
- Date: 2026-10-08

## Context

Tools that work with stored data need several kinds of information: what the data looks like, where it is
stored, and what should happen to it (create, change, remove). These concerns change at different
rates and are owned by different people. A schema author knows the structure of a format's data, but not
where it is stored on a given system or how it will be processed.

## Decision

A Skemata schema describes only the **structure** of data: members, types, constraints, identity,
storage representation within the format, and presentation (titles, groups, translations).

Out of scope:

- location (file paths, registry key paths, hives, database files),
- processing (creating, changing or removing data, scheduling, conflict handling),
- applicability (versions of the producing software, platforms),
- storage of secret values.

## Consequences

- Schemas are reusable: one schema serves every location where the same structure occurs, such as the same
  registry key under HKLM and HKCU.
- `default` documents the value that applies when a member is absent; it is not an instruction to write it.
- Tools that process data need their own location binding and processing logic on top of Skemata.
