using SlnxMermaid.CLI.Exceptions;
using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Emit;
using SlnxMermaid.Core.Extensions;
using SlnxMermaid.Core.Filtering;
using SlnxMermaid.Core.Graph;
using SlnxMermaid.Core.Naming;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SlnxMermaid.Cli;

public sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
{
    private const string DefaultConfigFile = "slnx-mermaid.yml";

    public sealed class Settings : CommandSettings
    {
        [CommandOption("--config")]
        [Description("Path to config file (default: slnx-mermaid.yml)")]
        public string? ConfigFile { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var configPath = EnsureThereIsConfig(settings.ConfigFile);

        var config = YamlConfigLoader.Load(configPath)
                .Normalize(configPath)
                .Validate();

        AnsiConsole.MarkupLine($"[green]Using config[/]: [grey]{Path.GetFileName(configPath)}[/]");

        var analyzer = new SolutionGraphAnalyzer();

        var nodes = analyzer.Analyze(config.Solution);

        var naming = new NameTransformer(config.Naming);

        var filter = new ProjectFilter(config.Filters.Exclude);

        var emitter = new MermaidEmitter(naming, filter);

        var mermaid = emitter.Emit(nodes, config.Diagram.Direction);

        await HandleResult(config.Output.File, mermaid, AnsiConsole.MarkupLine, cancellationToken);

        return 0;
    }

    private static string EnsureThereIsConfig(string? configFile)
    {
        var baseDir = Directory.GetCurrentDirectory();

        var rawPath = configFile ?? DefaultConfigFile;

        var configPath = rawPath.ToAbsolute(baseDir);

        if (!File.Exists(configPath))
        {
            throw new ConfigurationFileNotFoundException(configPath);
        }

        return configPath;
    }

    private static async Task HandleResult(
    string? configOutputFile,
    string mermaid,
    Action<string> markupLine,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configOutputFile))
            throw new DiagramOutputPathMissingException();

        var output = mermaid.WrapCodeForMarkdown();

        markupLine(output);

        var dir = Path.GetDirectoryName(configOutputFile);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(configOutputFile, output, cancellationToken);

        markupLine($"[green]Diagram written to[/]: [grey]{configOutputFile}[/]");
    }

}
