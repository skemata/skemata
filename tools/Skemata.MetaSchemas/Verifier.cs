using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Schema;

namespace Skemata.MetaSchemas;

/// <summary>
/// Checks that the generated files are up to date and validates the worked examples against them.
/// Valid examples must pass. Invalid examples must be caught by the meta-schemas, unless the rule they break
/// (the start of their file name) is one that spec 9.4.1 leaves to a schema validator.
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

        // The target meta-schemas go into a local registry first, so that the dispatcher's references to them
        // resolve to the generated files, not to the published site.
        var build = new BuildOptions { SchemaRegistry = new SchemaRegistry() };
        var perTarget = source.Targets.ToDictionary(t => t.Target, t => JsonSchema.FromText(outputs[$"targets/{t.Target}.json"], build));
        var dispatcher = JsonSchema.FromText(outputs["schema.json"], build);
        var bundled = JsonSchema.FromText(outputs["schema.bundled.json"], build);
        var locale = JsonSchema.FromText(outputs["locale.json"], build);
        var delegated = SpecCheck.DelegatedRules(Path.Combine(repo, "spec", "09-versioning-and-conformance.md"));

        var examples = Path.Combine(repo, "spec", "examples");
        int valid = 0, caught = 0, semantic = 0;
        foreach (var file in Directory.GetFiles(examples, "*.skemata.yaml", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(repo, file).Replace('\\', '/');
            var data = YamlLoader.Load(file);
            var result = Evaluate(dispatcher, data);
            var target = data?["$target"]?.GetValue<string>();
            if (Evaluate(bundled, data).IsValid != result.IsValid)
            {
                ok = false;
                Console.WriteLine($"INCONSISTENT {relative}: schema.json and schema.bundled.json disagree");
            }
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
            else if (Regex.Match(Path.GetFileName(file), @"^[A-Z]+-\d+").Value is var rule && delegated.Contains(rule))
            {
                semantic++;
                Console.WriteLine($"LEFT   {relative} ({rule} is left to the schema validator, 9.4.1)");
            }
            else
            {
                ok = false;
                Console.WriteLine($"MISSED {relative}: 9.4.1 does not leave {(rule.Length > 0 ? rule : "its rule")} to the schema validator, so the meta-schemas must catch it");
            }
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
