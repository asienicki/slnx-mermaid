using EnvDTE;
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
using System.ComponentModel.Design;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SlnxMermaidVsix
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class MermaidCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("b3477400-4987-402f-9e32-eb89274610d6");

        private static readonly Guid OutputPaneGuidValue = new Guid("7DB87FC4-AE9B-4AAB-9E32-F893A4B23DBB");
        private const string OutputPaneTitle = "Slnx Mermaid";

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="MermaidCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private MermaidCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static MermaidCommand Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in MermaidCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new MermaidCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
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
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(this.package.DisposalToken);

            IVsOutputWindowPane pane = await this.GetOrCreateOutputPaneAsync();
            this.Log(pane, "Generate Mermaid Diagram command invoked.");

            try
            {
                DTE dte = await this.package.GetServiceAsync(typeof(DTE)) as DTE;
                var solutionPath = dte?.Solution?.FullName;

                if (string.IsNullOrWhiteSpace(solutionPath))
                {
                    throw new InvalidOperationException("No solution is currently loaded.");
                }

                var solutionDirectory = Path.GetDirectoryName(solutionPath);
                if (string.IsNullOrWhiteSpace(solutionDirectory))
                {
                    throw new InvalidOperationException("Unable to determine the solution directory.");
                }

                var configPath = Path.Combine(solutionDirectory, "slnx-mermaid.yml");

                this.Log(pane, $"Selected solution: {solutionPath}");
                this.Log(pane, $"Invoking SlnxMermaid.Core with argument: --config \"{configPath}\"");

                await Task.Run(async () =>
                {
                    await this.GenerateDiagramAsync(configPath, pane, this.package.DisposalToken);
                }, this.package.DisposalToken);

                this.Log(pane, "Mermaid diagram generation completed successfully.");
            }
            catch (Exception ex)
            {
                this.Log(pane, $"Generation failed: {ex}");

                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Mermaid diagram generation failed. See '{OutputPaneTitle}' in the Output window for details.\n\n{ex.Message}",
                    "Slnx Mermaid",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private async Task GenerateDiagramAsync(string configPath, IVsOutputWindowPane pane, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(configPath))
            {
                throw new ConfigurationFileNotFoundException(configPath);
            }

            this.Log(pane, $"Loading config: {configPath}");
            var config = YamlConfigLoader.Load(configPath)
                .Normalize(configPath)
                .Validate();

            this.Log(pane, $"Analyzing solution graph: {config.Solution}");
            var nodes = SolutionGraphAnalyzer.Analyze(config.Solution);
            this.Log(pane, $"Discovered {nodes.Count} projects.");

            var naming = new NameTransformer(config.Naming);
            var filter = new ProjectFilter(config.Filters.Exclude);
            var emitter = new MermaidEmitter(naming, filter);

            this.Log(pane, "Emitting Mermaid diagram...");
            var mermaid = emitter.Emit(nodes, config.Diagram.Direction);
            var markdownDiagram = mermaid.WrapCodeForMarkdown();

            if (string.IsNullOrWhiteSpace(config.Output?.File))
            {
                throw new DiagramOutputPathMissingException();
            }

            var outputFile = config.Output.File;
            var outputDirectory = Path.GetDirectoryName(outputFile);

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            await File.WriteAllTextAsync(outputFile, markdownDiagram, cancellationToken);
            this.Log(pane, $"Diagram written to: {outputFile}");
        }

        private async Task<IVsOutputWindowPane> GetOrCreateOutputPaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(this.package.DisposalToken);

            IVsOutputWindow outputWindow = await this.package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var paneGuid = OutputPaneGuidValue;
            outputWindow.CreatePane(ref paneGuid, OutputPaneTitle, 1, 1);
            outputWindow.GetPane(ref paneGuid, out IVsOutputWindowPane pane);
            pane.Activate();

            return pane;
        }

        private void Log(IVsOutputWindowPane pane, string message)
        {
            pane.OutputStringThreadSafe($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}
