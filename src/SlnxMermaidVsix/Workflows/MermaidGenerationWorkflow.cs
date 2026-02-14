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
    /// Orkiestruje przebieg generowania diagramu: walidację kontekstu rozwiązania,
    /// przygotowanie konfiguracji, uruchomienie generatora oraz obsługę sukcesu i błędów.
    /// </summary>
    internal sealed class MermaidGenerationWorkflow
    {
        private readonly AsyncPackage package;
        private readonly DTE dte;
        private readonly MermaidOutputService outputService;
        private readonly MermaidConfigBootstrapper configBootstrapper;
        private readonly MermaidDiagramGenerator diagramGenerator;

        /// <summary>
        /// Tworzy workflow łączący usługi potrzebne do wykonania komendy generowania diagramu.
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
        /// Uruchamia workflow generowania diagramu wraz z obsługą wyjątków i komunikatów użytkownika.
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
        /// Buduje kontekst wykonania na podstawie aktualnie otwartego rozwiązania.
        /// Zwraca <c>null</c>, gdy rozwiązanie nie jest otwarte.
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
        /// Wykonuje właściwe generowanie: zapewnia konfigurację i uruchamia generator w tle.
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
        /// Obsługuje scenariusz poprawnego zakończenia generowania diagramu.
        /// </summary>
        private async Task HandleSuccessAsync(IVsOutputWindowPane pane)
        {
            const string successMessage = "Mermaid diagram generation completed successfully.";

            await outputService.LogAsync(pane, successMessage);
            await outputService.SendMessageToStatusBarAsync(successMessage);
        }

        /// <summary>
        /// Obsługuje błąd generowania: zapisuje szczegóły do logu i pokazuje komunikat błędu.
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
        /// Obsługuje przypadek uruchomienia komendy bez otwartego rozwiązania.
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
        /// Reprezentuje zebrany kontekst potrzebny do pojedynczego wykonania workflow.
        /// </summary>
        private sealed class MermaidGenerationContext
        {
            /// <summary>
            /// Tworzy kontekst wykonania na podstawie ścieżki rozwiązania i ścieżki konfiguracji.
            /// </summary>
            public MermaidGenerationContext(string solutionPath, string configPath)
            {
                SolutionPath = solutionPath;
                ConfigPath = configPath;
            }

            /// <summary>
            /// Pełna ścieżka aktualnie otwartego rozwiązania.
            /// </summary>
            public string SolutionPath { get; }

            /// <summary>
            /// Pełna ścieżka pliku konfiguracji `slnx-mermaid.yml`.
            /// </summary>
            public string ConfigPath { get; }
        }
    }
}
