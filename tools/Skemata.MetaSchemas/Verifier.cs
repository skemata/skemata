using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Schema;

namespace Skemata.MetaSchemas;

/// <summary>
/// Checks that the generated files are up to date, validates the worked examples and the conformance test suite
/// against them, and checks that both are free of DOC-2 and DOC-3 problems. Valid documents must pass. Invalid
/// ones must be caught by the meta-schemas unless spec 9.4.1 leaves their rule to a schema validator: for
/// examples, the rule is the start of the file name; test cases say so with <c>metaSchemas: false</c>.
/// </summary>
public static class Verifier
{
    public static bool Run(string repo, SourceDefinition source, SortedDictionary<string, string> outputs, string outputDir)
    {
        var ok = true;
        void Fail(string message)
        {
            ok = false;
            Console.WriteLine(message);
        }

        foreach (var (relative, json) in outputs)
        {
            var path = Path.Combine(outputDir, relative);
            if (!File.Exists(path) || File.ReadAllText(path).Replace("\r\n", "\n") != json)
                Fail($"OUT OF DATE schemas/{source.Version}/{relative} (run 'generate')");
        }

        // The target meta-schemas go into a local registry first, so that the dispatcher's references to them
        // resolve to the generated files, not to the published site.
        var build = new BuildOptions { SchemaRegistry = new SchemaRegistry() };
        var perColumn = source.Targets.Select(t => t.Column).Distinct()
            .ToDictionary(c => c, c => JsonSchema.FromText(outputs[$"targets/{c}.json"], build));
        var columnOf = source.Targets.ToDictionary(t => t.Target, t => t.Column);
        var dispatcher = JsonSchema.FromText(outputs["schema.json"], build);
        var bundled = JsonSchema.FromText(outputs["schema.bundled.json"], build);
        var locale = JsonSchema.FromText(outputs["locale.json"], build);
        var spec = Path.Combine(repo, "spec");
        var delegated = SpecCheck.DelegatedRules(Path.Combine(spec, "09-versioning-and-conformance.md"));
        var defined = SpecCheck.DefinedRules(spec);
        var covered = new HashSet<string>();

        // Evaluates a schema document with the dispatcher, the bundled dispatcher and its target's meta-schema, and
        // a locale file with the locale meta-schema; all must agree.
        EvaluationResults Check(JsonNode? data, bool isLocale, string name)
        {
            if (isLocale) return Evaluate(locale, data);
            var result = Evaluate(dispatcher, data);
            if (Evaluate(bundled, data).IsValid != result.IsValid) Fail($"INCONSISTENT {name}: schema.json and schema.bundled.json disagree");
            if (data is JsonObject o && o["$target"] is JsonValue t && t.TryGetValue<string>(out var target)
                && columnOf.TryGetValue(target, out var column) && Evaluate(perColumn[column], data).IsValid != result.IsValid)
                Fail($"INCONSISTENT {name}: dispatcher and targets/{column}.json disagree");
            return result;
        }

        // DOC-2 and DOC-3: every example and test file must mean the same under YAML 1.1 and 1.2. Invalid examples
        // of DOC rules break them on purpose.
        var yamlFiles = Directory.GetFiles(Path.Combine(spec, "examples"), "*.yaml", SearchOption.AllDirectories)
            .Where(f => !Regex.IsMatch(Path.GetFileName(f), "^DOC-"))
            .Concat(Directory.Exists(Path.Combine(repo, "tests")) ? Directory.GetFiles(Path.Combine(repo, "tests"), "*.yaml", SearchOption.AllDirectories) : []);
        foreach (var file in yamlFiles.OrderBy(f => f, StringComparer.Ordinal))
            foreach (var problem in YamlLint.Check(file))
                Fail($"YAML   {Path.GetRelativePath(repo, file).Replace('\\', '/')}:{problem}");

        // Worked examples
        var examples = Path.Combine(spec, "examples");
        int valid = 0, caught = 0, left = 0;
        foreach (var file in Directory.GetFiles(examples, "*.skemata*.yaml", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(repo, file).Replace('\\', '/');
            var result = Check(YamlLoader.Load(file), file.EndsWith(".skemata-locale.yaml"), relative);
            if (!relative.Contains("/invalid/"))
            {
                if (result.IsValid) { valid++; Console.WriteLine($"PASS   {relative}"); }
                else { Fail($"FAIL   {relative}"); PrintErrors(result); }
                continue;
            }
            var rule = Regex.Match(Path.GetFileName(file), @"^[A-Z]+-\d+").Value;
            covered.Add(rule);
            if (!result.IsValid) { caught++; Console.WriteLine($"CAUGHT {relative}"); }
            else if (delegated.Contains(rule)) { left++; Console.WriteLine($"LEFT   {relative} ({rule} is left to the schema validator, 9.4.1)"); }
            else Fail($"MISSED {relative}: 9.4.1 does not leave {(rule.Length > 0 ? rule : "its rule")} to the schema validator, so the meta-schemas must catch it");
        }
        Console.WriteLine();
        Console.WriteLine($"Examples: {valid} valid ones pass; of the invalid ones, {caught} are caught by the meta-schemas " +
                          $"and {left} are left to the schema validator.");

        // Conformance test suite
        var suite = Path.Combine(repo, "tests", "schema");
        if (Directory.Exists(suite))
        {
            Console.WriteLine();
            int cases = 0, suiteLeft = 0;
            foreach (var file in Directory.GetFiles(suite, "*.yaml", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(repo, file).Replace('\\', '/');
                var fileCases = 0;
                foreach (var (testCase, index) in ((JsonArray)YamlLoader.Load(file)!).Select((c, i) => ((JsonObject)c!, i)))
                {
                    fileCases++;
                    var name = $"{relative} #{index + 1} \"{testCase["description"]}\"";
                    var expectValid = testCase["valid"]!.GetValue<bool>();
                    var rules = ((JsonArray?)testCase["rules"] ?? []).Select(r => r!.GetValue<string>()).ToList();
                    var meta = testCase["metaSchemas"]?.GetValue<bool>() ?? true;
                    if (rules.Count == 0) Fail($"RULE   {name}: names no rule");
                    foreach (var r in rules.Where(r => !defined.Contains(r))) Fail($"RULE   {name}: {r} is not defined in the spec");
                    if (!expectValid) covered.UnionWith(rules);

                    JsonNode? data;
                    try
                    {
                        data = testCase["document"] is JsonValue text ? YamlLoader.LoadText(text.GetValue<string>(), name) : testCase["schema"]?.DeepClone();
                    }
                    catch (Exception e) when (e is YamlDotNet.Core.YamlException or InvalidDataException)
                    {
                        if (expectValid) Fail($"FAIL   {name}: {e.Message}");
                        continue;
                    }
                    var result = Check(data, testCase["kind"]?.GetValue<string>() == "locale", name);

                    if (expectValid && !result.IsValid) { Fail($"FAIL   {name}: rejected"); PrintErrors(result); }
                    else if (!expectValid && meta && result.IsValid) Fail($"MISSED {name}: not caught by the meta-schemas");
                    else if (!expectValid && !meta)
                    {
                        suiteLeft++;
                        if (!result.IsValid) Fail($"STALE  {name}: marked metaSchemas: false, but the meta-schemas catch it");
                        foreach (var r in rules.Where(r => !delegated.Contains(r)))
                            Fail($"RULE   {name}: marked metaSchemas: false, but 9.4.1 does not leave {r} to the schema validator");
                    }
                }
                cases += fileCases;
                Console.WriteLine($"SUITE  {relative}: {fileCases} cases");
            }
            Console.WriteLine();
            Console.WriteLine($"Test suite: {cases} cases; {suiteLeft} invalid cases are left to the schema validator.");
        }

        // Rules without an invalid example or test case. Many of them concern target data or processors rather
        // than schema documents.
        var uncovered = defined.Where(r => !covered.Contains(r)).ToList();
        Console.WriteLine($"Rules without an invalid example or test case ({uncovered.Count}): {string.Join(" ", uncovered)}");

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
