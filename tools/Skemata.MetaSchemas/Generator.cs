using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Skemata.MetaSchemas;

/// <summary>
/// Builds the meta-schemas described in spec 9.4: one self-contained meta-schema per target, a dispatcher
/// that selects one by <c>$target</c> (bundling all targets, so editors need a single file), and a
/// meta-schema for locale files. Only plain <c>$ref</c>, <c>additionalProperties: false</c>, <c>if</c>/<c>then</c>
/// and <c>anyOf</c> are used, for editors with partial JSON Schema 2020-12 support.
/// </summary>
public static class Generator
{
    const string JsonSchemaDialect = "https://json-schema.org/draft/2020-12/schema";
    const string AnchorPattern = "^[A-Za-z_][A-Za-z0-9._-]*$";
    static readonly string[] AllTypes = ["object", "array", "string", "integer", "number", "boolean"];
    static readonly string[] ScalarTypes = ["string", "integer", "number", "boolean"];

    public static SortedDictionary<string, string> Generate(SourceDefinition s)
    {
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var bundle = new JsonObject();
        foreach (var (target, column) in s.Targets)
        {
            var targetSchema = new JsonObject
            {
                ["$schema"] = JsonSchemaDialect,
                ["$id"] = s.TargetUri(target),
                ["title"] = $"Skemata {s.Version} schema document, target {target}",
                ["description"] = $"Generated from schemas/source/{s.Version}.yaml. Do not edit.",
                ["$ref"] = "#/$defs/root",
                ["$defs"] = new TargetBuilder(s, target, column, prefix: "").Build(),
            };
            files[$"targets/{target}.json"] = Serialize(targetSchema);
            foreach (var (name, def) in new TargetBuilder(s, target, column, prefix: target + ".").Build())
                bundle[target + "." + name] = def!.DeepClone();
        }

        var dispatch = new JsonArray();
        foreach (var (target, _) in s.Targets)
            dispatch.Add(new JsonObject
            {
                ["if"] = new JsonObject { ["required"] = Array("$target"), ["properties"] = new JsonObject { ["$target"] = Const(target) } },
                ["then"] = Ref($"#/$defs/{target}.root"),
            });
        files["schema.json"] = Serialize(new JsonObject
        {
            ["$schema"] = JsonSchemaDialect,
            ["$id"] = s.SchemaUri,
            ["title"] = $"Skemata {s.Version} schema document",
            ["description"] = $"Selects the meta-schema of the document's $target. Generated from schemas/source/{s.Version}.yaml. Do not edit.",
            ["type"] = "object",
            ["required"] = Array("$schema", "$target"),
            ["properties"] = new JsonObject
            {
                ["$schema"] = Const(s.SchemaUri),
                ["$target"] = new JsonObject { ["enum"] = Array(s.Targets.Select(t => t.Target).ToArray()) },
            },
            ["allOf"] = dispatch,
            ["$defs"] = bundle,
        });

        files["locale.json"] = Serialize(new JsonObject
        {
            ["$schema"] = JsonSchemaDialect,
            ["$id"] = s.LocaleUri,
            ["title"] = $"Skemata {s.Version} locale file",
            ["description"] = $"Generated from schemas/source/{s.Version}.yaml. Do not edit.",
            ["type"] = "object",
            ["required"] = Array("$schema", "schema", "$locale", "texts"),
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject
            {
                ["$schema"] = Const(s.LocaleUri),
                ["schema"] = new JsonObject { ["type"] = "string", ["format"] = "uri" },
                ["$locale"] = LocaleTag(),
                ["texts"] = new JsonObject
                {
                    ["type"] = "object",
                    ["propertyNames"] = new JsonObject { ["pattern"] = "^#" },
                    ["additionalProperties"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["minProperties"] = 1,
                        ["additionalProperties"] = false,
                        ["properties"] = new JsonObject { ["title"] = Title(), ["description"] = Type("string") },
                    },
                },
            },
        });
        return files;
    }

    /// <summary>Builds the definitions of one target's meta-schema, with references prefixed for bundling.</summary>
    sealed class TargetBuilder(SourceDefinition s, string target, string column, string prefix)
    {
        bool Row(string cell) => s.Rows.Single(r => r.Cell == cell).Allowed.Contains(column);
        bool KeywordAllowed(string name) =>
            s.Rows.Where(r => !r.Cell.StartsWith("empty member name") && r.Names.Contains(name)).Any(r => r.Allowed.Contains(column));
        bool TypeRow(string name) => s.TypeRows[name].Allowed.Contains(column);
        bool UnionAllowed => Row("`oneOf` as discriminated union");
        bool OrderedOnObjects => Row("`ordered` on objects");
        bool EmptyNameAllowed => Row("empty member name `\"\"` (MOD-3)");

        IEnumerable<string> Types => AllTypes.Where(t => t switch
        {
            "array" => TypeRow("array"),
            "boolean" => TypeRow("boolean"),
            _ => true,
        });

        IEnumerable<string> FormatsOf(string type) =>
            s.Formats.GetValueOrDefault(type, []).Where(f => !(s.FormatExceptions.TryGetValue(f, out var ex) && ex.Allowed.Contains(column)));

        JsonNode R(string name) => Ref($"#/$defs/{prefix}{name}");

        public JsonObject Build()
        {
            var defs = new JsonObject
            {
                ["root"] = SchemaNode(root: true),
                ["schema"] = SchemaNode(root: false),
                ["branch"] = UnionAllowed ? new JsonObject { ["anyOf"] = new JsonArray(R("choice"), R("unionBranch")) } : R("choice"),
                ["choice"] = Position("choice"),
            };
            if (UnionAllowed) defs["unionBranch"] = Position("unionBranch");
            if (KeywordAllowed("attributes"))
                defs["attributeSet"] = new JsonObject
                {
                    ["anyOf"] = new JsonArray(
                        new JsonObject
                        {
                            ["type"] = "object",
                            ["required"] = Array("$ref"),
                            ["additionalProperties"] = false,
                            ["properties"] = new JsonObject { ["$ref"] = KeywordValue("$ref"), ["$comment"] = KeywordValue("$comment") },
                        },
                        Position("attributeSet")),
                };
            defs["propertyNames"] = Position("propertyNames");
            defs["properties"] = new JsonObject
            {
                ["type"] = "object",
                ["propertyNames"] = Names(),
                ["additionalProperties"] = R("schema"),
            };
            defs["type"] = TypeValue();
            defs["format"] = new JsonObject { ["enum"] = Array(AllTypes.SelectMany(FormatsOf).ToArray()) };
            defs["unit"] = new JsonObject { ["enum"] = Array(s.Units.ToArray()) };
            defs["anchor"] = new JsonObject { ["type"] = "string", ["pattern"] = AnchorPattern };
            defs["title"] = Title();
            defs["count"] = new JsonObject { ["type"] = "integer", ["minimum"] = 0 };
            defs["nameList"] = new JsonObject { ["type"] = "array", ["uniqueItems"] = true, ["items"] = Type("string") };
            defs["locale"] = LocaleTag();
            defs["groups"] = new JsonObject
            {
                ["type"] = "object",
                ["propertyNames"] = R("anchor"),
                ["additionalProperties"] = new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["properties"] = new JsonObject { ["title"] = R("title"), ["description"] = Type("string") },
                },
            };
            return defs;
        }

        // A subschema (or the root): every allowed keyword, nothing else (KW-1, KW-2, KW-3), plus the rules
        // for $ref siblings (KW-5) and for keywords that do not match a written type (KW-17).
        JsonObject SchemaNode(bool root)
        {
            var keywords = s.Keywords.Where(k => (root || !k.RootOnly) && KeywordAllowed(k.Name)).ToList();
            var properties = new JsonObject();
            foreach (var k in keywords) properties[k.Name] = KeywordValue(k.Name);

            var rules = new JsonArray();
            var refSiblings = s.Positions["refSiblings"].ToHashSet();
            var notNextToRef = keywords.Where(k => !k.RootOnly && !refSiblings.Contains(k.Name)).Select(k => k.Name).ToList();
            rules.Add(new JsonObject
            {
                ["if"] = new JsonObject { ["required"] = Array("$ref") },
                ["then"] = new JsonObject { ["properties"] = Forbid(notNextToRef) },
            });
            foreach (var type in Types)
            {
                var forbidden = keywords.Where(k => k.Types != null && !k.Types.Contains(type)).Select(k => k.Name).ToList();
                if (type == "object" && !OrderedOnObjects && KeywordAllowed("ordered")) forbidden.Add("ordered");
                var then = Forbid(forbidden.Distinct());
                if (KeywordAllowed("format") && FormatsOf(type).Any()) then["format"] = new JsonObject { ["enum"] = Array(FormatsOf(type).ToArray()) };
                if (type == "array" && !TypeRow("nestedArrays") && KeywordAllowed("items"))
                    then["items"] = new JsonObject
                    {
                        ["not"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["required"] = Array("type"),
                            ["properties"] = new JsonObject { ["type"] = new JsonObject { ["anyOf"] = new JsonArray(Const("array"), ContainsConst("array")) } },
                        },
                    };
                if (then.Count == 0) continue;
                rules.Add(new JsonObject
                {
                    ["if"] = new JsonObject
                    {
                        ["required"] = Array("type"),
                        ["properties"] = new JsonObject { ["type"] = new JsonObject { ["anyOf"] = new JsonArray(Const(type), ContainsConst(type)) } },
                    },
                    ["then"] = new JsonObject { ["properties"] = then },
                });
            }

            var node = new JsonObject { ["type"] = "object" };
            if (root) node["required"] = Array("$schema", "$target");
            node["properties"] = properties;
            node["additionalProperties"] = false;
            node["allOf"] = rules;
            return node;
        }

        // The keywords allowed at a special position (KW-10, KW-14, KW-15, KW-22).
        JsonObject Position(string position)
        {
            var properties = new JsonObject();
            foreach (var name in s.Positions[position].Where(KeywordAllowed))
                properties[name] = name == "type" ? Const("object")
                    : position == "propertyNames" && name == "enum" ? new JsonObject { ["type"] = "array", ["minItems"] = 1, ["uniqueItems"] = true, ["items"] = Type("string") }
                    : KeywordValue(name);
            return new JsonObject { ["type"] = "object", ["properties"] = properties, ["additionalProperties"] = false };
        }

        JsonNode KeywordValue(string name) => Resolve(s.Keywords.Single(k => k.Name == name).Value);

        JsonNode TypeValue()
        {
            var alternatives = new JsonArray(new JsonObject { ["enum"] = Array(Types.ToArray()) });
            if (TypeRow("nullable"))
                alternatives.Add(new JsonObject
                {
                    ["type"] = "array",
                    ["minItems"] = 2,
                    ["maxItems"] = 2,
                    ["uniqueItems"] = true,
                    ["items"] = new JsonObject { ["enum"] = Array(ScalarTypes.Where(Types.Contains).Append("null").ToArray()) },
                    ["contains"] = Const("null"),
                });
            return new JsonObject { ["anyOf"] = alternatives };
        }

        JsonObject Names()
        {
            var names = new JsonObject();
            if (!EmptyNameAllowed) names["minLength"] = 1;
            if (s.NamePatterns.TryGetValue(target, out var pattern)) names["pattern"] = pattern;
            return names;
        }

        // Replaces "@name" references and the constants "@draftUri" and "@target" in a source value.
        JsonNode Resolve(JsonNode? template)
        {
            switch (template)
            {
                case JsonObject o:
                    var result = new JsonObject();
                    foreach (var (k, v) in o)
                        result[k] = k == "$ref" && v!.GetValue<string>().StartsWith('@') ? JsonValue.Create($"#/$defs/{prefix}{v.GetValue<string>()[1..]}")
                            : k == "const" && v is JsonValue cv && cv.TryGetValue<string>(out var c) && c == "@draftUri" ? JsonValue.Create(s.SchemaUri)
                            : k == "const" && v is JsonValue tv && tv.TryGetValue<string>(out var t) && t == "@target" ? JsonValue.Create(target)
                            : Resolve(v);
                    return result;
                case JsonArray a:
                    return new JsonArray(a.Select(Resolve).ToArray());
                default:
                    return template?.DeepClone() ?? JsonValue.Create(true)!;
            }
        }
    }

    static JsonObject Forbid(IEnumerable<string> keywords)
    {
        var o = new JsonObject();
        foreach (var k in keywords) o[k] = false;
        return o;
    }

    static JsonObject Title() => new() { ["type"] = "string", ["pattern"] = "^[^\\r\\n]*$" };
    static JsonObject LocaleTag() => new() { ["type"] = "string", ["pattern"] = "^[A-Za-z]{2,8}(-[A-Za-z0-9]{1,8})*$" };
    static JsonObject Type(string t) => new() { ["type"] = t };
    static JsonObject Const(string value) => new() { ["const"] = value };
    static JsonObject ContainsConst(string value) => new() { ["type"] = "array", ["contains"] = Const(value) };
    static JsonObject Ref(string pointer) => new() { ["$ref"] = pointer };
    static JsonArray Array(params string[] values) => new(values.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray());

    static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    static string Serialize(JsonNode node) => node.ToJsonString(Options).Replace("\r\n", "\n") + "\n";
}
