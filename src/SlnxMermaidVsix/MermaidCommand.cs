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
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
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
            if (commandService == null)
            {
                throw new InvalidOperationException("Unable to acquire OleMenuCommandService.");
            }

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
            await this.LogAsync(pane, "Generate Mermaid Diagram command invoked.");

            try
            {
                DTE dte = await this.package.GetServiceAsync(typeof(DTE)) as DTE;
                if (dte == null)
                {
                    throw new InvalidOperationException("Unable to acquire DTE service.");
                }

                var solutionPath = dte.Solution?.FullName;

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
                await this.EnsureConfigFileExistsAsync(configPath, solutionPath, pane, this.package.DisposalToken);

                await this.LogAsync(pane, $"Selected solution: {solutionPath}");
                await this.LogAsync(pane, $"Invoking SlnxMermaid.Core with argument: --config \"{configPath}\"");

                await Task.Run(async () =>
                {
                    await this.GenerateDiagramAsync(configPath, pane, this.package.DisposalToken);
                }, this.package.DisposalToken);

                await this.LogAsync(pane, "Mermaid diagram generation completed successfully.");
            }
            catch (Exception ex)
            {
                await this.LogAsync(pane, $"Generation failed: {ex}");

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

            await this.LogAsync(pane, $"Loading config: {configPath}");
            var config = YamlConfigLoader.Load(configPath)
                .Normalize(configPath)
                .Validate();

            await this.LogAsync(pane, $"Analyzing solution graph: {config.Solution}");
            var nodes = SolutionGraphAnalyzer.Analyze(config.Solution);
            await this.LogAsync(pane, $"Discovered {nodes.Count} projects.");

            var naming = new NameTransformer(config.Naming);
            var filter = new ProjectFilter(config.Filters.Exclude);
            var emitter = new MermaidEmitter(naming, filter);

            await this.LogAsync(pane, "Emitting Mermaid diagram...");
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

            cancellationToken.ThrowIfCancellationRequested();
            File.WriteAllText(outputFile, markdownDiagram);
            await this.LogAsync(pane, $"Diagram written to: {outputFile}");
        }

        private async Task EnsureConfigFileExistsAsync(
            string configPath,
            string solutionPath,
            IVsOutputWindowPane pane,
            CancellationToken cancellationToken)
        {
            if (File.Exists(configPath))
            {
                return;
            }

            var defaultConfig = new SlnxMermaidConfig
            {
                Solution = Path.GetFileName(solutionPath),
                Output = new OutputConfig
                {
                    File = "dependency-graph-mermaid.md"
                },
                Naming = new NamingConfig
                {
                    StripPrefix = $"{Path.GetFileNameWithoutExtension(solutionPath)}_",
                    Aliases = new Dictionary<string, string>()
                },
                Filters = new FilterConfig
                {
                    Exclude = new string[]
                    {
                        "Test",
                        "Tests",
                        "Testing",
                        "Mock",
                        "Mocks",
                        "Stub",
                        "Stubs",
                        "Fake",
                        "Fakes",
                        "Enums",
                        "AppHost",
                        "WebHost",
                        "ServiceDefaults",
                        "Dto"
                    }.ToList()
                },
            };

            var yaml = new StringBuilder()
                .AppendLine("# Auto-generated by Slnx Mermaid Visual Studio extension")
                .AppendLine(defaultConfig.ToYaml())
                .ToString();

            cancellationToken.ThrowIfCancellationRequested();

            using (var stream = new FileStream(
                    configPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    useAsync: true))
            {
                using (var writer = new StreamWriter(stream))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.WriteAsync(yaml);
                }
            }

            await LogAsync(pane, $"Configuration file was missing and has been created: {configPath}");
        }

        private async Task<IVsOutputWindowPane> GetOrCreateOutputPaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(this.package.DisposalToken);

            IVsOutputWindow outputWindow = await this.package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null)
            {
                throw new InvalidOperationException("Unable to acquire SVsOutputWindow service.");
            }

            var paneGuid = OutputPaneGuidValue;
            outputWindow.CreatePane(ref paneGuid, OutputPaneTitle, 1, 1);
            outputWindow.GetPane(ref paneGuid, out IVsOutputWindowPane pane);
            if (pane == null)
            {
                throw new InvalidOperationException("Unable to acquire the output pane.");
            }

            pane.Activate();

            return pane;
        }

        private async Task LogAsync(IVsOutputWindowPane pane, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(this.package.DisposalToken);
            pane.OutputString($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}
