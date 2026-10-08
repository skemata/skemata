namespace Skemata.MetaSchemas;

/// <summary>Compares the source definition with the matrices 8.4 and 8.5 of the spec, so they cannot drift apart.</summary>
public static class SpecCheck
{
    public static List<string> Run(SourceDefinition source, string specPath)
    {
        var errors = new List<string>();
        var lines = File.ReadAllLines(specPath);
        var keywordMatrix = ReadTable(lines, "| Keyword |", source.Columns.Count, errors);
        var typeMatrix = ReadTable(lines, "| Type / format |", source.Columns.Count, errors);

        // 8.4: every row, in both directions, with the same columns marked ✓
        foreach (var row in source.Rows)
        {
            if (!keywordMatrix.TryGetValue(row.Cell, out var cells)) { errors.Add($"8.4 has no row {row.Cell}"); continue; }
            var spec = Columns(source, cells, c => c.Contains('✓'));
            if (!spec.SetEquals(row.Allowed)) errors.Add($"8.4 row {row.Cell}: spec allows [{Join(spec)}], source [{Join(row.Allowed)}]");
        }
        foreach (var cell in keywordMatrix.Keys.Where(c => source.Rows.All(r => r.Cell != c)))
            errors.Add($"8.4 row {cell} is missing from the source definition");

        // 8.5: rows that decide whether a type or format is allowed ("—" means not allowed)
        foreach (var (name, row) in source.TypeRows)
        {
            if (!typeMatrix.TryGetValue(row.Cell, out var cells)) { errors.Add($"8.5 has no row {row.Cell}"); continue; }
            var spec = Columns(source, cells, c => c != "—");
            if (!spec.SetEquals(row.Allowed)) errors.Add($"8.5 row {row.Cell} ({name}): spec allows [{Join(spec)}], source [{Join(row.Allowed)}]");
        }
        foreach (var (format, row) in source.FormatExceptions)
        {
            if (!typeMatrix.TryGetValue(row.Cell, out var cells)) { errors.Add($"8.5 has no row {row.Cell}"); continue; }
            var spec = Columns(source, cells, c => c == "—");
            if (!spec.SetEquals(row.Allowed)) errors.Add($"8.5 row {row.Cell} ({format}): spec excludes [{Join(spec)}], source [{Join(row.Allowed)}]");
        }

        // The target table (8.3) must name every target of the source
        foreach (var (target, _) in source.Targets)
            if (!lines.Any(l => l.StartsWith($"| `{target}` |"))) errors.Add($"8.3 does not list target {target}");

        return errors;
    }

    static Dictionary<string, List<string>> ReadTable(string[] lines, string header, int columnCount, List<string> errors)
    {
        var table = new Dictionary<string, List<string>>();
        var start = Array.FindIndex(lines, l => l.StartsWith(header));
        if (start < 0) { errors.Add($"table starting with '{header}' not found"); return table; }
        foreach (var line in lines.Skip(start + 2).TakeWhile(l => l.StartsWith('|')))
        {
            var cells = line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToList();
            if (cells.Count != columnCount + 1) { errors.Add($"row with {cells.Count - 1} columns: {line}"); continue; }
            table[cells[0]] = cells.Skip(1).ToList();
        }
        return table;
    }

    static HashSet<string> Columns(SourceDefinition s, List<string> cells, Func<string, bool> allowed) =>
        s.Columns.Where((_, i) => allowed(cells[i])).ToHashSet();

    static string Join(IEnumerable<string> xs) => string.Join(", ", xs);
}
