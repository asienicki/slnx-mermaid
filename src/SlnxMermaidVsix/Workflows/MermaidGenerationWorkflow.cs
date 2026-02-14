using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SlnxMermaidVsix
{
    /// <summary>
    /// Orchestrates the diagram generation flow: validates solution context,
    /// prepares configuration, runs generation, and handles success/error user feedback.
    /// </summary>
    internal sealed class MermaidGenerationWorkflow
    {
        private readonly AsyncPackage package;
        private readonly DTE dte;
        private readonly MermaidOutputService outputService;
        private readonly MermaidConfigBootstrapper configBootstrapper;
        private readonly MermaidDiagramGenerator diagramGenerator;

        /// <summary>
        /// Creates a workflow that coordinates services required for diagram generation.
        /// </summary>
        public MermaidGenerationWorkflow(
            AsyncPackage package,
            DTE dte,
            MermaidOutputService outputService,
            MermaidConfigBootstrapper configBootstrapper,
            MermaidDiagramGenerator diagramGenerator)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.dte = dte ?? throw new ArgumentNullException(nameof(dte));
            this.outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
            this.configBootstrapper = configBootstrapper ?? throw new ArgumentNullException(nameof(configBootstrapper));
            this.diagramGenerator = diagramGenerator ?? throw new ArgumentNullException(nameof(diagramGenerator));
        }

        /// <summary>
        /// Runs the generation workflow with exception handling and user notifications.
        /// </summary>
        public async Task RunAsync(IVsOutputWindowPane pane, CancellationToken cancellationToken)
        {
            await outputService.LogAsync(pane,
                "Generate Mermaid Diagram command invoked.");

            try
            {
                var context = TryBuildContext();

                if (context == null)
                {
                    await HandleMissingSolutionAsync(pane);
                    return;
                }

                await GenerateDiagramAsync(context, pane, cancellationToken);
                await HandleSuccessAsync(pane);
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(pane, ex);
            }
        }

        /// <summary>
        /// Builds execution context from the currently open solution.
        /// Returns <c>null</c> when no solution is open.
        /// </summary>
        private MermaidGenerationContext TryBuildContext()
        {
            var solutionPath = dte.Solution?.FullName;

            if (string.IsNullOrWhiteSpace(solutionPath))
                return null;

            var solutionDirectory = Path.GetDirectoryName(solutionPath);

            if (string.IsNullOrWhiteSpace(solutionDirectory))
                throw new InvalidOperationException("Unable to determine the solution directory.");

            var configPath = Path.Combine(solutionDirectory, "slnx-mermaid.yml");

            return new MermaidGenerationContext(solutionPath, configPath);
        }

        /// <summary>
        /// Executes generation by ensuring configuration exists and invoking the generator in background.
        /// </summary>
        private async Task GenerateDiagramAsync(
            MermaidGenerationContext context,
            IVsOutputWindowPane pane,
            CancellationToken cancellationToken)
        {
            await configBootstrapper.EnsureConfigFileExistsAsync(
                context.ConfigPath,
                context.SolutionPath,
                pane,
                cancellationToken);

            await outputService.LogAsync(
                pane,
                $"Selected solution: {context.SolutionPath} {Environment.NewLine} Invoking SlnxMermaid.Core with argument: --config \"{context.ConfigPath}\"");

            await Task.Run(async () =>
            {
                await diagramGenerator.GenerateAsync(
                    context.ConfigPath,
                    pane,
                    cancellationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// Handles successful completion of diagram generation.
        /// </summary>
        private async Task HandleSuccessAsync(IVsOutputWindowPane pane)
        {
            const string successMessage = "Mermaid diagram generation completed successfully.";

            await outputService.LogAsync(pane, successMessage);
            await outputService.SendMessageToStatusBarAsync(successMessage);
        }

        /// <summary>
        /// Handles generation failure by logging details and showing an error message.
        /// </summary>
        private async Task HandleFailureAsync(IVsOutputWindowPane pane, Exception ex)
        {
            await outputService.LogAsync(pane, $"Generation failed: {ex}");

            VsShellUtilities.ShowMessageBox(
                package,
                $"Mermaid diagram generation failed. See '{MermaidOutputService.OutputPaneTitle}' in the Output window for details.\n\n{ex.Message}",
                "Slnx Mermaid",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        /// <summary>
        /// Handles command execution when no solution is currently open.
        /// </summary>
        private async Task HandleMissingSolutionAsync(IVsOutputWindowPane pane)
        {
            await outputService.LogAsync(
                pane,
                "Generation skipped because no solution is currently loaded.");

            VsShellUtilities.ShowMessageBox(
                package,
                "Open a solution file first, then run 'Generate Mermaid Diagram'.",
                "Slnx Mermaid",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        /// <summary>
        /// Represents the context required for a single workflow execution.
        /// </summary>
        private sealed class MermaidGenerationContext
        {
            /// <summary>
            /// Creates execution context from solution and configuration paths.
            /// </summary>
            public MermaidGenerationContext(string solutionPath, string configPath)
            {
                SolutionPath = solutionPath;
                ConfigPath = configPath;
            }

            /// <summary>
            /// Full path of the currently open solution.
            /// </summary>
            public string SolutionPath { get; }

            /// <summary>
            /// Full path to the <c>slnx-mermaid.yml</c> configuration file.
            /// </summary>
            public string ConfigPath { get; }
        }
    }
}
