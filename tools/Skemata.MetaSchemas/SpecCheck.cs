using System.Text.RegularExpressions;

namespace Skemata.MetaSchemas;

/// <summary>Compares the source definition with tables of the spec (5.2 and chapter 8), so they cannot drift apart.</summary>
public static class SpecCheck
{
    public static List<string> Run(SourceDefinition source, string specDir)
    {
        var errors = new List<string>();

        // 5.2: the range of every integer format
        var ranges = ReadTable(File.ReadAllLines(Path.Combine(specDir, "05-types-and-formats.md")), "| `format` | Range |", 2, errors);
        foreach (var format in source.Formats["integer"])
        {
            if (!ranges.TryGetValue($"`{format}`", out var cells)) { errors.Add($"5.2 has no range for {format}"); continue; }
            var spec = cells[0].Replace(" ", "").Replace('−', '-').Split('…');
            var range = source.FormatRanges.GetValueOrDefault(format);
            if (range == null || spec[0] != range.Minimum.ToJsonString() || spec[1] != range.Maximum.ToJsonString())
                errors.Add($"5.2 range of {format}: spec {cells[0]}, source {range?.Minimum.ToJsonString()} … {range?.Maximum.ToJsonString()}");
        }

        var lines = File.ReadAllLines(Path.Combine(specDir, "08-target-profiles.md"));
        var keywordMatrix = ReadTable(lines, "| Keyword |", source.Columns.Count + 1, errors);
        var typeMatrix = ReadTable(lines, "| Type / format |", source.Columns.Count + 1, errors);
        var targetTable = ReadTable(lines, "| Target |", 6, errors);

        // 8.4: every row, in both directions, with the same columns marked ✓
        foreach (var row in source.Rows)
        {
            if (!keywordMatrix.TryGetValue(row.Cell, out var cells)) { errors.Add($"8.4 has no row {row.Cell}"); continue; }
            var spec = Columns(source, cells, c => c.Contains('✓'));
            if (!spec.SetEquals(row.Allowed)) errors.Add($"8.4 row {row.Cell}: spec allows [{Join(spec)}], source [{Join(row.Allowed)}]");
        }
        foreach (var cell in keywordMatrix.Keys.Where(c => source.Rows.All(r => r.Cell != c)))
            errors.Add($"8.4 row {cell} is missing from the source definition");

        // 8.5: rows that decide whether a type or format is allowed ("—" means not allowed, "— unless `textual`"
        // means allowed only with textual: true)
        foreach (var (name, row) in source.TypeRows)
        {
            if (!typeMatrix.TryGetValue(row.Cell, out var cells)) { errors.Add($"8.5 has no row {row.Cell}"); continue; }
            var spec = Columns(source, cells, c => !c.StartsWith('—'));
            if (!spec.SetEquals(row.Allowed)) errors.Add($"8.5 row {row.Cell} ({name}): spec allows [{Join(spec)}], source [{Join(row.Allowed)}]");
            var textualOnly = Columns(source, cells, c => c.StartsWith("— unless `textual`"));
            if (!textualOnly.SetEquals(row.TextualOnly))
                errors.Add($"8.5 row {row.Cell} ({name}): spec allows only with textual [{Join(textualOnly)}], source [{Join(row.TextualOnly)}]");
        }
        foreach (var (format, row) in source.FormatExceptions)
        {
            if (!typeMatrix.TryGetValue(row.Cell, out var cells)) { errors.Add($"8.5 has no row {row.Cell}"); continue; }
            var spec = Columns(source, cells, c => c == "—");
            if (!spec.SetEquals(row.Allowed)) errors.Add($"8.5 row {row.Cell} ({format}): spec excludes [{Join(spec)}], source [{Join(row.Allowed)}]");
        }
        var textual = source.TextualFormats;
        if (!typeMatrix.TryGetValue(textual.Cell, out var textualCells)) errors.Add($"8.5 has no row {textual.Cell}");
        else
        {
            var spec = Columns(source, textualCells, c => c != "—");
            if (!spec.SetEquals(textual.Allowed)) errors.Add($"8.5 row {textual.Cell}: spec allows [{Join(spec)}], source [{Join(textual.Allowed)}]");
            var formats = textual.Names.Where(n => !n.StartsWith("textual")).ToList();
            if (!formats.SequenceEqual(textual.Formats)) errors.Add($"8.5 row {textual.Cell}: names formats [{Join(formats)}], source [{Join(textual.Formats)}]");
        }

        // 8.3: every target is listed, and its root node kinds are those of the root position of its layout
        foreach (var (target, column) in source.Targets)
        {
            if (!targetTable.TryGetValue($"`{target}`", out var cells)) { errors.Add($"8.3 does not list target {target}"); continue; }
            var spec = Regex.Replace(cells[1], @"\s*\(.*\)", "").Split(" or ").ToHashSet();
            var layout = source.Layouts.GetValueOrDefault(column)?.GetValueOrDefault("root")?.Kinds ?? [];
            if (!spec.SetEquals(layout)) errors.Add($"8.3 target {target}: spec root node [{Join(spec)}], source layout root [{Join(layout)}]");
        }

        return errors;
    }

    /// <summary>The rule IDs that the spec defines (written as **[ID]** in the chapters and target chapters), in spec order.</summary>
    public static List<string> DefinedRules(string specDir) =>
        Directory.GetFiles(specDir, "*.md").Concat(Directory.GetFiles(Path.Combine(specDir, "targets"), "*.md"))
            .OrderBy(f => f, StringComparer.Ordinal)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"\*\*\[([A-Z]+-\d+)\]\*\*").Select(m => m.Groups[1].Value))
            .Distinct().ToList();

    /// <summary>The rule IDs in the first column of the table in spec 9.4.1: rules that the meta-schemas leave to a schema validator.</summary>
    public static HashSet<string> DelegatedRules(string specPath)
    {
        var lines = File.ReadAllLines(specPath);
        var section = Array.FindIndex(lines, l => l.StartsWith("### 9.4.1"));
        var header = section < 0 ? -1 : Array.FindIndex(lines, section, l => l.StartsWith("| Rule |"));
        if (header < 0) throw new InvalidDataException($"{specPath}: table of 9.4.1 not found");
        return lines.Skip(header + 2).TakeWhile(l => l.StartsWith('|'))
            .SelectMany(l => Regex.Matches(l.Split('|')[1], @"[A-Z]+-\d+").Select(m => m.Value))
            .ToHashSet();
    }

    // The rows of the table whose header starts with the given text, keyed by their first cell.
    static Dictionary<string, List<string>> ReadTable(string[] lines, string header, int cellCount, List<string> errors)
    {
        var table = new Dictionary<string, List<string>>();
        var start = Array.FindIndex(lines, l => l.StartsWith(header));
        if (start < 0) { errors.Add($"table starting with '{header}' not found"); return table; }
        foreach (var line in lines.Skip(start + 2).TakeWhile(l => l.StartsWith('|')))
        {
            var cells = line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToList();
            if (cells.Count != cellCount) { errors.Add($"row with {cells.Count} cells instead of {cellCount}: {line}"); continue; }
            table[cells[0]] = cells.Skip(1).ToList();
        }
        return table;
    }

    static HashSet<string> Columns(SourceDefinition s, List<string> cells, Func<string, bool> allowed) =>
        s.Columns.Where((_, i) => allowed(cells[i])).ToHashSet();

    static string Join(IEnumerable<string> xs) => string.Join(", ", xs);
}
