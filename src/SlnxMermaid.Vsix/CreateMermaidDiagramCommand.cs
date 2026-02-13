using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Emit;
using SlnxMermaid.Core.Extensions;
using SlnxMermaid.Core.Filtering;
using SlnxMermaid.Core.Graph;
using SlnxMermaid.Core.Naming;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SlnxMermaid.Vsix
{
    internal sealed class CreateMermaidDiagramCommand
    {
        private readonly AsyncPackage package;

        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("7d71780f-b20f-4672-9732-aab300428f2d");

        private CreateMermaidDiagramCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package;

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandId);
            menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
            {
                return;
            }

            _ = new CreateMermaidDiagramCommand(package, commandService);
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            var targetPath = GetSelectedPath();
            var extension = Path.GetExtension(targetPath);
            var isSolution = extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);

            command.Visible = isSolution;
            command.Enabled = isSolution;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ExecuteAsync();
            });
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var selectedPath = GetSelectedPath();
            var extension = Path.GetExtension(selectedPath);

            if (!extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                await ShowMessageAsync("Wybierz plik .sln lub .slnx, aby wygenerować diagram Mermaid.", OLEMSGICON.OLEMSGICON_WARNING);
                return;
            }

            try
            {
                var solutionDirectory = Path.GetDirectoryName(selectedPath) ?? string.Empty;
                var configPath = Path.Combine(solutionDirectory, "slnx-mermaid.yml");

                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath, BuildDefaultConfig(selectedPath), Encoding.UTF8);
                }

                var outputPath = await Task.Run(delegate
                {
                    var config = YamlConfigLoader.Load(configPath)
                        .Normalize(configPath)
                        .Validate();

                    var nodes = SolutionGraphAnalyzer.Analyze(config.Solution);
                    var naming = new NameTransformer(config.Naming);
                    var filter = new ProjectFilter(config.Filters.Exclude);
                    var emitter = new MermaidEmitter(naming, filter);

                    var mermaid = emitter.Emit(nodes, config.Diagram.Direction).WrapCodeForMarkdown();

                    var targetOutput = config.Output.File;
                    if (string.IsNullOrWhiteSpace(targetOutput))
                        throw new InvalidOperationException("Brak output.file w konfiguracji.");

                    var outputDirectory = Path.GetDirectoryName(targetOutput);
                    if (!string.IsNullOrWhiteSpace(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    File.WriteAllText(targetOutput, mermaid, Encoding.UTF8);
                    return targetOutput;
                });

                await ShowMessageAsync("Diagram Mermaid zapisano do:\n" + outputPath, OLEMSGICON.OLEMSGICON_INFO);
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Nie udało się wygenerować diagramu.\n\n" + ex.Message, OLEMSGICON.OLEMSGICON_CRITICAL);
            }
        }

        private static string BuildDefaultConfig(string solutionPath)
        {
            var solutionFileName = Path.GetFileName(solutionPath);
            var inferredPrefix = Path.GetFileNameWithoutExtension(solutionFileName);

            return string.Format(CultureInfo.InvariantCulture,
@"solution: {0}

diagram:
  direction: TD

filters:
  exclude:
    - Tests
    - Test
    - Benchmarks
    - Samples

naming:
  stripPrefix: {1}.
  aliases: {{ }}

output:
  file: docs/dependencies.md
", solutionFileName, inferredPrefix);
        }

        private string GetSelectedPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var serviceProvider = (IServiceProvider)package;
            var dte = serviceProvider.GetService(typeof(SDTE)) as DTE;
            if (dte == null)
            {
                return string.Empty;
            }

            if (dte.SelectedItems == null || dte.SelectedItems.Count == 0)
            {
                return dte.Solution != null ? dte.Solution.FullName ?? string.Empty : string.Empty;
            }

            var selectedItem = dte.SelectedItems.Item(1);

            if (selectedItem.ProjectItem != null)
            {
                try
                {
                    return selectedItem.ProjectItem.FileNames[1];
                }
                catch
                {
                    return string.Empty;
                }
            }

            if (selectedItem.Project != null)
            {
                return selectedItem.Project.FullName ?? string.Empty;
            }

            return string.Empty;
        }

        private async Task ShowMessageAsync(string message, OLEMSGICON icon)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var shell = await package.GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
            if (shell == null)
            {
                return;
            }

            int result;
            shell.ShowMessageBox(
                0,
                Guid.Empty,
                "slnx-mermaid",
                message,
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                icon,
                0,
                out result);
        }
    }
}
