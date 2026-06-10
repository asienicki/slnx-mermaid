using SlnxMermaid.Core.Config;

var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
var outputPath = args.Length > 0
    ? Path.GetFullPath(args[0], Directory.GetCurrentDirectory())
    : Path.Combine(repositoryRoot, "schemas", "slnx-mermaid.schema.json");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, ConfigurationSchemaGenerator.Generate());
Console.WriteLine($"Generated {Path.GetRelativePath(repositoryRoot, outputPath)}");

static string FindRepositoryRoot(string startDirectory)
{
    var directory = new DirectoryInfo(startDirectory);
    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SlnxMermaid.slnx")))
        directory = directory.Parent;

    return directory?.FullName
        ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
}
