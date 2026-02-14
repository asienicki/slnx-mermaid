using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
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

            var workflow = new MermaidGenerationWorkflow(
                this.package,
                this.dte,
                this.outputService,
                this.configBootstrapper,
                this.diagramGenerator);

            await workflow.RunAsync(pane, this.package.DisposalToken);
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
