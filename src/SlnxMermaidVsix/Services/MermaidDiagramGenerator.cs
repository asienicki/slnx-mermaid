using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SlnxMermaid.CLI.Exceptions;
using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Emit;
using SlnxMermaid.Core.Extensions;
using SlnxMermaid.Core.Filtering;
using SlnxMermaid.Core.Graph;
using SlnxMermaid.Core.Naming;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SlnxMermaidVsix
{
    /// <summary>
    /// Executes the Mermaid generation pipeline from configuration loading and validation,
    /// through solution graph analysis, to output emission and file persistence.
    /// </summary>
    internal sealed class MermaidDiagramGenerator
    {
        private readonly AsyncPackage package;
        private readonly MermaidOutputService outputService;

        /// <summary>
        /// Creates a diagram generator for the current VSIX package.
        /// </summary>
        public MermaidDiagramGenerator(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            outputService = new MermaidOutputService(package);
        }

        /// <summary>
        /// Generates a Mermaid diagram and writes it to the output path defined in configuration.
        /// </summary>
        public async Task GenerateAsync(
            string configPath,
            IVsOutputWindowPane pane,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await outputService.LogAsync(pane,
                $"Loading config: {configPath}");

            var config = YamlConfigLoader.Load(configPath)
                .Normalize(configPath)
                .Validate();

            await outputService.LogAsync(pane,
                $"Analyzing solution graph: {config.Solution}");

            var nodes = SolutionGraphAnalyzer.Analyze(config.Solution);

            await outputService.LogAsync(pane,
                $"Discovered {nodes.Count} projects.");

            var naming = new NameTransformer(config.Naming);
            var filter = new ProjectFilter(config.Filters.Exclude);
            var emitter = new MermaidEmitter(naming, filter);

            await outputService.LogAsync(pane,
                "Emitting Mermaid diagram...");

            var mermaid = emitter.Emit(nodes, config.Diagram.Direction);
            var markdownDiagram = mermaid.WrapCodeForMarkdown();

            if (string.IsNullOrWhiteSpace(config.Output?.File))
                throw new DiagramOutputPathMissingException();

            var outputFile = config.Output.File;
            var outputDirectory = Path.GetDirectoryName(outputFile);

            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            cancellationToken.ThrowIfCancellationRequested();

            File.WriteAllText(outputFile, markdownDiagram);

            await outputService.LogAsync(pane,
                $"Diagram written to: {outputFile}");

            VsShellUtilities.OpenDocument(package, outputFile);
        }
    }
}
