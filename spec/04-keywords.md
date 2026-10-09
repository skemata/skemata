# 4. Keywords

## 4.1 Principles

1. **Same name, same meaning.** A keyword that exists in JSON Schema 2020-12 has the same meaning in Skemata.
   Skemata may *tighten* a keyword (restrict where or how it may be used), but never changes what it means.
2. **Generic names for new concepts.** Concepts that JSON Schema lacks get new, unprefixed keywords that are
   named after the concept, never after a storage format.
3. **Closed vocabulary.** **[KW-1]** A schema MUST NOT contain keywords other than those defined in this
   chapter. Use `$comment` for notes.
4. **Per-target allowlists.** Each target allows a subset of the keywords, `type` values and `format`
   values ([8.4](08-target-profiles.md#84-keyword--target-matrix)). **[KW-2]** A keyword, `type` value or
   `format` value MUST NOT be used in a schema whose target does not allow it.
5. **Document keywords start with `$`.** As in JSON Schema, keywords that concern the schema document itself
   (its version, identity, target, language, references and notes) start with `$`: `$schema`, `$id`,
   `$target`, `$locale`, `$defs`, `$ref`, `$anchor` and `$comment`. Keywords without `$` describe the data
   and how it is presented.
6. **Implicit where obvious.** What follows from the structure need not be written: types are inferred,
   identity values are required, titles are derived from names. Every implicit rule is defined exactly, all
   processors apply it in the same way, and explicit keywords always take precedence (4.11).

JSON Schema 2020-12 lets keywords apply to any instance type and silently ignores those that do not match.
Skemata tightens this: a keyword appears only where it is meaningful (rule KW-17 and the rules in 4.7 and 4.8).
This keeps schemas unambiguous for UIs and for target mappings.

A keyword is **used** when it is present in a subschema, whatever its value. Rules that restrict where a
keyword may be used, or with which other keywords, therefore also apply when its value equals its default:
`textual: false` on a string without a natively stored format violates KW-25, and `ordered: true` next to
`prefixItems` violates KW-27.

## 4.2 Keyword summary

| Keyword | Origin | Applies to | Section |
|---|---|---|---|
| `$schema`, `$id` | JSON Schema | root | [2.2](02-document-structure.md#22-root-keywords) |
| `$target`, `$locale` | Skemata | root | [2.2](02-document-structure.md#22-root-keywords) |
| `$defs`, `$ref`, `$anchor`, `$comment` | JSON Schema | see 4.3 | 4.3 |
| `type` | JSON Schema | any subschema | 4.4, 4.11 |
| `properties`, `required`, `dependentRequired`, `additionalProperties`, `patternProperties`, `propertyNames`, `minProperties`, `maxProperties` | JSON Schema | objects | 4.4 |
| `items`, `prefixItems`, `minItems`, `maxItems`, `uniqueItems` | JSON Schema | arrays | 4.4 |
| `identity`, `ordered` | Skemata | arrays; `ordered` also objects | 4.4 |
| `oneOf` | JSON Schema | labeled enums, unions | 4.4 |
| `enum`, `const` | JSON Schema | scalars | 4.6 |
| `minimum`, `maximum`, `exclusiveMinimum`, `exclusiveMaximum`, `multipleOf` | JSON Schema | integers, numbers | 4.6 |
| `minLength`, `maxLength`, `pattern` | JSON Schema | strings | 4.6 |
| `format`, `contentEncoding` | JSON Schema | scalars | 4.6, [chapter 5](05-types-and-formats.md) |
| `attributes`, `content` | Skemata | objects | 4.7 |
| `namespace`, `textual`, `expandable` | Skemata | members, attributes, scalars | 4.8 |
| `title`, `description`, `default`, `examples`, `deprecated`, `readOnly`, `writeOnly` | JSON Schema | any subschema | [chapter 6](06-ui-annotations.md) |
| `unit` | Skemata | integers, numbers | [6.9](06-ui-annotations.md#69-units) |
| `groups`, `group` | Skemata | root; members | [6.8](06-ui-annotations.md#68-groups) |

## 4.3 Document and reference keywords

**[KW-3]** `$schema`, `$id`, `$target`, `$locale`, `groups` and `$defs` MUST appear only in the root schema.

### `$defs`

An object whose values are subschemas. It holds reusable definitions; it does not describe a node by itself.

### `$ref`

References a subschema in the same document. Same meaning as in JSON Schema: the referencing subschema
describes the node as the referenced subschema does.

**[KW-4]** The value of `$ref` MUST be a local reference: either `#/$defs/<name>` or `#<anchor>`.
External references are reserved for a later draft.

**[KW-5]** A subschema containing `$ref` MUST NOT contain other keywords except the annotation keywords
`title`, `description`, `default`, `examples`, `deprecated`, `readOnly`, `writeOnly`, `group`, `unit` and
`$comment`, and the keywords `$anchor` and `namespace`. Annotations next to `$ref` override those of the
referenced subschema for this use.

A subschema may refer to itself directly or indirectly. Such **recursive** schemas describe trees of
unbounded depth, for example bookmark folders that contain folders:

```yaml
$defs:
  bookmark:
    type: object
    required: [name]
    properties:
      name: { type: string }
      url: { type: string, format: uri }
      children:                         # optional, so the recursion can end
        type: array
        items: { $ref: "#/$defs/bookmark" }
```

**[KW-6]** A chain of subschemas that consist only of `$ref` MUST NOT loop. Every recursion MUST be able to
end: for every subschema, there MUST be finite data that it describes. A recursion can end where it passes
through a member or attribute that may be absent (not required, explicitly or implicitly), through an array
that may be empty (no `minItems` above 0), or through a union or labeled enum that has a branch outside the
recursion. Data described by a recursive schema is therefore always finite.

### `$anchor`

Gives a subschema a plain name. It can be referenced as `#<name>`, and localization files can use it as a
stable key ([chapter 7](07-localization.md)).

**[KW-7]** An `$anchor` value MUST match `^[A-Za-z_][A-Za-z0-9._-]*$` (the JSON Schema 2020-12 anchor syntax)
and MUST be unique within the document.

### `$comment`

A note for schema authors. Consumers MUST NOT show it to end users.

## 4.4 Structure keywords

### `type`

**[KW-8]** The value of `type` MUST be either a single type name (`object`, `array`, `string`, `integer`,
`number`, `boolean`), or a two-element array containing one of the scalar type names `string`, `integer`,
`number`, `boolean` and the name `"null"` (a nullable scalar). A standalone `null` type is not allowed.

`type` can often be omitted: it is inferred from the other keywords (4.11.1).

### `properties`, `required`

`properties` maps member names to subschemas (declared members). `required` lists member names that must
not be absent ([3.6](03-data-model.md#36-absent-null-and-empty)).

**[KW-9]** Every name in `required` MUST be declared in `properties` of the same subschema, and names in
`required` MUST be unique.

### `dependentRequired`

Same meaning as in JSON Schema: maps a member name to the names of members that must be present whenever that
member is present.

```yaml
Homepage:
  properties:
    URL: { format: uri }
    Locked: { type: boolean }
  dependentRequired:
    Locked: [URL]                # locking the home page requires a URL
```

**[KW-29]** Every name in `dependentRequired`, whether a key or an entry of a list, MUST be declared in
`properties` of the same subschema, and the names in each list MUST be unique. Inside `attributes`, the names
refer to attributes.

### `additionalProperties`, `propertyNames`

`additionalProperties` describes dynamic members ([3.3](03-data-model.md#33-objects-and-members)). Its value
is a subschema, `false` (closed object) or `true` (same as absent: open content).

`propertyNames` constrains the names of all members of the object, declared and dynamic, as in JSON Schema.
Its value is a subschema that implicitly describes a string.

**[KW-10]** `propertyNames` MAY contain only `pattern`, `minLength`, `maxLength`, `enum`, `description` and
`$comment`. It MUST NOT contain `type`.

### `patternProperties`

Same meaning as in JSON Schema: maps regular expressions to subschemas, and a dynamic member whose name
matches a pattern is described by that pattern's subschema. `additionalProperties` then describes only the
members that match neither a declared name nor a pattern. This lets one object hold dynamic members of
different kinds, for example a default entry `"*"` next to entries keyed by extension ID:

```yaml
ExtensionSettings:
  properties:
    "*": { $ref: "#/$defs/defaults" }
  patternProperties:
    "^[^*]": { $ref: "#/$defs/extension" }
  additionalProperties: false
```

**[KW-31]** Every key of `patternProperties` MUST be a valid regular expression under KW-20, and every value a
subschema. Skemata tightens JSON Schema so that every member has exactly one subschema: a name declared in
`properties` MUST NOT match any pattern, and a data validator MUST report a member whose name matches more
than one pattern as invalid.

### `minProperties`, `maxProperties`

Count all members of the object: declared, dynamic and undescribed (open content), as in JSON Schema.
Attributes and content are not members. Inside `attributes` (4.7), the keywords count attributes.

### `items`, `minItems`, `maxItems`

`items` describes the items of an array. `minItems` and `maxItems` limit the number of items. Same meaning
as in JSON Schema.

**[KW-11]** `items` MUST be a single subschema, or `false` in an array with `prefixItems`. It MAY describe an
array (nested arrays) only where the target allows nested arrays
([8.5](08-target-profiles.md#85-type-and-format--target-matrix)).

### `prefixItems`

Describes a **tuple**: an array whose items have fixed positions, each with its own subschema. Same meaning as
in JSON Schema: the first item is described by the first subschema, and so on. A position is identified by its
index, so tuples are always ordered.

```yaml
position:                        # GeoJSON position: [longitude, latitude, optional altitude]
  prefixItems:
    - { type: number, minimum: -180, maximum: 180, title: Longitude }
    - { type: number, minimum: -90, maximum: 90, title: Latitude }
    - { type: number, title: Altitude }
  minItems: 2                    # explicit: the altitude may be missing
```

**[KW-27]** `prefixItems` MUST be a non-empty array of subschemas. It MUST NOT be combined with `uniqueItems`,
`identity` or `ordered`. Unless written otherwise, items after the last position are not allowed
(`items: false` is implied) and every position must be present (`minItems` equal to the number of positions
is implied) (4.11).

### `uniqueItems`

`uniqueItems: true` declares a **set** ([3.4](03-data-model.md#34-arrays-and-item-identity)): no two items
are equal, and an item is identified by its value.

**[KW-12]** `uniqueItems` MUST only be used on arrays whose items are scalars. Arrays of objects use
`identity`.

### `identity`

Declares a **keyed** array: the values at the listed **identity references** together identify an item, and
no two items have the same identity. The value is a non-empty array of references. Each reference is a
string:

- A reference that does not start with `/` is the name of a member of the item. `name` is shorthand for
  `/properties/name`.
- A reference that starts with `/` is a JSON Pointer (RFC 6901) evaluated against the item subschema, the
  subschema that describes the item. It can address a member (`/properties/name`), an attribute
  (`/attributes/properties/key`), or a value nested in either, at any depth.

The values at the references are implicitly required: they must be present in every item, whether or not
they are listed in `required`.

```yaml
updates:                       # members of the item (shorthand)
  identity: [package-ecosystem, directory]

add:                           # an attribute of the item: <add key="…" value="…"/>
  identity: [/attributes/properties/key]
  items:
    attributes:
      properties:
        key: { type: string }
        value: { type: string }

dependentAssembly:             # an attribute of a child element of the item
  identity: [/properties/assemblyIdentity/attributes/properties/name]
  items:
    properties:
      assemblyIdentity:
        attributes:
          properties:
            name: { type: string }
```

**[KW-13]** `identity` MUST only be used on arrays whose `items` subschema describes an object, and MUST NOT
be combined with `uniqueItems`. Its references MUST be unique. Each reference MUST resolve as follows:

- Starting at the item subschema, the path passes only through `properties/<name>` and
  `attributes/properties/<name>` steps. Where a step reaches a subschema containing `$ref`, resolution
  continues in the referenced subschema.
- It ends at a subschema with a non-nullable scalar type. It therefore never passes through an array.

In every item, every member and attribute on the path, including the last, MUST be present. This is a
constraint of `identity` itself; it does not change the referenced subschemas, which may be shared through
`$defs`.

Names in pointers use RFC 6901 escaping: `~` is written `~0` and `/` is written `~1`. A member whose name
starts with `/` therefore needs the pointer form, for example `/properties/~1path`.

Two identities are equal when the values at all references are equal. Strings are compared exactly
(case-sensitively), regardless of the target's name comparison, because they are data, not names.

### `ordered`

Declares whether order carries meaning ([3.4](03-data-model.md#34-arrays-and-item-identity)). The value is
a boolean.

- On arrays, the default is `true`, unless the target defines the array as unordered (`sqlite` tables).
- On objects, the default is `false`. `ordered: true` declares that the object's members must appear in the
  declaration order of `properties`. Only targets with ordered children allow it on objects (8.4).

### `oneOf`

JSON Schema meaning: the node is valid against exactly one branch. Skemata allows `oneOf` in two forms only,
recognised by their branches.

**[KW-30]** A `oneOf` is a **discriminated union** if every branch is an object description: it contains
`type: object` or at least one of `properties`, `required`, `dependentRequired`, `additionalProperties`,
`patternProperties`, `propertyNames`, `minProperties`, `maxProperties`, `attributes` and `content`. It is a
**labeled enum** if no branch is an object description. A `oneOf` that mixes both kinds of branches is
invalid.

**Labeled enum.** A list of **choices**, each with its own title. A **value choice** stands for one value
(`const`) or for several spellings of the same value (`enum`). A **range choice** has no fixed value; it
stands for all values that meet its constraints.

```yaml
RestoreOnStartup:                # value choices
  format: uint32
  oneOf:
    - { const: 5, title: Open a new tab }
    - { const: 1, title: Restore the last session }
    - { const: 4, title: Open a list of URLs }

DiskCacheSize:                   # a special value next to a range
  format: uint32
  unit: byte
  oneOf:
    - { const: 0, title: Let the browser decide }
    - { minimum: 1048576, title: Fixed size }

AutoCheck:                       # several spellings per value; the first is written
  oneOf:
    - { enum: ["yes", "true", "1"], title: Enabled }
    - { enum: ["no", "false", "0"], title: Disabled }
```

**[KW-14]** In a labeled enum, the enclosing subschema MUST describe a scalar and MUST NOT contain `enum` or
`const`. Every branch is a choice:

- A value choice contains `const` or `enum`, not both.
- A range choice contains neither, but at least one value keyword allowed for the type (KW-17) other than
  `format`. A labeled enum has at most one range choice.

Besides that, a choice MAY contain only `title`, `description`, `deprecated`, `$anchor` and `$comment`.
Choices inherit the enclosing subschema's type and constraints. Choices MUST be disjoint: no value may belong
to two choices. In an `enum` choice, the first value is **canonical**: tools that write the choice SHOULD
write that value.

**Discriminated union.** An object that takes one of several shapes, selected by a member or attribute with a
fixed value (the **discriminator**):

```yaml
proxy:
  oneOf:
    - properties:
        mode: { const: direct }
    - properties:
        mode: { const: fixed }
        server: { format: hostname }
        port: { format: uint16 }
      required: [server]
```

**[KW-15]** In a discriminated union, the enclosing subschema MUST describe an object and MUST NOT contain
`properties`, `required`, `dependentRequired`, `additionalProperties`, `patternProperties`, `propertyNames`,
`minProperties`, `maxProperties`, `attributes` or `content`. Every branch MUST be an object description and
MAY contain only `type` (with the value `object`, which may be omitted), `properties`, `required`,
`dependentRequired`, `additionalProperties`, `patternProperties`, `propertyNames`, `minProperties`,
`maxProperties`, `attributes`, `content`, `ordered`, `title`, `description`, `deprecated`, `$anchor` and
`$comment`. Exactly one location MUST be
declared in every branch with a `const` value: either a member (`/properties/<name>`) or an attribute
(`/attributes/properties/<name>`) of the branch. This location is the discriminator. It is implicitly
required in every branch, and its `const` values MUST be distinct across branches.

## 4.5 Node kind

[MOD-1](03-data-model.md#31-nodes) requires that every subschema describing a node determines its kind. A
subschema determines it as follows:

| Subschema form | Kind |
|---|---|
| contains `type` | given by `type` |
| no `type`, but the type can be inferred (4.11.1) | the inferred type |
| contains `$ref` | the kind of the referenced subschema |
| branch of a labeled enum | the enclosing subschema's kind (scalar) |
| branch of a discriminated union | object |
| value of `attributes` | the attribute set, an object |
| value of `propertyNames` | a string (the names) |

**[KW-16]** Every subschema that describes a node MUST contain `type` or allow its type to be inferred
(4.11.1), unless it contains `$ref`, is a `oneOf` branch, or is the value of `attributes` or `propertyNames`.

## 4.6 Value keywords

**[KW-17]** Value keywords MUST match the subschema's type:

| Keywords | Allowed with |
|---|---|
| `minimum`, `maximum`, `exclusiveMinimum`, `exclusiveMaximum`, `multipleOf` | `integer`, `number` |
| `minLength`, `maxLength`, `pattern` | `string` |
| `format` | the types listed per format in [5.9](05-types-and-formats.md#59-format-and-type-compatibility) |
| `contentEncoding` | `string` |
| `enum`, `const` | scalar types |

The numeric keywords, `minLength`, `maxLength` and `pattern` have their JSON Schema meaning. In particular,
`minLength` and `maxLength` count Unicode code points.

### `enum`, `const`

Same meaning as JSON Schema. To give allowed values titles, use a labeled enum (`oneOf`, 4.4).

**[KW-18]** `enum` MUST be a non-empty array of distinct values. Every `enum` value and every `const` value
MUST be valid against the enclosing subschema.

### Numeric bounds

**[KW-19]** Numeric bounds MUST lie within the range of the subschema's `format`
([5.2](05-types-and-formats.md#52-integers)).

### `pattern`

An ECMA-262 regular expression, as in JSON Schema. It is not implicitly anchored; use `^…$` to match the
whole string.

**[KW-20]** A `pattern` MUST be a valid ECMA-262 regular expression. For portability across validator
implementations, it SHOULD be limited to character classes, quantifiers, alternation, groups and the anchors
`^` and `$`. It SHOULD NOT use lookbehind, backreferences, named groups or Unicode property escapes.

### `format`

Declares a refined value type, for example `uint32` or `date-time`. Skemata tightens JSON Schema's `format`:
the set of values is closed ([chapter 5](05-types-and-formats.md)), and validators MUST check it, as with
JSON Schema 2020-12's format-assertion vocabulary.

### `contentEncoding`

Declares binary data ([5.6](05-types-and-formats.md#56-binary-data)).

**[KW-21]** `contentEncoding` MUST have the value `base64`, MUST only be used with `type: string`, and MUST
NOT be combined with `format`.

## 4.7 Attributes and content

These keywords describe what an object node carries besides its members
([3.5](03-data-model.md#35-attributes-and-content)).

### `attributes`

Describes the attributes of an object node: named scalar values that belong to the node itself and are
separate from its members. The value is a subschema that describes the attribute set as an object. Inside it,
the object keywords have their usual meaning, applied to attributes instead of members.

```yaml
# <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
supportedRuntime:
  attributes:
    required: [version]
    properties:
      version: { enum: [v2.0.50727, v4.0] }
      sku: { type: string }
    additionalProperties: false
```

**[KW-22]** `attributes` MUST only be used in subschemas that describe an object (including union branches).
Its value MUST contain only `type` (with the value `object`, which may be omitted), `properties`, `required`,
`dependentRequired`, `additionalProperties`, `patternProperties`, `propertyNames`, `minProperties`,
`maxProperties` and `$comment`, or consist of a `$ref` to such a subschema. Every declared attribute, and
every `patternProperties` and `additionalProperties` subschema, MUST describe a scalar.

Without `attributes`, the attributes of a node are undescribed content: they are valid and outside the
schema's concern ([3.3.1](03-data-model.md#331-open-content)).

### `content`

Describes the node's own unnamed scalar value, such as the text of an XML element. The value is a scalar
subschema.

```yaml
# <GUIConfig name="ToolBar">standard</GUIConfig>
GUIConfig:
  attributes:
    properties:
      name: { type: string }
  content:
    type: string
    title: Value
```

**[KW-23]** `content` MUST only be used in subschemas that describe an object (including union branches), and
its value MUST describe a scalar. A subschema with `content` describes a node without members: it MUST NOT
contain `properties`, `required`, `dependentRequired`, `patternProperties`, `propertyNames`, `minProperties`
or `maxProperties`, and `additionalProperties` MAY only be `false`.

How content relates to presence, whitespace and other data is defined by the target (`xml`).

## 4.8 Storage keywords

These keywords describe how a value is stored where a target offers more than one way. They do not change
the logical value.

### `namespace`

The namespace of a member's or attribute's name, as an absolute URI. Inheritance rules are defined by the
target (`xml`).

**[KW-24]** `namespace` MUST be an absolute URI. It MUST only be used on subschemas that are values of
`properties`, either of an object or inside `attributes`.

### `textual`

`textual: true` declares that a value is stored in its lexical form
([5.8](05-types-and-formats.md#58-lexical-forms)), even though the target also has a native representation
for it. Examples include a number in a REG_SZ value, a port number as a JSON string, or a date as a quoted
TOML string. The default is `false`.

**[KW-25]** `textual` MUST only be used on subschemas of type `integer` or `number`, or of type `string`
with a `format` that the target stores natively. The target profile lists the allowed combinations.

### `expandable`

`expandable: true` declares that a string contains references to environment variables (such as
`%SystemRoot%`). Consumers are expected to expand them when reading the value. The stored text keeps the
references unexpanded. The default is `false`.

**[KW-26]** `expandable` MUST only be used on subschemas of type `string` without `format` or
`contentEncoding`.

## 4.9 Annotation keywords

`title`, `description`, `default`, `examples`, `deprecated`, `readOnly`, `writeOnly`, `unit`, `groups` and
`group` are defined in [chapter 6](06-ui-annotations.md).

## 4.10 Excluded JSON Schema keywords

The following JSON Schema 2020-12 keywords are not part of Skemata draft-01 (KW-1):

| Keywords | Reason |
|---|---|
| `allOf`, `anyOf` | Composition makes the node kind and member set depend on evaluation. Use `$ref` and explicit `properties`. |
| `not` | Negative constraints can be neither rendered nor mapped to a store. |
| `if`, `then`, `else`, `dependentSchemas` | Conditional structure. Candidates for a later draft as validation-only rules. |
| `contains`, `minContains`, `maxContains` | Validation-only. Candidates for a later draft. |
| `unevaluatedProperties`, `unevaluatedItems` | Depend on composition, which Skemata excludes. |
| `$dynamicRef`, `$dynamicAnchor`, `$vocabulary` | The Skemata vocabulary is fixed per version. |
| `contentMediaType`, `contentSchema` | Embedded documents are out of scope. |
| `definitions`, `dependencies`, `$recursiveRef`, `$recursiveAnchor` | Forms from earlier JSON Schema drafts. |

## 4.11 Implicit rules and the normalized form

**[KW-28]** Processors MUST interpret a schema in its **normalized form**: the schema as written, plus every
keyword and constraint that the rules below imply where nothing is written. A keyword that is written always
takes precedence over an implied one. Tools that show or process schemas (UIs, data validators) SHOULD work
on the normalized form, so that they all see the same schema.

| Implicit rule | What is implied | Defined in |
|---|---|---|
| Type inference | the keyword `type` | 4.11.1 |
| Presence by structure | a constraint: identity values, union discriminators and the XML document element must be present. The referenced subschemas, which may be shared, are not changed. | KW-13, KW-15, [XML-1](targets/xml.md#2-mapping) |
| Closed tuples | the keywords `items: false` and `minItems` (the number of positions), next to `prefixItems` | KW-27 |
| Derived titles | the keyword `title` of members, attributes and groups | [6.2.1](06-ui-annotations.md#621-derived-titles) |
| Target defaults | keywords such as `format: uint32` for registry integers, and readings such as PSD-4 | target chapters ([REG-4](targets/registry.md#5-value-types), [PSD-4](targets/psd1.md#4-values)) |
| Keyword defaults | `$locale: en`, `ordered` (4.4), `additionalProperties: true`, `textual: false`, `expandable: false` | the keyword's section |

Implicit rules never remove or weaken a written keyword. Where a written keyword contradicts what another
keyword implies, for example `type: string` next to `format: uint32`, the rule for that combination applies
(here TYP-7) and the schema is invalid.

### 4.11.1 Type inference

When `type` is absent, it is inferred from the other keywords of the subschema:

| Keywords present | Inferred `type` |
|---|---|
| `properties`, `required`, `dependentRequired`, `additionalProperties`, `patternProperties`, `propertyNames`, `minProperties`, `maxProperties`, `attributes`, `content`, or a discriminated union (KW-30) | `object` |
| `items`, `prefixItems`, `minItems`, `maxItems`, `uniqueItems`, `identity` | `array` |
| `format` | the type of the format ([5.9](05-types-and-formats.md#59-format-and-type-compatibility)) |
| `minLength`, `maxLength`, `pattern`, `contentEncoding`, `expandable` | `string` |
| `const`, `enum`, or the value choices of a labeled enum | the type shared by all their values: `string`, `boolean`, `integer` or `number` (`number` if integers and non-integral numbers are mixed) |

If the keywords present imply different types, or none at all, the type cannot be inferred, and KW-16
requires `type`. Numeric bounds alone (`minimum`, …) imply no type, because they apply to integers and
numbers alike. A nullable type is never inferred.

In JSON Schema, a subschema without `type` accepts values of every type. Inference tightens this, as allowed
by principle 1: a JSON Schema validator would accept more data than Skemata, but never reject data that Skemata
accepts.
