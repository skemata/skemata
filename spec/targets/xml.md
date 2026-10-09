# Target `xml`

Describes an **XML 1.0 document** (with namespaces).

## 1 Native structure

An XML document has one **document element**. Elements have a name (optionally in a namespace),
**attributes**, and content: either **child elements** or **text**. Elements with the same name can repeat.
The order of child elements is significant in XML, although much software that reads XML ignores it.

## 2 Mapping

| Data model | XML |
|---|---|
| root object | the document |
| the root's single member (an object) | the document element |
| object member | a child element |
| scalar member | a child element whose text is the value |
| array member | repeated child elements, each named after the member |
| array item (scalar) | an element whose text is the value |
| array item (object) | an element |
| `attributes` of an object | the attributes of its element |
| `content` of an object | the text of its element |

**[XML-1]** The root schema MUST describe an object with exactly one member in `properties`. This member is
the document element. It MUST be an object and is implicitly required: a document always has its document
element. The root MUST NOT contain `additionalProperties`, `patternProperties`, `propertyNames`,
`minProperties`, `maxProperties`, `attributes` or `content`: a document has no other members than its
document element.

**[XML-2]** A declared member that is an object or a scalar MUST occur at most once among the child elements
of its parent element. If it occurs more than once, the data is invalid; repeated elements are described as
arrays.

Example: the `appSettings` section of a .NET configuration file.

```xml
<configuration>
  <appSettings>
    <add key="LogLevel" value="Warning" />
    <add key="MaxConnections" value="20" />
  </appSettings>
</configuration>
```

```yaml
type: object
required: [configuration]
properties:
  configuration:
    type: object
    properties:
      appSettings:
        type: object
        properties:
          add:
            type: array
            identity: [/attributes/properties/key]
            items:
              type: object
              attributes:
                required: [key]
                properties:
                  key: { type: string }
                  value: { type: string }
```

An array describes all child elements with its name. A wrapper element such as `<appSettings>` is an
ordinary object member. `identity: [/attributes/properties/key]` is a JSON Pointer to the attribute `key`
of each item ([KW-13](../04-keywords.md#identity)); a plain name such as `[key]` would refer to a child
element `key`.

## 3 Attributes and content

Attributes are described with `attributes`, the text of an element with `content`
([4.7](../04-keywords.md#47-attributes-and-content)). Members, attributes and content are separate, so an
element may have an attribute and a child element with the same name.

```xml
<GUIConfig name="ToolBar" visible="yes">standard</GUIConfig>
```

```yaml
type: object
attributes:
  required: [name]
  properties:
    name: { type: string }
    visible:
      type: string
      oneOf:
        - { const: "yes", title: Visible }
        - { const: "no", title: Hidden }
content:
  type: string
```

Namespace declarations (`xmlns` and `xmlns:…`) are not attributes in the data model.

## 4 Names and namespaces

**[XML-3]** Names of members and attributes MUST be valid XML local names (NCName: no colon). Names are
compared case-sensitively.

**[XML-4]** A member's or attribute's namespace is given by `namespace`. A child element without `namespace`
is in the namespace of its parent element. An attribute without `namespace` is in no namespace. Prefixes are
not significant: `<a:x xmlns:a="urn:n"/>` and `<x xmlns="urn:n"/>` are the same element.

The namespace of the document element is declared with `namespace` on the root's single member.

## 5 Values and text

All values are text. Integers and numbers use the lexical forms of
[5.8](../05-types-and-formats.md#58-lexical-forms). There is no boolean and no null: on/off values are labeled
enums of their stored text, and `xsi:nil` is not supported.

**[XML-5]** Scalar values are the attribute values and element texts as reported by an XML 1.0 parser: after
entity expansion and attribute-value normalization, and with CDATA sections merged into the text. Validators
MUST NOT trim or otherwise change them.

An attribute `a=""` or an element `<a/>` holds the empty string. A missing attribute or element is absent.

**[XML-6]** For an element described with `content`, the content is the element's whole text, including
whitespace. It exists whenever the element exists; an element without text has the empty string as content.
If such an element has child elements, the data is invalid. For an element described without `content`,
whitespace-only text between child elements is ignored, and other text is undescribed content.

## 6 Order

**[XML-7]** If an object declares `ordered: true`, its child elements MUST appear in the declaration order of
`properties`, and the elements of an array member MUST be contiguous at the array's position. Attributes are
unordered.

```yaml
connection:
  type: object
  ordered: true           # <host> must come before <port>
  properties:
    host: { type: string, format: hostname }
    port: { type: integer, format: uint16 }
```

## 7 Dynamic members and attributes

**[XML-8]** Dynamic members (`patternProperties` and `additionalProperties` of an object) are child elements
with undeclared names, and `propertyNames` constrains the local names of all child elements. Inside
`attributes`, `patternProperties` and `additionalProperties` describe undeclared attributes, and
`propertyNames` constrains the local names of all attributes. Patterns match local names.

## 8 Representability limits

- Mixed content (text and child elements in the same element) cannot be described (XML-6).
- Two child elements, or two attributes, of one element that share a local name but differ in namespace
  cannot both be declared (MOD-2).
- Repeated elements of *different* names whose relative order carries meaning cannot be described. Examples
  are .NET configuration `<add>`, `<remove>` and `<clear>` children. Each name can be an array, but the
  interleaving is lost.
- Comments, processing instructions, the XML declaration, DOCTYPE, `xsi:type`, `xsi:nil` and
  `xsi:schemaLocation` semantics are outside the data model.

## 9 Examples

| Example | Shows |
|---|---|
| [dotnet-app-config](../examples/xml/dotnet-app-config.skemata.yaml) | keyed lists of `<add>` elements identified by an attribute, nested sections, an inherited namespace |
| [gui-config](../examples/xml/gui-config.skemata.yaml) | attributes together with content, open attributes |
| [invalid/](../examples/xml/invalid/) | content next to members, two document elements, a pattern for further document elements, an array as attribute, a colon in a name |
