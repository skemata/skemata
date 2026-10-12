# 3. Data model

A Skemata schema does not describe the bytes of a file or the API of a store. It describes a **data model**:
a tree of nodes onto which every target maps its native structure. Each target profile
([chapter 8](08-target-profiles.md) and [`targets/`](targets/)) defines that mapping. This chapter defines the
tree itself.

## 3.1 Nodes

The data model has three node kinds:

| Kind | Holds | Declared with `type` |
|---|---|---|
| **object** | named members; in some targets also attributes and content (3.5) | `object` |
| **array** | items in sequence | `array` |
| **scalar** | a single value | `string`, `integer`, `number`, `boolean` (optionally nullable, see 3.6) |

The target data has exactly one root node. The target constrains its kind and defines what it corresponds
to. For example, the root of a `registry` schema is one registry key (an object or an array), and the root of
a `sqlite` schema is one database (an object).

**[MOD-1]** Every subschema that describes a node MUST determine exactly one node kind without looking at
data, with one exception: a type union ([KW-32](04-keywords.md#oneof)) offers branches of different types,
each of which determines its kind, and the type of the value in the data selects the branch.
[Section 4.5](04-keywords.md#45-node-kind) defines how each subschema form determines its kind.

## 3.2 Scalar values

Scalars hold **logical values**:

| Type | Logical value |
|---|---|
| `string` | a sequence of Unicode scalar values |
| `integer` | an integer in the range −2⁶³ … 2⁶⁴−1, narrowed by `format` ([5.2](05-types-and-formats.md#52-integers)) |
| `number` | a finite IEEE 754 binary64 value, or binary32 with `format: float` |
| `boolean` | true or false; only in targets with a native boolean ([5.4](05-types-and-formats.md#54-booleans)) |
| `null` | an explicit "no value"; only in targets with a native null (3.6) |

Some strings carry a more specific meaning: `format` declares dates, times, URIs and similar values, and
`contentEncoding` declares binary data ([chapter 5](05-types-and-formats.md)).

A logical value has one or more **stored representations** in a target:

- **native**: the target has a type for it, for example REG_DWORD for a 32-bit integer or a TOML local date;
- **textual**: the value is stored as its lexical form ([5.8](05-types-and-formats.md#58-lexical-forms)),
  for example `8080` in an INI file.

The target profile defines the default representation. Where a target offers both, `textual: true` selects
the textual one ([4.8](04-keywords.md#48-storage-keywords)).

## 3.3 Objects and members

An object holds **members**. Each member has a **name** that is unique within the object. A schema describes
members in two ways:

- **Declared members** are listed in `properties`, each with its own subschema.
- **Dynamic members** are all other members. `patternProperties` describes them by name pattern, and
  `additionalProperties` describes those that match no pattern.

`propertyNames` constrains the names of all members, declared and dynamic, as in JSON Schema.

A record with a fixed structure uses `properties`. A map whose names are not known in advance, for example
one entry per browser extension ID, uses `additionalProperties`. An object may combine both.

### 3.3.1 Open content

By default an object is **open**: target data MAY contain members that the schema does not describe. Such
members are valid; they are simply not the schema's concern. A consumer that modifies target data SHOULD
preserve members it does not manage. `additionalProperties: false` closes an object, so that any undeclared
member makes the data invalid.

### 3.3.2 Names

Name comparison is defined by the target. It is either case-sensitive (`xml`, `json`, `yaml-1.2`, `toml`,
`csv` and its variants) or case-insensitive (`registry`, `ini-windows`, `sqlite`, `psd1`). Case-sensitive
comparison compares the names code point by code point. Case-insensitive comparison first maps every
character of both names to upper case with the Unicode simple uppercase mapping, then compares them code
point by code point. A target may restrict the mapping further (`sqlite` folds ASCII letters only).

**[MOD-2]** Two declared members of one object MUST NOT have names that are equal under the target's name
comparison. The same applies to two declared attributes of one object (3.5).

Some stores give a node a single value that has no name, for example the `(Default)` value of a registry key.
Skemata represents it as the member with the **empty name** `""`.

**[MOD-3]** A member or attribute name MUST NOT be the empty string unless the target profile allows unnamed
members. In draft-01, only `registry` allows them.

Further name restrictions (characters, length) are defined per target.

## 3.4 Arrays and item identity

An array holds items in sequence. `items` describes every item, or, for a **tuple**, `prefixItems` describes
each position separately. Where the target allows it, items may themselves be arrays (nested arrays, such as
the coordinate lists of GeoJSON). Skemata distinguishes four ways in which items are identified. The
distinction matters to any tool that compares, displays or changes a single item.

| Mode | Declared by | An item is identified by | Typical use |
|---|---|---|---|
| **positional** | default | its position; the array is one value | command-line arguments, search paths |
| **tuple** | `prefixItems` | its position, which also determines its type | coordinates, `host` and `port` pairs |
| **set** | `uniqueItems: true` | its value | allowed URLs, extension IDs |
| **keyed** | `identity: [reference, …]` | the values at the listed references: members, attributes, or values nested in them | `<add key="…">` entries, table rows, named profiles |

A keyed array lets a tool address one item, such as one `<add key="LogLevel">` element or one database row,
without treating the whole list as a single value. The identity values are always present in every item.
Skemata only declares identity. How a tool compares or merges arrays is outside Skemata's scope.

`ordered` declares whether item order carries meaning. It defaults to `true`, except where the target
defines an array kind as unordered (rows of `sqlite` tables). A set of allowed URLs is typically
`ordered: false`.

Member order within an object normally carries no meaning. Targets with ordered children (`xml`) allow
`ordered: true` on objects: the children then appear in the declaration order of `properties`.

## 3.5 Attributes and content

Some targets attach more to a node than its members. Skemata models two such additions on object nodes:

- **Attributes** are named scalar values that belong to the node itself. They are separate from the node's
  members: an XML element can have an attribute `name` and a child element `name` at the same time.
  The keyword `attributes` describes them with an object subschema. `properties`, `required`,
  `additionalProperties`, `propertyNames`, `minProperties` and `maxProperties` therefore work for attributes
  exactly as they do for members, including open content (3.3.1).
- **Content** is the node's own unnamed scalar value, such as the text of an XML element. The keyword
  `content` describes it with a scalar subschema. A node with content has no members; Skemata has no mixed
  content.

```yaml
# <GUIConfig name="ToolBar" visible="yes">standard</GUIConfig>
type: object
attributes:
  required: [name]
  properties:
    name: { type: string }
    visible: { type: string, enum: ["yes", "no"] }
content:
  type: string
```

Only targets whose profile allows them use `attributes` and `content`; in draft-01 that is `xml`
([4.7](04-keywords.md#47-attributes-and-content)). Attributes are a separate set rather than members marked
with a keyword such as `placement: attribute`: one shared set of names could not describe an attribute and a
member of the same name, nor open attributes next to closed members.

The registry `(Default)` value is not content. The registry stores it as an ordinary value of the key, next
to the other values, that happens to have no name. Skemata therefore describes it as the member with the empty
name (3.3.2).

## 3.6 Absent, null and empty

- **Absent**: the member or attribute does not exist in the target data. `required` lists the members (or,
  inside `attributes`, the attributes) that must not be absent.
- **Null**: the member exists and holds null. Only targets with a native null support it (`json`,
  `yaml-1.2`, `sqlite`, `psd1`). A schema allows null with a two-element type, for example
  `type: [string, "null"]`.
- **Empty**: `""`, `[]` and `{}` are values. A registry value with empty REG_SZ data exists; a registry value
  that is missing is absent.

**[MOD-4]** Validators MUST NOT treat absent, null and empty as equivalent.

## 3.7 Data outside the model

Every target has native constructs that the data model does not represent, such as comments, XML processing
instructions or INI duplicate keys. Each target chapter lists them. A schema cannot describe them. Validators
ignore them unless they make declared content ambiguous; the target chapter defines when that is the case.
