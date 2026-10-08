using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Skemata.MetaSchemas;

/// <summary>Loads a single-document YAML file as JSON, resolving plain scalars with the YAML 1.2 core schema (DOC-1).</summary>
public static class YamlLoader
{
    public static JsonNode? Load(string path)
    {
        var stream = new YamlStream();
        using var reader = new StreamReader(path);
        stream.Load(reader);
        if (stream.Documents.Count != 1) throw new InvalidDataException($"{path}: expected exactly one YAML document");
        return ToJson(stream.Documents[0].RootNode);
    }

    static JsonNode? ToJson(YamlNode node) => node switch
    {
        YamlScalarNode s => Scalar(s),
        YamlSequenceNode q => new JsonArray(q.Children.Select(ToJson).ToArray()),
        YamlMappingNode m => new JsonObject(m.Children.Select(c =>
            KeyValuePair.Create(((YamlScalarNode)c.Key).Value ?? "", ToJson(c.Value)))),
        _ => throw new InvalidDataException("unsupported YAML node"),
    };

    static JsonNode? Scalar(YamlScalarNode s)
    {
        var v = s.Value ?? "";
        if (s.Style != ScalarStyle.Plain) return JsonValue.Create(v);
        if (Regex.IsMatch(v, @"^(null|Null|NULL|~)?$")) return null;
        if (Regex.IsMatch(v, @"^(true|True|TRUE)$")) return JsonValue.Create(true);
        if (Regex.IsMatch(v, @"^(false|False|FALSE)$")) return JsonValue.Create(false);
        if (Regex.IsMatch(v, @"^[-+]?[0-9]+$"))
            return long.TryParse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l) ? JsonValue.Create(l)
                : JsonValue.Create(decimal.Parse(v, CultureInfo.InvariantCulture));
        if (Regex.IsMatch(v, @"^0x[0-9a-fA-F]+$")) return JsonValue.Create(Convert.ToInt64(v[2..], 16));
        if (Regex.IsMatch(v, @"^0o[0-7]+$")) return JsonValue.Create(Convert.ToInt64(v[2..], 8));
        if (Regex.IsMatch(v, @"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$"))
            return JsonValue.Create(double.Parse(v, CultureInfo.InvariantCulture));
        return JsonValue.Create(v);
    }
}
