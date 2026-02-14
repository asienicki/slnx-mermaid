using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SlnxMermaidVsix
{
    internal sealed class MermaidCommand
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet =
            new Guid("b3477400-4987-402f-9e32-eb89274610d6");

        private readonly AsyncPackage package;
        private readonly MermaidOutputService outputService;
        private readonly MermaidConfigBootstrapper configBootstrapper;
        private readonly MermaidDiagramGenerator diagramGenerator;

        private DTE dte;

        public static MermaidCommand Instance { get; private set; }

        private MermaidCommand(
            AsyncPackage package,
            OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.outputService = new MermaidOutputService(package);
            this.configBootstrapper = new MermaidConfigBootstrapper(package);
            this.diagramGenerator = new MermaidDiagramGenerator(package);

            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            menuItem.BeforeQueryStatus += this.OnBeforeQueryStatus;

            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService))
                as OleMenuCommandService;

            if (commandService == null)
                throw new InvalidOperationException(
                    "Unable to acquire OleMenuCommandService.");

            var instance = new MermaidCommand(package, commandService);

            instance.dte = (await package.GetServiceAsync(typeof(DTE))) as DTE;

            if (instance.dte == null)
                throw new InvalidOperationException(
                    "Unable to acquire DTE service.");

            Instance = instance;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.package.JoinableTaskFactory.RunAsync(async delegate
            {
                await this.ExecuteAsync();
            }).FileAndForget("SlnxMermaidVsix/MermaidCommand");
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(this.package.DisposalToken);

            var pane = await this.outputService.GetOrCreateOutputPaneAsync();

            await this.outputService.LogAsync(pane,
                "Generate Mermaid Diagram command invoked.");

            try
            {
                var solutionPath = this.dte?.Solution?.FullName;

                if (string.IsNullOrWhiteSpace(solutionPath))
                {
                    await this.HandleMissingSolutionAsync(pane);
                    return;
                }

                var solutionDirectory = Path.GetDirectoryName(solutionPath);

                if (string.IsNullOrWhiteSpace(solutionDirectory))
                    throw new InvalidOperationException("Unable to determine the solution directory.");

                var configPath = Path.Combine(solutionDirectory, "slnx-mermaid.yml");

                await this.configBootstrapper.EnsureConfigFileExistsAsync(
                    configPath,
                    solutionPath,
                    pane,
                    this.package.DisposalToken);

                await this.outputService.LogAsync(
                    pane,
                    $"Selected solution: {solutionPath} {Environment.NewLine} Invoking SlnxMermaid.Core with argument: --config \"{configPath}\"");

                await Task.Run(async () =>
                {
                    await this.diagramGenerator.GenerateAsync(
                        configPath,
                        pane,
                        this.package.DisposalToken);
                }, this.package.DisposalToken);

                var successMessage = "Mermaid diagram generation completed successfully.";

                await this.outputService.LogAsync(pane, successMessage);
                await this.outputService.SendMessageToStatusBarAsync(successMessage);
            }
            catch (Exception ex)
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

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                command.Enabled = this.IsSolutionLoaded();
            }
        }

        private bool IsSolutionLoaded()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return this.dte?.Solution != null
                && this.dte.Solution.IsOpen
                && !string.IsNullOrWhiteSpace(this.dte.Solution.FullName);
        }
    }
}
