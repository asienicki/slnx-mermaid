using SlnxMermaid.Core.Config;

var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
var outputPath = args.Length > 0
    ? Path.GetFullPath(args[0], Directory.GetCurrentDirectory())
    : Path.Combine(repositoryRoot, "slnx-mermaid.schema.json");
var generatedSchema = ConfigurationSchemaGenerator.Generate();

if (File.Exists(outputPath) && File.ReadAllText(outputPath) == generatedSchema)
{
    Console.WriteLine($"Schema is up to date: {Path.GetRelativePath(repositoryRoot, outputPath)}");
    return;
}

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, generatedSchema);
Console.WriteLine($"Generated {Path.GetRelativePath(repositoryRoot, outputPath)}");

static string FindRepositoryRoot(string startDirectory)
{
    var directory = new DirectoryInfo(startDirectory);
    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SlnxMermaid.slnx")))
        directory = directory.Parent;

    return directory?.FullName
        ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
}
