using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Skemata.MetaSchemas;

/// <summary>
/// Checks YAML files against DOC-2 and DOC-3 (spec 2.1): no explicit tags, anchors, aliases or merge keys, and
/// no unquoted string that YAML 1.1 or 1.2 would read as something else. The worked examples and the test
/// suite are read by tools with either YAML version, so they must mean the same under both.
/// </summary>
public static class YamlLint
{
    // Plain scalars that YAML 1.1 resolves to a boolean, null, number or timestamp.
    static readonly Regex Yaml11NonString = new(
        "^(?:y|Y|yes|Yes|YES|n|N|no|No|NO|true|True|TRUE|false|False|FALSE|on|On|ON|off|Off|OFF" +
        "|~|null|Null|NULL" +
        "|[-+]?0b[01_]+|[-+]?0[0-7_]+|[-+]?(?:0|[1-9][0-9_]*)|[-+]?0x[0-9a-fA-F_]+|[-+]?[1-9][0-9_]*(?::[0-5]?[0-9])+" +
        "|[-+]?(?:[0-9][0-9_]*)?\\.[0-9.]*(?:[eE][-+][0-9]+)?|[-+]?[0-9][0-9_]*(?::[0-5]?[0-9])+\\.[0-9_]*" +
        "|[-+]?\\.(?:inf|Inf|INF)|\\.(?:nan|NaN|NAN)" +
        "|[0-9]{4}-[0-9]{1,2}-[0-9]{1,2}(?:(?:[Tt]|[ \\t]+)[0-9]{1,2}:[0-9]{2}:[0-9]{2}(?:\\.[0-9]*)?(?:[ \\t]*(?:Z|[-+][0-9]{1,2}(?::[0-9]{2})?))?)?)$");

    // Plain scalars that YAML 1.2 (core schema) resolves to something other than a string.
    static readonly Regex Yaml12NonString = new(
        "^(?:|~|null|Null|NULL|true|True|TRUE|false|False|FALSE|[-+]?[0-9]+|0o[0-7]+|0x[0-9a-fA-F]+" +
        "|[-+]?(?:\\.[0-9]+|[0-9]+(?:\\.[0-9]*)?)(?:[eE][-+]?[0-9]+)?|[-+]?\\.(?:inf|Inf|INF)|\\.(?:nan|NaN|NAN))$");

    /// <summary>The DOC-2 and DOC-3 problems of a YAML file, as "line: message".</summary>
    public static List<string> Check(string path)
    {
        var problems = new List<string>();
        var stream = new YamlStream();
        using (var reader = new StreamReader(path)) stream.Load(reader);
        foreach (var document in stream.Documents) Visit(document.RootNode, isKey: false, problems);
        return problems;
    }

    static void Visit(YamlNode node, bool isKey, List<string> problems)
    {
        var line = node.Start.Line;
        if (!node.Anchor.IsEmpty) problems.Add($"{line}: anchor &{node.Anchor} (DOC-2)");
        if (!node.Tag.IsEmpty) problems.Add($"{line}: explicit tag {node.Tag} (DOC-2)");
        switch (node)
        {
            case YamlScalarNode s when s.Style == YamlDotNet.Core.ScalarStyle.Plain:
                var value = s.Value ?? "";
                if (isKey && value == "<<") problems.Add($"{line}: merge key << (DOC-2)");
                else if (isKey && Yaml12NonString.IsMatch(value)) problems.Add($"{line}: key {value} is not a string unless quoted (DOC-3)");
                else if (!Yaml12NonString.IsMatch(value) && Yaml11NonString.IsMatch(value))
                    problems.Add($"{line}: '{value}' is a string in YAML 1.2 but not in YAML 1.1; quote it (DOC-3)");
                break;
            case YamlSequenceNode q:
                foreach (var child in q.Children) Visit(child, false, problems);
                break;
            case YamlMappingNode m:
                foreach (var (key, child) in m.Children)
                {
                    Visit(key, true, problems);
                    Visit(child, false, problems);
                }
                break;
        }
    }
}
