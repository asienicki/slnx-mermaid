using Microsoft.Build.Locator;
using SlnxMermaid.Cli;
using SlnxMermaid.CLI.Exceptions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SlnxMermaid.CLI
{
    internal static class Program
    {
        private static readonly string[] ConfigExampleArguments = ["--config", "slnx-mermaid.yml"];

        private static async Task<int> Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();

            return await RunAsync(args);
        }

        private static async Task<int> RunAsync(string[] args)
        {
            SpectraConsoleHelper.PrintHeader();

            var app = new CommandApp<GenerateCommand>();

            app.Configure(config =>
            {
                config.SetApplicationName("slnx-mermaid");

                config.ValidateExamples();

                config.AddExample();
                config.AddExample(ConfigExampleArguments);

                config.SetExceptionHandler((ex, resolver) =>
                {
                    AnsiConsole.WriteException(ex,
                    ExceptionFormats.ShortenPaths |
                    ExceptionFormats.ShortenTypes |
                    ExceptionFormats.ShortenMethods);
                });

                config.SetExceptionHandler((ex, _) =>
                {
                    switch (ex)
                    {
                        case ConfigurationFileNotFoundException cfg:
                            AnsiConsole.MarkupLine($"[red bold]Configuration file not found:[/] [grey]{cfg.FilePath}[/]");
                            AnsiConsole.MarkupLine($"Expected default file: [grey]slnx-mermaid.yml[/]");
                            break;

                        case SolutionNotFoundException snfe:
                            AnsiConsole.MarkupLine($"[red bold]Solution file not found:[/] [grey]{snfe.FilePath}[/]");
                            break;

                        case YamlDeserializeException yde:
                            AnsiConsole.MarkupLine($"[red bold]Config file is invalid:[/] [grey]{yde.FilePath}[/]");
                            break;

                        case DiagramOutputPathMissingException:
                            AnsiConsole.MarkupLine("[red bold]Diagram output path is missing in the configuration file.[/]");
                            AnsiConsole.MarkupLine("Please ensure that the configuration file contains a valid output path for the generated Mermaid diagram.");
                            break;

                        default:
                            AnsiConsole.WriteException(ex,
                                ExceptionFormats.ShortenPaths |
                                ExceptionFormats.ShortenTypes |
                                ExceptionFormats.ShortenMethods |
                                ExceptionFormats.ShowLinks);
                            break;
                    }
                });
            });

            return await app.RunAsync(args);
        }
    }
}
