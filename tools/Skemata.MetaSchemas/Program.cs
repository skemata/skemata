// Generates the Skemata meta-schemas from schemas/source/<version>.yaml (spec chapter 9.4) and verifies them.
//
//   dotnet run --project tools/Skemata.MetaSchemas -- generate   writes schemas/<version>/
//   dotnet run --project tools/Skemata.MetaSchemas -- verify     checks that the files are up to date and
//                                                             validates spec/examples against them
//
// Both commands first check the source definition against the tables of spec/05-types-and-formats.md and
// spec/08-target-profiles.md.

using Skemata.MetaSchemas;

var command = args.FirstOrDefault() ?? "generate";
var repo = FindRepository(Directory.GetCurrentDirectory());
if (repo == null)
{
    Console.Error.WriteLine("Run this tool inside the Skemata repository (schemas/source not found).");
    return 2;
}

var source = SourceDefinition.Load(Path.Combine(repo, "schemas", "source", "draft-01.yaml"));
var specErrors = SpecCheck.Run(source, Path.Combine(repo, "spec"));
if (specErrors.Count > 0)
{
    Console.Error.WriteLine("The source definition does not match the spec:");
    foreach (var e in specErrors) Console.Error.WriteLine("  " + e);
    return 1;
}
Console.WriteLine("Source definition matches the tables of the spec (5.2, 8.3, 8.4, 8.5).");

var outputs = Generator.Generate(source);
var outputDir = Path.Combine(repo, "schemas", source.Version);

switch (command)
{
    case "generate":
        foreach (var (relative, json) in outputs)
        {
            var path = Path.Combine(outputDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
            Console.WriteLine($"wrote schemas/{source.Version}/{relative}");
        }
        return 0;

    case "verify":
        return Verifier.Run(repo, source, outputs, outputDir) ? 0 : 1;

    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Use 'generate' or 'verify'.");
        return 2;
}

static string? FindRepository(string start)
{
    for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
        if (Directory.Exists(Path.Combine(dir.FullName, "schemas", "source"))) return dir.FullName;
    return null;
}
