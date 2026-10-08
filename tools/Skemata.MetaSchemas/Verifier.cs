using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Skemata.MetaSchemas;

/// <summary>
/// Checks that the generated files are up to date and validates the worked examples against them.
/// Valid examples must pass. Invalid examples are reported as caught by the meta-schemas or as left to the
/// rules that only a schema validator can check (spec 9.4.1).
/// </summary>
public static class Verifier
{
    public static bool Run(string repo, SourceDefinition source, SortedDictionary<string, string> outputs, string outputDir)
    {
        var ok = true;

        foreach (var (relative, json) in outputs)
        {
            var path = Path.Combine(outputDir, relative);
            if (!File.Exists(path) || File.ReadAllText(path).Replace("\r\n", "\n") != json)
            {
                ok = false;
                Console.WriteLine($"OUT OF DATE schemas/{source.Version}/{relative} (run 'generate')");
            }
        }

        var dispatcher = JsonSchema.FromText(outputs["schema.json"]);
        var perTarget = source.Targets.ToDictionary(t => t.Target, t => JsonSchema.FromText(outputs[$"targets/{t.Target}.json"]));
        var locale = JsonSchema.FromText(outputs["locale.json"]);

        var examples = Path.Combine(repo, "spec", "examples");
        int valid = 0, caught = 0, semantic = 0;
        foreach (var file in Directory.GetFiles(examples, "*.skemata.yaml", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(repo, file).Replace('\\', '/');
            var data = YamlLoader.Load(file);
            var result = Evaluate(dispatcher, data);
            var target = data?["$target"]?.GetValue<string>();
            if (target != null && perTarget.TryGetValue(target, out var targetSchema) && Evaluate(targetSchema, data).IsValid != result.IsValid)
            {
                ok = false;
                Console.WriteLine($"INCONSISTENT {relative}: dispatcher and targets/{target}.json disagree");
            }

            if (!relative.Contains("/invalid/"))
            {
                if (result.IsValid) { valid++; Console.WriteLine($"PASS   {relative}"); }
                else { ok = false; Console.WriteLine($"FAIL   {relative}"); PrintErrors(result); }
            }
            else if (!result.IsValid) { caught++; Console.WriteLine($"CAUGHT {relative}"); }
            else { semantic++; Console.WriteLine($"LEFT   {relative} (rule beyond the meta-schemas, 9.4.1)"); }
        }

        foreach (var file in Directory.GetFiles(examples, "*.skemata-locale.yaml", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(repo, file).Replace('\\', '/');
            var result = Evaluate(locale, YamlLoader.Load(file));
            if (result.IsValid) Console.WriteLine($"PASS   {relative}");
            else { ok = false; Console.WriteLine($"FAIL   {relative}"); PrintErrors(result); }
        }

        Console.WriteLine();
        Console.WriteLine($"{valid} valid examples pass; of the invalid examples, {caught} are caught by the meta-schemas " +
                          $"and {semantic} are left to the schema validator.");
        Console.WriteLine(ok ? "VERIFIED" : "VERIFICATION FAILED");
        return ok;
    }

    static EvaluationResults Evaluate(JsonSchema schema, JsonNode? data)
    {
        using var doc = JsonDocument.Parse(data?.ToJsonString() ?? "null");
        return schema.Evaluate(doc.RootElement.Clone(), new EvaluationOptions { OutputFormat = OutputFormat.List, RequireFormatValidation = true });
    }

    static void PrintErrors(EvaluationResults result)
    {
        foreach (var detail in result.Details ?? [])
            if (detail.Errors != null)
                foreach (var (_, message) in detail.Errors)
                    Console.WriteLine($"         {detail.InstanceLocation}: {message}");
    }
}
