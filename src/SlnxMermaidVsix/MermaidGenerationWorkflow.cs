using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SlnxMermaidVsix
{
    internal sealed class MermaidGenerationWorkflow
    {
        private readonly AsyncPackage package;
        private readonly DTE dte;
        private readonly MermaidOutputService outputService;
        private readonly MermaidConfigBootstrapper configBootstrapper;
        private readonly MermaidDiagramGenerator diagramGenerator;

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

        public async Task RunAsync(IVsOutputWindowPane pane, CancellationToken cancellationToken)
        {
            await this.outputService.LogAsync(pane,
                "Generate Mermaid Diagram command invoked.");

            try
            {
                var context = this.TryBuildContext();

                if (context == null)
                {
                    await this.HandleMissingSolutionAsync(pane);
                    return;
                }

                await this.GenerateDiagramAsync(context, pane, cancellationToken);
                await this.HandleSuccessAsync(pane);
            }
            catch (Exception ex)
            {
                await this.HandleFailureAsync(pane, ex);
            }
        }

        private MermaidGenerationContext TryBuildContext()
        {
            var solutionPath = this.dte.Solution?.FullName;

            if (string.IsNullOrWhiteSpace(solutionPath))
                return null;

            var solutionDirectory = Path.GetDirectoryName(solutionPath);

            if (string.IsNullOrWhiteSpace(solutionDirectory))
                throw new InvalidOperationException("Unable to determine the solution directory.");

            var configPath = Path.Combine(solutionDirectory, "slnx-mermaid.yml");

            return new MermaidGenerationContext(solutionPath, configPath);
        }

        private async Task GenerateDiagramAsync(
            MermaidGenerationContext context,
            IVsOutputWindowPane pane,
            CancellationToken cancellationToken)
        {
            await this.configBootstrapper.EnsureConfigFileExistsAsync(
                context.ConfigPath,
                context.SolutionPath,
                pane,
                cancellationToken);

            await this.outputService.LogAsync(
                pane,
                $"Selected solution: {context.SolutionPath} {Environment.NewLine} Invoking SlnxMermaid.Core with argument: --config \"{context.ConfigPath}\"");

            await Task.Run(async () =>
            {
                await this.diagramGenerator.GenerateAsync(
                    context.ConfigPath,
                    pane,
                    cancellationToken);
            }, cancellationToken);
        }

        private async Task HandleSuccessAsync(IVsOutputWindowPane pane)
        {
            const string successMessage = "Mermaid diagram generation completed successfully.";

            await this.outputService.LogAsync(pane, successMessage);
            await this.outputService.SendMessageToStatusBarAsync(successMessage);
        }

        private async Task HandleFailureAsync(IVsOutputWindowPane pane, Exception ex)
        {
            await this.outputService.LogAsync(pane, $"Generation failed: {ex}");

            VsShellUtilities.ShowMessageBox(
                this.package,
                $"Mermaid diagram generation failed. See '{MermaidOutputService.OutputPaneTitle}' in the Output window for details.\n\n{ex.Message}",
                "Slnx Mermaid",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private async Task HandleMissingSolutionAsync(IVsOutputWindowPane pane)
        {
            await this.outputService.LogAsync(
                pane,
                "Generation skipped because no solution is currently loaded.");

            VsShellUtilities.ShowMessageBox(
                this.package,
                "Open a solution file first, then run 'Generate Mermaid Diagram'.",
                "Slnx Mermaid",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private sealed class MermaidGenerationContext
        {
            public MermaidGenerationContext(string solutionPath, string configPath)
            {
                this.SolutionPath = solutionPath;
                this.ConfigPath = configPath;
            }

            public string SolutionPath { get; }

            public string ConfigPath { get; }
        }
    }
}
