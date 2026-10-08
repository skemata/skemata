# ADR 0021: Units of measurement as an annotation

- Status: Accepted
- Date: 2026-10-08

## Context

Many numeric values carry a unit that only their documentation mentions: cache sizes in bytes, intervals in
hours, timeouts in milliseconds. UIs cannot label or convert such values, and users misread them.

## Decision

The annotation keyword `unit` names the unit of an integer or number, from a closed list (data sizes in powers
of 1000 and 1024, time units, percent, pixel, point). It does not change the stored value. Tools write values
in the declared unit; UIs may convert them for display.

Strings that contain a unit (`"5min"`, `"1Gi"`) are not covered; they would need a format of their own.

## Consequences

- UIs can show "100 MiB" instead of "104857600".
- The list can grow in later versions without breaking schemas.
