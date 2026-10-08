# ADR 0016: Attributes and content are separate from members

- Status: Accepted
- Date: 2026-10-08

## Context

XML elements carry three kinds of data: child elements, attributes and text. An earlier draft described all
three as members of `properties`, with a `placement` keyword (`child`, `attribute`, `content`) selecting
where each member is stored. Because members share one namespace, this approach could not describe an
element that has an attribute and a child element with the same name. It also could not describe undeclared
attributes, and the text content needed an invented member name that is stored nowhere.

## Decision

- **`attributes`** describes the attributes of an object node with an object subschema. Inside it,
  `properties`, `required`, `additionalProperties`, `propertyNames`, `minProperties` and `maxProperties` work
  as usual, applied to attributes. Attributes are a namespace of their own.
- **`content`** describes the node's own unnamed scalar value (the element's text) with a scalar subschema.
  A node with content has no members.
- `placement` is removed.
- `identity` and union discriminators can refer to attributes as well as members (see ADR 0018 for identity
  references).

A flat keyword family (`attributeProperties`, `additionalAttributeProperties`, `requiredAttributes`, …) was
considered. The nested form was chosen because it reuses every object keyword unchanged and adds only one
keyword.

## Consequences

- Elements with an attribute and a child element of the same name can be described.
- Undeclared attributes can be described or forbidden (`additionalProperties` inside `attributes`).
- One extra indentation level for attributes.
- The registry `(Default)` value remains the member with the empty name (ADR 0012); it is a value among
  values, not the node's own content.
