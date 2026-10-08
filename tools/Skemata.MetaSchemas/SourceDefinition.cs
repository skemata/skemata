using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Skemata.MetaSchemas;

/// <summary>A row of a spec matrix: its first cell and the columns that allow it.</summary>
public sealed record MatrixRow(string Cell, HashSet<string> Allowed)
{
    /// <summary>The backticked names in the first cell, for example `items`, `minItems`.</summary>
    public List<string> Names { get; } = Regex.Matches(Cell, "`([^`]+)`").Select(m => m.Groups[1].Value).ToList();
}

public sealed record KeywordDefinition(string Name, bool RootOnly, HashSet<string>? Types, JsonNode? Value);

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
    public required List<string> Units { get; init; }
    public required Dictionary<string, string> NamePatterns { get; init; }
    public required List<KeywordDefinition> Keywords { get; init; }
    public required Dictionary<string, List<string>> Positions { get; init; }

    public string SchemaUri => BaseUri + "schema";
    public string LocaleUri => BaseUri + "locale";
    public string TargetUri(string target) => BaseUri + "targets/" + target;

    public static SourceDefinition Load(string path)
    {
        var root = YamlLoader.Load(path) as JsonObject ?? throw new InvalidDataException($"{path}: not a mapping");
        var columns = Strings(root["columns"]);

        HashSet<string> Allowed(JsonNode? n) =>
            n is JsonValue v && v.GetValue<string>() == "all" ? columns.ToHashSet() : Strings(n).ToHashSet();

        return new SourceDefinition
        {
            Version = root["version"]!.GetValue<string>(),
            BaseUri = root["baseUri"]!.GetValue<string>(),
            Columns = columns,
            Targets = ((JsonObject)root["targets"]!).Select(p => (p.Key, p.Value!.GetValue<string>())).ToList(),
            Rows = ((JsonArray)root["rows"]!).Select(r => new MatrixRow(r!["cell"]!.GetValue<string>(), Allowed(r["allowed"]))).ToList(),
            TypeRows = ((JsonObject)root["typeRows"]!).ToDictionary(p => p.Key,
                p => new MatrixRow(p.Value!["cell"]!.GetValue<string>(), Allowed(p.Value["allowed"]))),
            Formats = ((JsonObject)root["formats"]!).ToDictionary(p => p.Key, p => Strings(p.Value)),
            FormatExceptions = ((JsonObject)root["formatExceptions"]!).ToDictionary(p => p.Key,
                p => new MatrixRow(p.Value!["cell"]!.GetValue<string>(), Strings(p.Value["notAllowed"]).ToHashSet())),
            Units = Strings(root["units"]),
            NamePatterns = ((JsonObject?)root["namePatterns"] ?? []).ToDictionary(p => p.Key, p => p.Value!.GetValue<string>()),
            Keywords = ((JsonObject)root["keywords"]!).Select(p => new KeywordDefinition(
                p.Key,
                p.Value!["position"]?.GetValue<string>() == "root",
                p.Value["types"] is JsonArray t ? Strings(t).ToHashSet() : null,
                p.Value["value"]?.DeepClone())).ToList(),
            Positions = ((JsonObject)root["positions"]!).ToDictionary(p => p.Key, p => Strings(p.Value)),
        };
    }

    static List<string> Strings(JsonNode? n) => ((JsonArray?)n ?? []).Select(x => x!.GetValue<string>()).ToList();
}
