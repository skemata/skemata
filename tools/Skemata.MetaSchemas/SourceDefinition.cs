using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Skemata.MetaSchemas;

/// <summary>A row of a spec matrix: its first cell and the columns that allow it.</summary>
public sealed record MatrixRow(string Cell, HashSet<string> Allowed)
{
    /// <summary>The backticked names in the first cell, for example `items`, `minItems`.</summary>
    public List<string> Names { get; } = Regex.Matches(Cell, "`([^`]+)`").Select(m => m.Groups[1].Value).ToList();

    /// <summary>The types the row's keywords apply to where this row allows them; null: their usual types.</summary>
    public HashSet<string>? Types { get; init; }

    /// <summary>Columns that allow the row only with <c>textual: true</c> ("— unless `textual`" in 8.5).</summary>
    public HashSet<string> TextualOnly { get; init; } = [];

    /// <summary>The formats the row is about.</summary>
    public List<string> Formats { get; init; } = [];
}

public sealed record KeywordDefinition(string Name, string? Position, HashSet<string>? Types, JsonNode? Value)
{
    /// <summary>Allowed only in the root schema (KW-3).</summary>
    public bool RootOnly => Position == "root";

    /// <summary>Allowed only on declared members, the values of <c>properties</c> (UI-5, KW-24).</summary>
    public bool MemberOnly => Position == "member";
}

/// <summary>
/// A subschema position of a target (target chapters): the node kinds allowed there, the positions of its
/// members and items, and the keywords it forbids, requires or restricts.
/// </summary>
public sealed record LayoutPosition(
    List<string> Kinds, string? Members, string? Items, string? Names, int? MemberCount,
    HashSet<string> Absent, List<string> Present, Dictionary<string, JsonNode?> Values, string? Rule);

/// <summary>The keywords allowed at a special position, and those of which it needs at least one.</summary>
public sealed record SpecialPosition(List<string> Keywords, List<string> RequireAny);

/// <summary>
/// A rule between the keywords of one subschema: where the keywords of <see cref="If"/> are present (with
/// the given values, if not null), <see cref="Absent"/> must be missing, <see cref="Present"/> must be
/// there, and <see cref="Values"/> fixes values. <see cref="In"/> limits the rule to a special position,
/// <see cref="Kinds"/> to subschemas of these types.
/// </summary>
public sealed record KeywordRule(
    string Id, string? In, HashSet<string>? Kinds, Dictionary<string, JsonNode?> If, List<string> Absent, List<string> Present,
    Dictionary<string, JsonNode?> Values)
{
    /// <summary>Only presence conditions: its <see cref="Absent"/> keywords exclude the <see cref="If"/> keywords.</summary>
    public bool Excludes => If.Values.All(v => v == null) && Absent.Count > 0;

    /// <summary>Whether anything is left for an if/then once the exclusions are expressed by separate alternatives.</summary>
    public bool HasConditions => !Excludes || Present.Count > 0 || Values.Count > 0;
}

/// <summary>The smallest and largest value of a format (spec 5.2, 5.3).</summary>
public sealed record FormatRange(JsonNode Minimum, JsonNode Maximum);

/// <summary>The parsed content of schemas/source/&lt;version&gt;.yaml.</summary>
public sealed class SourceDefinition
{
    public required string Version { get; init; }
    public required string BaseUri { get; init; }
    public required List<string> Columns { get; init; }
    public required List<(string Target, string Column)> Targets { get; init; }
    public required List<MatrixRow> Rows { get; init; }
    public required Dictionary<string, MatrixRow> TypeRows { get; init; }
    public required Dictionary<string, List<string>> Formats { get; init; }
    public required Dictionary<string, MatrixRow> FormatExceptions { get; init; }
    public required MatrixRow TextualFormats { get; init; }
    public required Dictionary<string, FormatRange> FormatRanges { get; init; }
    public required string IntegerDefault { get; init; }
    public required Dictionary<string, string> ImplicitFormats { get; init; }
    public required Dictionary<string, List<string>> Inference { get; init; }
    public required List<string> Units { get; init; }
    public required Dictionary<string, string> NamePatterns { get; init; }
    public required Dictionary<string, Dictionary<string, LayoutPosition>> Layouts { get; init; }
    public required List<KeywordDefinition> Keywords { get; init; }
    public required List<KeywordRule> Rules { get; init; }
    public required List<string> RefSiblings { get; init; }
    public required Dictionary<string, SpecialPosition> Positions { get; init; }

    public string SchemaUri => BaseUri + "schema";
    public string BundleUri => BaseUri + "schema.bundled";
    public string LocaleUri => BaseUri + "locale";
    public string TargetUri(string target) => BaseUri + "targets/" + target;

    public static SourceDefinition Load(string path)
    {
        var root = YamlLoader.Load(path) as JsonObject ?? throw new InvalidDataException($"{path}: not a mapping");
        var columns = Strings(root["columns"]);

        HashSet<string> Allowed(JsonNode? n) =>
            n is JsonValue v && v.GetValue<string>() == "all" ? columns.ToHashSet() : Strings(n).ToHashSet();

        MatrixRow Row(JsonNode n, string allowedKey = "allowed") => new(n["cell"]!.GetValue<string>(), Allowed(n[allowedKey]))
        {
            Types = n["types"] is JsonArray t ? Strings(t).ToHashSet() : null,
            TextualOnly = Strings(n["textualOnly"]).ToHashSet(),
            Formats = Strings(n["formats"]),
        };

        var positions = (JsonObject)root["positions"]!;
        return new SourceDefinition
        {
            Version = root["version"]!.GetValue<string>(),
            BaseUri = root["baseUri"]!.GetValue<string>(),
            Columns = columns,
            Targets = ((JsonObject)root["targets"]!).Select(p => (p.Key, p.Value!.GetValue<string>())).ToList(),
            Rows = ((JsonArray)root["rows"]!).Select(r => Row(r!)).ToList(),
            TypeRows = ((JsonObject)root["typeRows"]!).ToDictionary(p => p.Key, p => Row(p.Value!)),
            Formats = ((JsonObject)root["formats"]!).ToDictionary(p => p.Key, p => Strings(p.Value)),
            FormatExceptions = ((JsonObject)root["formatExceptions"]!).ToDictionary(p => p.Key, p => Row(p.Value!, "notAllowed")),
            TextualFormats = Row(root["textualFormats"]!),
            FormatRanges = ((JsonObject)root["formatRanges"]!).ToDictionary(p => p.Key,
                p => new FormatRange(p.Value![0]!.DeepClone(), p.Value[1]!.DeepClone())),
            IntegerDefault = root["integerDefault"]!.GetValue<string>(),
            ImplicitFormats = ((JsonObject?)root["implicitFormats"] ?? []).ToDictionary(p => p.Key, p => p.Value!.GetValue<string>()),
            Inference = ((JsonObject)root["inference"]!).ToDictionary(p => p.Key, p => Strings(p.Value)),
            Units = Strings(root["units"]),
            NamePatterns = ((JsonObject?)root["namePatterns"] ?? []).ToDictionary(p => p.Key, p => p.Value!.GetValue<string>()),
            Layouts = ((JsonObject?)root["layouts"] ?? []).ToDictionary(c => c.Key,
                c => ((JsonObject)c.Value!).ToDictionary(p => p.Key, p => Layout((JsonObject)p.Value!))),
            Keywords = ((JsonObject)root["keywords"]!).Select(p => new KeywordDefinition(
                p.Key,
                p.Value!["position"]?.GetValue<string>(),
                p.Value["types"] is JsonArray t ? Strings(t).ToHashSet() : null,
                p.Value["value"]?.DeepClone())).ToList(),
            Rules = ((JsonArray?)root["rules"] ?? []).Select(r => Rule((JsonObject)r!)).ToList(),
            RefSiblings = Strings(positions["refSiblings"]),
            Positions = positions.Where(p => p.Key != "refSiblings").ToDictionary(p => p.Key, p => new SpecialPosition(
                Strings(p.Value!["keywords"]), Strings(p.Value["requireAny"]))),
        };
    }

    static LayoutPosition Layout(JsonObject o) => new(
        Strings(o["kinds"]),
        o["members"]?.GetValue<string>(),
        o["items"]?.GetValue<string>(),
        o["names"]?.GetValue<string>(),
        o["memberCount"] is JsonValue count ? (int)count.GetValue<long>() : null,
        Strings(o["absent"]).ToHashSet(),
        Strings(o["present"]),
        Map(o["values"]),
        o["rule"]?.GetValue<string>());

    static KeywordRule Rule(JsonObject o) => new(
        o["id"]!.GetValue<string>(),
        o["in"]?.GetValue<string>(),
        o["kinds"] is JsonArray kinds ? Strings(kinds).ToHashSet() : null,
        o["if"] is JsonArray names ? Strings(names).ToDictionary(n => n, _ => (JsonNode?)null) : Map(o["if"]),
        Strings(o["absent"]),
        Strings(o["present"]),
        Map(o["values"]));

    static Dictionary<string, JsonNode?> Map(JsonNode? n) =>
        ((JsonObject?)n ?? []).ToDictionary(p => p.Key, p => p.Value?.DeepClone());

    static List<string> Strings(JsonNode? n) => ((JsonArray?)n ?? []).Select(x => x!.GetValue<string>()).ToList();
}
