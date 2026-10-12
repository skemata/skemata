using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Skemata.MetaSchemas;

/// <summary>
/// Builds the meta-schemas described in spec 9.4: one self-contained meta-schema per target, a dispatcher
/// that selects one by <c>$target</c>, a bundled copy of the dispatcher that contains all targets (a single
/// file for use without network access), and a meta-schema for locale files. A target's meta-schema has a
/// definition for each subschema position of the target's layout, such as the root, the records and the fields
/// of a csv file (9.4 item 7), made of closed alternatives per node kind (9.4 item 6). Only keywords of JSON
/// Schema 2020-12 are used, without <c>$dynamicRef</c> and <c>unevaluatedProperties</c>, for editors with
/// partial support.
/// </summary>
public static class Generator
{
    const string JsonSchemaDialect = "https://json-schema.org/draft/2020-12/schema";
    const string AnchorPattern = "^[A-Za-z_][A-Za-z0-9._-]*$";
    static readonly string[] AllTypes = ["object", "array", "string", "integer", "number", "boolean"];
    static readonly string[] ScalarTypes = ["string", "integer", "number", "boolean"];
    static readonly string[] KindNames = [.. AllTypes, "scalar"];
    // Definitions that are not positions; layouts cannot use these names for positions.
    static readonly string[] SharedDefinitions =
        ["choice", "unionBranch", "attributeSet", "propertyNames", "unit", "anchor", "title", "count", "nameList", "locale", "groups"];

    public static SortedDictionary<string, string> Generate(SourceDefinition s)
    {
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var bundle = new JsonObject();
        // One meta-schema per matrix column: targets that share a column (csv, csv-semicolon, csv-tab) share it.
        foreach (var column in s.Targets.Select(t => t.Column).Distinct())
        {
            var targets = s.Targets.Where(t => t.Column == column).Select(t => t.Target).ToList();
            var targetSchema = new JsonObject
            {
                ["$schema"] = JsonSchemaDialect,
                ["$id"] = s.TargetUri(column),
                ["title"] = $"Skemata {s.Version} schema document, target {string.Join(", ", targets)}",
                ["description"] = $"Generated from schemas/source/{s.Version}.yaml. Do not edit.",
                ["$ref"] = "#/$defs/root",
                ["$defs"] = new TargetBuilder(s, targets, column, prefix: "").Build(),
            };
            files[$"targets/{column}.json"] = Serialize(targetSchema);
            foreach (var (name, def) in new TargetBuilder(s, targets, column, prefix: column + ".").Build())
                bundle[column + "." + name] = def!.DeepClone();
        }

        // The dispatcher refers to the target meta-schemas by their $id, relative to its own (9.4 item 3). The
        // bundled dispatcher contains them, for editors and validators that work without network access.
        string ColumnOf(string target) => s.Targets.Single(t => t.Target == target).Column;
        files["schema.json"] = Serialize(Dispatcher(s, s.SchemaUri, $"Skemata {s.Version} schema document",
            "Selects the meta-schema of the document's $target from targets/.", target => $"targets/{ColumnOf(target)}", defs: null));
        files["schema.bundled.json"] = Serialize(Dispatcher(s, s.BundleUri, $"Skemata {s.Version} schema document (bundled)",
            "Selects the meta-schema of the document's $target. Same as schema, with all target meta-schemas included.",
            target => $"#/$defs/{ColumnOf(target)}.root", bundle));

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
    sealed class TargetBuilder(SourceDefinition s, List<string> targets, string column, string prefix)
    {
        // A subschema position: its layout entry, the types allowed there, and within one alternative the
        // keywords of that alternative.
        sealed record Context(LayoutPosition Layout, List<string> Types, HashSet<string>? Keywords = null);

        static readonly string[] Bounds = ["minimum", "maximum", "exclusiveMinimum", "exclusiveMaximum"];

        // Definitions referenced while building, in order of first reference, and the positions that are
        // referenced as declared members (only those allow `group` and `namespace`, UI-5 and KW-24).
        readonly List<string> requested = [];
        readonly HashSet<string> memberPositions = [];

        // The definitions of the current build, and the names of alternatives by their content, so that
        // identical alternatives of different positions are emitted once.
        JsonObject defs = [];
        readonly Dictionary<string, string> namesByContent = [];

        bool Row(string cell) => s.Rows.Single(r => r.Cell == cell).Allowed.Contains(column);
        IEnumerable<MatrixRow> AllowingRows(string keyword) =>
            s.Rows.Where(r => !r.Cell.StartsWith("empty member name") && r.Names.Contains(keyword) && r.Allowed.Contains(column));
        bool KeywordAllowed(string name) => AllowingRows(name).Any();
        bool TypeRow(string name) => s.TypeRows[name].Allowed.Contains(column);
        bool EmptyNameAllowed => Row("empty member name `\"\"` (MOD-3)");
        bool NumberNeedsTextual => s.TypeRows["number"].TextualOnly.Contains(column);
        bool TextualStrings => s.TextualFormats.Allowed.Contains(column);

        // The types the target allows (8.5).
        IEnumerable<string> TargetTypes => AllTypes.Where(t => t switch
        {
            "array" => TypeRow("array"),
            "boolean" => TypeRow("boolean"),
            "number" => TypeRow("number") || NumberNeedsTextual,
            _ => true,
        });

        // The types a keyword applies to in this target (KW-17), null for all: the types of the matrix rows
        // that allow it, if they name any, otherwise the keyword's own.
        HashSet<string>? KeywordTypes(KeywordDefinition k)
        {
            var rows = AllowingRows(k.Name).ToList();
            var types = rows.Any(r => r.Types != null) ? rows.SelectMany(r => r.Types ?? []).ToHashSet() : k.Types?.ToHashSet();
            if (k.Name == "textual" && !TextualStrings) types?.Remove("string"); // KW-25: strings only where 8.5 allows
            return types;
        }

        bool Applies(KeywordDefinition k, List<string> types) =>
            KeywordAllowed(k.Name) && (KeywordTypes(k) is not { } keywordTypes || keywordTypes.Overlaps(types));

        KeywordDefinition Keyword(string name) => s.Keywords.Single(k => k.Name == name);

        IEnumerable<string> FormatsOf(string type) =>
            s.Formats.GetValueOrDefault(type, []).Where(f => !(s.FormatExceptions.TryGetValue(f, out var ex) && ex.Allowed.Contains(column)));

        // Positions of the target's layout, and those every target has: "schema" (any subschema, also $defs
        // entries), "item" (an array item where nested arrays are not allowed, KW-11) and "scalar".
        LayoutPosition Layout(string name)
        {
            if (s.Layouts.TryGetValue(column, out var layout) && layout.TryGetValue(name, out var position)) return position;
            return name switch
            {
                "root" or "schema" => Synthetic(["object", "array", "scalar"]),
                "item" => Synthetic(["object", "scalar"]),
                "scalar" => Synthetic(["scalar"]),
                _ => throw new InvalidDataException($"layout {column}: unknown position '{name}'"),
            };
        }

        static LayoutPosition Synthetic(List<string> kinds, string? members = null) => new(kinds, members, null, null, null, [], [], [], null);

        List<string> TypesOf(LayoutPosition p)
        {
            if (p.Kinds.FirstOrDefault(k => !KindNames.Contains(k)) is { } unknown) throw new InvalidDataException($"layout {column}: unknown kind '{unknown}'");
            return TargetTypes.Where(t => p.Kinds.Contains(t) || (p.Kinds.Contains("scalar") && ScalarTypes.Contains(t))).ToList();
        }

        string MembersOf(LayoutPosition p) => p.Members ?? "schema";
        string ItemsOf(LayoutPosition p) => p.Items ?? (TypeRow("nestedArrays") ? "schema" : "item");

        JsonNode R(string name) => Ref($"#/$defs/{prefix}{name}");

        JsonNode Def(string name)
        {
            if (!requested.Contains(name)) requested.Add(name);
            return R(name);
        }

        JsonNode Position(string name, bool member = false)
        {
            if (member) memberPositions.Add(name);
            return Def(name);
        }

        // Adds an alternative under the given name, or refers to an identical one added before.
        JsonNode Alternative(string name, JsonObject node)
        {
            var content = node.ToJsonString();
            if (namesByContent.TryGetValue(content, out var existing)) return R(existing);
            if (defs.ContainsKey(name)) throw new InvalidDataException($"two definitions named {name}");
            defs[name] = node;
            namesByContent[content] = name;
            return R(name);
        }

        public JsonObject Build()
        {
            foreach (var name in s.Layouts.GetValueOrDefault(column, []).Keys)
                if (SharedDefinitions.Contains(name) || name.Contains('.'))
                    throw new InvalidDataException($"layout {column}: '{name}' cannot be used as a position name");

            // Whether a position holds declared members is known only once every definition that refers to it
            // is built, so build until that no longer changes.
            while (true)
            {
                var members = memberPositions.Count;
                defs = [];
                namesByContent.Clear();
                Def("root");
                Def("schema");
                for (var i = 0; i < requested.Count; i++)
                {
                    defs[requested[i]] = null; // keeps a definition in front of its alternatives
                    defs[requested[i]] = Definition(requested[i]);
                }
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
                if (memberPositions.Count == members) return defs;
            }
        }

        JsonObject Definition(string name)
        {
            if (name.StartsWith("choice."))
            {
                var kind = name["choice.".Length..];
                return Special(name, "choice", new Context(Synthetic([kind]), [kind]), kind);
            }
            if (name == "unionBranch" || name.StartsWith("unionBranch."))
            {
                var members = name == "unionBranch" ? "schema" : name["unionBranch.".Length..];
                return Special(name, "unionBranch", new Context(Synthetic(["object"], members), ["object"]), "object");
            }
            return name switch
            {
                "propertyNames" => Special(name, "propertyNames", new Context(Synthetic(["string"]), ["string"]), null),
                "attributeSet" => new JsonObject
                {
                    ["anyOf"] = new JsonArray(
                        new JsonObject
                        {
                            ["type"] = "object",
                            ["required"] = Array("$ref"),
                            ["additionalProperties"] = false,
                            ["properties"] = new JsonObject { ["$ref"] = KeywordValue("$ref", null), ["$comment"] = KeywordValue("$comment", null) },
                        },
                        Special(name, "attributeSet", new Context(Synthetic(["object"], members: "scalar"), ["object"]), "object")),
                },
                _ => Node(name),
            };
        }

        // A subschema at a position: one closed alternative for a $ref with its siblings (KW-5), and for each
        // node kind allowed there one closed alternative per set of keywords that are allowed together (KW-1,
        // KW-2, KW-3, KW-17, target rules). Editors therefore offer only the keywords of the alternatives that
        // still match. Each kind alternative needs `type` or a keyword that implies its kind (KW-16, 4.11.1).
        JsonObject Node(string name)
        {
            var layout = Layout(name);
            var types = TypesOf(layout);
            var root = name == "root";
            var member = memberPositions.Contains(name);
            bool Allowed(KeywordDefinition k) => (root || !k.RootOnly) && (member || !k.MemberOnly) && !layout.Absent.Contains(k.Name);

            var alternatives = new JsonArray();
            var refKeywords = s.Keywords
                .Where(k => Allowed(k) && Applies(k, types) && (k.Name == "$ref" || k.RootOnly || s.RefSiblings.Contains(k.Name)))
                .Select(k => k.Name).ToList();
            alternatives.Add(Alternative($"{name}.ref",
                Closed($"{name}.ref", refKeywords, new Context(layout, types, [.. refKeywords]), ["$ref"], root, null, null)));

            foreach (var kind in types)
            {
                var keywords = s.Keywords.Where(k => k.Name != "$ref" && Allowed(k) && Applies(k, [kind])).Select(k => k.Name).ToList();
                var required = layout.Present.ToList();
                var textualOnly = kind == "number" && NumberNeedsTextual;
                if (textualOnly) required.Add("textual"); // REG-5
                if (!required.All(keywords.Contains)) continue;
                var sets = CompatibleSets(keywords, null, kind).Where(set => required.All(set.Contains)).ToList();
                for (var i = 0; i < sets.Count; i++)
                {
                    var set = sets[i];
                    var alternative = sets.Count == 1 ? $"{name}.{kind}" : $"{name}.{kind}.{i + 1}";
                    var node = Closed(alternative, keywords.Where(set.Contains).ToList(), new Context(layout, [kind], set), required, root, null, kind,
                        Evidence(kind, set), KindRules(kind, set));
                    if (textualOnly) node["properties"]!["textual"] = Const(true);
                    alternatives.Add(Alternative(alternative, node));
                }
            }

            // A type union (KW-32): branches of different types, each a subschema of this position but not a type
            // union itself. Whether the target tells the branches apart is left to the schema validator (9.4.1).
            if (Row("`oneOf` as type union") && layout.TypeUnion && layout.Present.Count == 0 && types.Count > 1)
            {
                var branch = Alternative($"{name}.branch", new JsonObject { ["anyOf"] = alternatives.DeepClone() });
                var keywords = s.Positions["typeUnion"].Keywords.Where(k => Allowed(Keyword(k)) && KeywordAllowed(k)).ToList();
                var node = Closed($"{name}.typeUnion", keywords, new Context(layout, types, [.. keywords]), ["oneOf"], root, "typeUnion", null);
                node["properties"]!["oneOf"] = new JsonObject { ["type"] = "array", ["minItems"] = 2, ["items"] = branch };
                alternatives.Add(Alternative($"{name}.typeUnion", node));
            }

            var position = new JsonObject();
            if (layout.Rule != null) position["$comment"] = layout.Rule;
            position["anyOf"] = alternatives;
            return position;
        }

        // A special position (KW-10, KW-14, KW-15, KW-22): its keywords that apply to its types, split into
        // closed alternatives of keywords allowed together, each needing one of the keywords it requires.
        JsonObject Special(string name, string position, Context c, string? kind)
        {
            var definition = s.Positions[position];
            var keywords = definition.Keywords.Where(n => Applies(Keyword(n), c.Types)).ToList();
            var sets = CompatibleSets(keywords, position, kind)
                .Where(set => definition.RequireAny.Count == 0 || definition.RequireAny.Any(set.Contains)).ToList();
            var alternatives = new List<JsonObject>();
            foreach (var set in sets)
            {
                var requireAny = definition.RequireAny.Where(set.Contains).ToList();
                var anyOf = requireAny.Count > 1 ? new JsonArray(requireAny.Select(k => (JsonNode)Required(k)).ToArray()) : null;
                alternatives.Add(Closed(sets.Count == 1 ? name : $"{name}.{alternatives.Count + 1}", keywords.Where(set.Contains).ToList(),
                    c with { Keywords = set }, requireAny.Count == 1 ? requireAny : [], false, position, kind, anyOf));
            }
            if (alternatives.Count == 1) return alternatives[0];
            return new JsonObject { ["anyOf"] = new JsonArray(alternatives.Select((a, i) => Alternative($"{name}.{i + 1}", a)).ToArray()) };
        }

        // A closed object schema: exactly these keywords, the required ones, and the rules that apply to them.
        // Several rules go into a definition of their own, "<name>.rules", which alternatives with the same rules
        // share.
        JsonObject Closed(string name, List<string> keywords, Context c, List<string> required, bool root, string? position, string? kind,
            JsonArray? anyOf = null, IEnumerable<JsonObject>? rules = null)
        {
            var properties = new JsonObject();
            foreach (var k in keywords)
                properties[k] = position == "propertyNames" && k == "enum"
                    ? new JsonObject { ["type"] = "array", ["minItems"] = 1, ["uniqueItems"] = true, ["items"] = Type("string") }
                    : c.Layout.Values.TryGetValue(k, out var value) ? Resolve(value, c) ?? new JsonObject() : KeywordValue(k, c);
            var node = new JsonObject { ["type"] = "object" };
            var allRequired = (root ? new[] { "$schema", "$target" } : []).Concat(required).ToArray();
            if (allRequired.Length > 0) node["required"] = Array(allRequired);
            node["properties"] = properties;
            node["additionalProperties"] = false;
            if (anyOf != null) node["anyOf"] = anyOf;
            var all = KeywordRules([.. keywords], c, position, kind).Concat(rules ?? []).ToArray<JsonNode>();
            if (all.Length == 1) node["allOf"] = new JsonArray(all);
            if (all.Length > 1) node["allOf"] = new JsonArray(Alternative($"{name}.rules", new JsonObject { ["allOf"] = new JsonArray(all) }));
            return node;
        }

        // The largest sets of keywords that do not exclude each other, in keyword order: the maximal independent
        // sets of the graph of exclusions (Bron–Kerbosch on its complement). Rules with only "absent" keywords
        // are exclusions.
        List<HashSet<string>> CompatibleSets(List<string> keywords, string? position, string? kind)
        {
            var conflicts = new HashSet<(string, string)>();
            foreach (var rule in s.Rules.Where(r => r.Excludes && Scoped(r, position, kind)))
                foreach (var a in rule.If.Keys.Where(keywords.Contains))
                    foreach (var b in rule.Absent.Where(keywords.Contains))
                    {
                        conflicts.Add((a, b));
                        conflicts.Add((b, a));
                    }
            var involved = keywords.Where(k => conflicts.Any(c => c.Item1 == k)).ToHashSet();
            var sets = new List<HashSet<string>>();
            void Extend(List<string> chosen, List<string> candidates, List<string> excluded)
            {
                if (candidates.Count == 0 && excluded.Count == 0)
                {
                    sets.Add(keywords.Where(k => !involved.Contains(k) || chosen.Contains(k)).ToHashSet());
                    return;
                }
                foreach (var v in candidates.ToList())
                {
                    bool Compatible(string w) => w != v && !conflicts.Contains((v, w));
                    Extend([.. chosen, v], candidates.Where(Compatible).ToList(), excluded.Where(Compatible).ToList());
                    candidates.Remove(v);
                    excluded.Add(v);
                }
            }
            Extend([], keywords.Where(involved.Contains).ToList(), []);
            return sets;
        }

        static bool Scoped(KeywordRule r, string? position, string? kind) =>
            (r.In == null || r.In == position) && (r.Kinds == null || kind != null && r.Kinds.Contains(kind));

        // The rules of the source definition whose keywords are present here, as far as they are not expressed
        // by separate alternatives already.
        IEnumerable<JsonObject> KeywordRules(HashSet<string> names, Context c, string? position, string? kind)
        {
            foreach (var rule in s.Rules.Where(r => r.HasConditions && Scoped(r, position, kind) && r.If.Keys.All(names.Contains)))
            {
                var condition = Required([.. rule.If.Keys]);
                var values = new JsonObject();
                foreach (var (k, v) in rule.If)
                    if (v != null) values[k] = new JsonObject { ["const"] = v.DeepClone() };
                if (values.Count > 0) condition["properties"] = values;

                var properties = rule.Excludes ? new JsonObject() : Forbid(rule.Absent.Where(names.Contains));
                foreach (var (k, v) in rule.Values.Where(v => names.Contains(v.Key)))
                    properties[k] = v is JsonObject template ? Resolve(template, c) : new JsonObject { ["const"] = v?.DeepClone() };
                var then = new JsonObject();
                if (properties.Count > 0) then["properties"] = properties;
                if (rule.Present.Count > 0) then["required"] = Array([.. rule.Present]);
                if (then.Count > 0) yield return Rule(rule.Id, condition, then);
            }
        }

        // KW-16: a kind alternative applies where `type` is written or a keyword implies its kind (4.11.1).
        // Integers alone imply integer; a non-integral value implies number.
        JsonArray Evidence(string kind, HashSet<string> keywords)
        {
            var number = kind == "number";
            JsonObject NonIntegral() => new() { ["not"] = Type("integer") };
            JsonObject Value(string keyword) => !number ? Required(keyword)
                : keyword == "const" ? Has("const", NonIntegral()) : Has("enum", new JsonObject { ["contains"] = NonIntegral() });

            var evidence = new JsonArray(Required("type"));
            foreach (var k in s.Inference.GetValueOrDefault(kind, []).Where(keywords.Contains))
                evidence.Add(k switch
                {
                    "const" or "enum" => Value(k),
                    "oneOf" when ScalarTypes.Contains(kind) => Has("oneOf", new JsonObject
                    {
                        ["contains"] = new JsonObject { ["anyOf"] = new JsonArray(Value("const"), Value("enum")) },
                    }),
                    _ => Required(k),
                });
            return evidence;
        }

        // The rules of one kind: null literals only next to a nullable type (KW-8, KW-18), literals and bounds
        // within the range of the format (TYP-1, TYP-2, KW-19, REG-4), and textual strings with a format that
        // the target stores natively (KW-25).
        IEnumerable<JsonObject> KindRules(string kind, HashSet<string> keywords)
        {
            if (TypeRow("nullable") && ScalarTypes.Contains(kind) && Restrict(keywords, () => Type(kind), bounds: false) is { Count: > 0 } nonNull)
                yield return new JsonObject
                {
                    ["$comment"] = "KW-8, KW-18",
                    ["if"] = new JsonObject { ["required"] = Array("type"), ["properties"] = new JsonObject { ["type"] = Type("array") } },
                    ["else"] = new JsonObject { ["properties"] = nonNull },
                };

            JsonObject InRange(string format) => new()
            {
                ["properties"] = Restrict(keywords, () => new JsonObject
                {
                    ["minimum"] = s.FormatRanges[format].Minimum.DeepClone(),
                    ["maximum"] = s.FormatRanges[format].Maximum.DeepClone(),
                }, bounds: true),
            };
            if (kind is "integer" or "number" && keywords.Contains("format"))
                foreach (var format in FormatsOf(kind).Where(s.FormatRanges.ContainsKey))
                    yield return Rule(kind == "integer" ? "TYP-1, KW-19" : "TYP-2, KW-19", Has("format", Const(format)), InRange(format));
            if (kind == "integer")
            {
                if (s.ImplicitFormats.TryGetValue(column, out var implicitFormat))
                {
                    yield return Rule("REG-4, KW-19",
                        new JsonObject { ["properties"] = new JsonObject { ["format"] = false, ["textual"] = Const(false) } }, InRange(implicitFormat));
                    if (keywords.Contains("textual"))
                        yield return Rule("TYP-1, KW-19", new JsonObject
                        {
                            ["required"] = Array("textual"),
                            ["properties"] = new JsonObject { ["format"] = false, ["textual"] = Const(true) },
                        }, InRange(s.IntegerDefault));
                }
                else yield return Rule("TYP-1, KW-19", new JsonObject { ["properties"] = new JsonObject { ["format"] = false } }, InRange(s.IntegerDefault));
            }

            if (kind == "string" && keywords.Contains("textual"))
                yield return Rule("KW-25", Required("textual"),
                    new JsonObject { ["required"] = Array("format"), ["properties"] = new JsonObject { ["format"] = Enum(s.TextualFormats.Formats) } });
        }

        // The keywords that hold literal values (and, with bounds, the numeric bounds), each restricted by the
        // given schema. The choices of a labeled enum are not restricted here: editors offer every key that a
        // matching schema names in `properties`, so naming `enum` or `minimum` for the items of `oneOf` would
        // offer them in every choice.
        static JsonObject Restrict(HashSet<string> keywords, Func<JsonObject> value, bool bounds)
        {
            string[] single = ["const", "default", .. bounds ? Bounds : []];
            var restricted = new JsonObject();
            foreach (var k in single.Where(keywords.Contains)) restricted[k] = value();
            foreach (var k in new[] { "enum", "examples" }.Where(keywords.Contains)) restricted[k] = new JsonObject { ["items"] = value() };
            return restricted;
        }

        // The branches of a oneOf: choices of a labeled enum where scalars are allowed (KW-14), and branches of
        // a discriminated union where objects are allowed and the target allows unions (KW-15).
        (JsonNode? Choice, JsonNode? Union) Branches(Context c)
        {
            var scalars = c.Types.Where(ScalarTypes.Contains).ToList();
            JsonNode? choice = scalars.Count switch
            {
                0 => null,
                1 => Def($"choice.{scalars[0]}"),
                _ => new JsonObject { ["anyOf"] = new JsonArray(scalars.Select(t => Def($"choice.{t}")).ToArray()) },
            };
            var union = c.Types.Contains("object") && KeywordTypes(Keyword("oneOf"))!.Contains("object")
                ? Def(MembersOf(c.Layout) == "schema" ? "unionBranch" : "unionBranch." + MembersOf(c.Layout)) : null;
            return (choice, union);
        }

        // A keyword's value; an unrestricted value is written {} rather than true, which editors do not offer.
        JsonNode KeywordValue(string name, Context? c) =>
            Resolve(Keyword(name).Value, c) is { } value && !(value is JsonValue v && v.TryGetValue<bool>(out var b) && b) ? value : new JsonObject();

        // Replaces "@name" references with the definition for this position, and the constant "@draftUri".
        JsonNode? Resolve(JsonNode? template, Context? c)
        {
            switch (template)
            {
                case JsonObject o when o.Count == 1 && o["$ref"] is JsonValue v && v.GetValue<string>().StartsWith('@'):
                    return Placeholder(v.GetValue<string>()[1..], c);
                case JsonObject o:
                    var result = new JsonObject();
                    foreach (var (k, v) in o)
                        result[k] = k == "const" && v is JsonValue cv && cv.TryGetValue<string>(out var constant) && constant.StartsWith('@')
                            ? JsonValue.Create(constant switch
                            {
                                "@draftUri" => s.SchemaUri,
                                _ => throw new InvalidDataException($"unknown constant {constant}"),
                            })
                            : Resolve(v, c);
                    return result;
                case JsonArray a:
                    return new JsonArray(a.Select(x => Resolve(x, c)).ToArray());
                default:
                    return template?.DeepClone();
            }
        }

        JsonNode Placeholder(string name, Context? c)
        {
            if (c == null && name is "member" or "item" or "items" or "properties" or "type" or "format" or "branch" or "literal")
                throw new InvalidDataException($"@{name} needs a position");
            return name switch
            {
                "schema" or "scalar" => Position(name),
                "member" => Position(MembersOf(c!.Layout)),
                "item" => Position(ItemsOf(c!.Layout)),
                "items" => (c!.Keywords?.Contains("prefixItems") ?? KeywordAllowed("prefixItems"))
                    ? new JsonObject { ["anyOf"] = new JsonArray(Const(false), Position(ItemsOf(c.Layout))) }
                    : Position(ItemsOf(c.Layout)),
                "properties" => MemberMap(c!),
                "type" => TypeValue(c!.Types),
                "format" => Enum(c!.Types.SelectMany(FormatsOf)),
                "literal" => Literal(c!.Types),
                "branch" => Branches(c!) switch
                {
                    ({ } choice, { } union) => new JsonObject { ["anyOf"] = new JsonArray(choice, union) },
                    var (choice, union) => choice ?? union ?? throw new InvalidDataException("oneOf without branches"),
                },
                "attributeSet" or "propertyNames" => Def(name),
                "targets" => Enum(targets),
                _ => R(name),
            };
        }

        // A literal value of the given types (2.5): null as well where the target has nullable types; KindRules
        // allow it only next to a written nullable type.
        JsonObject Literal(List<string> types)
        {
            var json = types.ToList();
            if (TypeRow("nullable") && types.Any(ScalarTypes.Contains)) json.Add("null");
            return new JsonObject { ["type"] = json.Count == 1 ? json[0] : Array([.. json]) };
        }

        // The value of `properties`: the declared members, with the names the target allows (MOD-3, XML-3, INI-4).
        JsonObject MemberMap(Context c)
        {
            var members = MembersOf(c.Layout);
            var names = new JsonObject();
            if (!EmptyNameAllowed) names["minLength"] = 1;
            if ((Layout(members).Names ?? s.NamePatterns.GetValueOrDefault(column)) is { } pattern) names["pattern"] = pattern;
            var map = new JsonObject { ["type"] = "object", ["propertyNames"] = names, ["additionalProperties"] = Position(members, member: true) };
            if (c.Layout.MemberCount is { } count)
            {
                map["minProperties"] = count;
                map["maxProperties"] = count;
            }
            return map;
        }

        JsonNode TypeValue(List<string> types)
        {
            var single = new JsonObject { ["enum"] = Array([.. types]) };
            var scalars = ScalarTypes.Where(types.Contains).ToArray();
            if (!TypeRow("nullable") || scalars.Length == 0) return single;
            return new JsonObject
            {
                ["anyOf"] = new JsonArray(single, new JsonObject
                {
                    ["type"] = "array",
                    ["minItems"] = 2,
                    ["maxItems"] = 2,
                    ["uniqueItems"] = true,
                    ["items"] = new JsonObject { ["enum"] = Array([.. scalars, "null"]) },
                    ["contains"] = Const("null"),
                }),
            };
        }
    }

    static JsonObject Dispatcher(SourceDefinition s, string id, string title, string description, Func<string, string> targetRef, JsonObject? defs)
    {
        var dispatch = new JsonArray();
        foreach (var (target, _) in s.Targets)
            dispatch.Add(new JsonObject
            {
                ["if"] = new JsonObject { ["required"] = Array("$target"), ["properties"] = new JsonObject { ["$target"] = Const(target) } },
                ["then"] = Ref(targetRef(target)),
            });
        var dispatcher = new JsonObject
        {
            ["$schema"] = JsonSchemaDialect,
            ["$id"] = id,
            ["title"] = title,
            ["description"] = $"{description} Generated from schemas/source/{s.Version}.yaml. Do not edit.",
            ["type"] = "object",
            ["required"] = Array("$schema", "$target"),
            ["properties"] = new JsonObject
            {
                ["$schema"] = Const(s.SchemaUri),
                ["$target"] = new JsonObject { ["enum"] = Array(s.Targets.Select(t => t.Target).ToArray()) },
            },
            ["allOf"] = dispatch,
        };
        if (defs != null) dispatcher["$defs"] = defs;
        return dispatcher;
    }

    static JsonObject Rule(string id, JsonObject condition, JsonObject then) => new() { ["$comment"] = id, ["if"] = condition, ["then"] = then };

    static JsonObject Has(string keyword, JsonObject value) => new() { ["required"] = Array(keyword), ["properties"] = new JsonObject { [keyword] = value } };

    static JsonObject Forbid(IEnumerable<string> keywords)
    {
        var o = new JsonObject();
        foreach (var k in keywords) o[k] = false;
        return o;
    }

    static JsonObject Required(params string[] keywords) => new() { ["required"] = Array(keywords) };
    static JsonObject Enum(IEnumerable<string> values) => new() { ["enum"] = Array(values.ToArray()) };
    static JsonObject Title() => new() { ["type"] = "string", ["pattern"] = "^[^\\r\\n]*$" };
    static JsonObject LocaleTag() => new() { ["type"] = "string", ["pattern"] = "^[A-Za-z]{2,8}(-[A-Za-z0-9]{1,8})*$" };
    static JsonObject Type(string t) => new() { ["type"] = t };
    static JsonObject Const(string value) => new() { ["const"] = value };
    static JsonObject Const(bool value) => new() { ["const"] = value };
    static JsonObject Ref(string pointer) => new() { ["$ref"] = pointer };
    static JsonArray Array(params string[] values) => new(values.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray());

    static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    static string Serialize(JsonNode node) => node.ToJsonString(Options).Replace("\r\n", "\n") + "\n";
}
