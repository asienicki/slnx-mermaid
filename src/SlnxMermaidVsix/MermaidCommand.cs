using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
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
            new Guid(Strings.CommandSetGuid);

        private readonly AsyncPackage package;
        private readonly MermaidOutputService outputService;
        private readonly MermaidConfigBootstrapper configBootstrapper;
        private readonly MermaidDiagramGenerator diagramGenerator;

        private DTE dte;

        public static MermaidCommand Instance { get; private set; }

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
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            menuItem.BeforeQueryStatus += this.OnBeforeQueryStatus;

            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Creates and registers the command singleton and resolves required VS services.
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

            Instance = instance;
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
            }).FileAndForget(Strings.FileAndForgetOperationName);
        }

        /// <summary>
        /// Prepares dependencies and delegates the generation flow to the workflow.
        /// </summary>
        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(this.package.DisposalToken);

            var pane = await this.outputService.GetOrCreateOutputPaneAsync();

            var workflow = new MermaidGenerationWorkflow(
                this.package,
                this.dte,
                this.outputService,
                this.configBootstrapper,
                this.diagramGenerator);

            await workflow.RunAsync(pane, this.package.DisposalToken);
        }

        /// <summary>
        /// Updates command availability before Visual Studio renders the menu item.
        /// </summary>
        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                command.Enabled = this.IsSolutionLoaded();
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
