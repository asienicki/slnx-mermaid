using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using SlnxMermaidVsix.Resources;

namespace SlnxMermaidVsix
{
    /// <summary>
    /// Registers and handles the VSIX command that starts Mermaid diagram generation.
    /// This class acts as a thin command handler and delegates execution to the workflow.
    /// </summary>
    internal sealed class MermaidCommand
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet =
            new Guid(GlobalConstants.CommandSetGuid);

        private readonly AsyncPackage package;
        private readonly MermaidOutputService outputService;
        private readonly MermaidConfigBootstrapper configBootstrapper;
        private readonly MermaidDiagramGenerator diagramGenerator;
        private readonly SemaphoreSlim executionGate = new SemaphoreSlim(1, 1);
        private readonly OleMenuCommand menuItem;

        private const string CommandAlreadyRunningMessage =
            "Diagram generation is already running. Wait for completion before starting again.";

        private DTE dte;

        /// <summary>
        /// Initializes the command instance and wires it into the Visual Studio menu.
        /// </summary>
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
            menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            menuItem.BeforeQueryStatus += this.OnBeforeQueryStatus;

            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Creates and registers the command and resolves required VS services.
        /// </summary>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService))
                as OleMenuCommandService;

            if (commandService == null)
                throw new InvalidOperationException(
                    Strings.ErrorAcquireMenuCommandService);

            var instance = new MermaidCommand(package, commandService);

            instance.dte = (await package.GetServiceAsync(typeof(DTE))) as DTE;

            if (instance.dte == null)
                throw new InvalidOperationException(
                    Strings.ErrorAcquireDteService);

        }

        /// <summary>
        /// UI entry point for the command click; starts async execution without blocking the UI thread.
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.package.JoinableTaskFactory.RunAsync(async delegate
            {
                await this.ExecuteAsync();
            }).FileAndForget(GlobalConstants.FileAndForgetOperationName);
        }

        /// <summary>
        /// Prepares dependencies and delegates the generation flow to the workflow.
        /// </summary>
        private async Task ExecuteAsync()
        {
            var pane = await this.outputService.GetOrCreateOutputPaneAsync();

            if (!await this.executionGate.WaitAsync(0, this.package.DisposalToken))
            {
                await this.outputService.LogAsync(pane, CommandAlreadyRunningMessage);
                await this.outputService.SendMessageToStatusBarAsync(CommandAlreadyRunningMessage);
                return;
            }

            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(this.package.DisposalToken);

            this.menuItem.Enabled = false;

            var workflow = new MermaidGenerationWorkflow(
                this.package,
                this.dte,
                this.outputService,
                this.configBootstrapper,
                this.diagramGenerator);

            try
            {
                await workflow.RunAsync(pane, this.package.DisposalToken);
            }
            finally
            {
                this.executionGate.Release();

                await ThreadHelper.JoinableTaskFactory
                    .SwitchToMainThreadAsync(CancellationToken.None);

                this.menuItem.Enabled = this.IsSolutionLoaded();
            }
        }

        /// <summary>
        /// Updates command availability before Visual Studio renders the menu item.
        /// </summary>
        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                command.Enabled = this.executionGate.CurrentCount > 0
                    && this.IsSolutionLoaded();
            }
        }

        /// <summary>
        /// Determines whether a solution is currently loaded and open in Visual Studio.
        /// </summary>
        private bool IsSolutionLoaded()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return this.dte?.Solution != null
                && this.dte.Solution.IsOpen
                && !string.IsNullOrWhiteSpace(this.dte.Solution.FullName);
        }
    }
}
