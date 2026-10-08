# ADR 0018: Identity references are JSON Pointers relative to the item schema

- Status: Accepted
- Date: 2026-10-08

## Context

`identity` listed plain names, which were looked up among the members and the attributes of an item. Since
attributes have their own namespace (ADR 0016), an element can have an attribute and a child element with
the same name, and a plain name becomes ambiguous. Some lists are also identified by a value one level down.
For example, a .NET `<dependentAssembly>` entry is identified by the `name` attribute of its child element
`<assemblyIdentity>`. With plain names, such lists can only be positional: tools cannot address an entry by
its key, and validators cannot detect duplicate entries.

Three forms were considered:

- keeping the plain-name lookup and rejecting ambiguous names;
- tagged entries (`{ attribute: key }`, combinable with `member` for one level of nesting);
- JSON Pointers relative to the item schema.

## Decision

An identity reference is either:

- a plain name, which is shorthand for the member `/properties/<name>`; or
- a string starting with `/`, which is a JSON Pointer (RFC 6901) evaluated against the item subschema. It
  passes only through `properties/<name>` and `attributes/properties/<name>` steps, follows `$ref`, and ends
  at a non-nullable scalar. The values at the references are implicitly required (ADR 0019).

Union discriminators use the same notion of location: a member (`/properties/<name>`) or an attribute
(`/attributes/properties/<name>`).

## Consequences

- References are unambiguous, and they reach nested values at any depth with one mechanism, the same
  JSON Pointer syntax that `$ref` and locale files already use.
- Existing schemas that identify items by members keep their short form.
- Attribute references are longer to write (`/attributes/properties/key`).
