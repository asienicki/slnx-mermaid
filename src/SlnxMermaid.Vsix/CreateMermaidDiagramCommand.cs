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
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SlnxMermaid.Vsix;

internal sealed class CreateMermaidDiagramCommand
{
    private readonly AsyncPackage package;

    public const int CommandId = 0x0100;
    public static readonly Guid CommandSet = new("7d71780f-b20f-4672-9732-aab300428f2d");

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
        if (commandService is null)
        {
            return;
        }

        _ = new CreateMermaidDiagramCommand(package, commandService);
    }

    private void OnBeforeQueryStatus(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (sender is not OleMenuCommand command)
        {
            return;
        }

        var targetPath = GetSelectedPath();
        var extension = Path.GetExtension(targetPath);

        command.Visible = extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);

        command.Enabled = command.Visible;
    }

    private async void Execute(object sender, EventArgs e)
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
                await File.WriteAllTextAsync(configPath, BuildDefaultConfig(selectedPath));
            }

            var config = YamlConfigLoader.Load(configPath)
                .Normalize(configPath)
                .Validate();

            var nodes = SolutionGraphAnalyzer.Analyze(config.Solution);
            var naming = new NameTransformer(config.Naming);
            var filter = new ProjectFilter(config.Filters.Exclude);
            var emitter = new MermaidEmitter(naming, filter);

            var mermaid = emitter.Emit(nodes, config.Diagram.Direction).WrapCodeForMarkdown();

            var outputPath = config.Output.File!;
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            await File.WriteAllTextAsync(outputPath, mermaid);

            await ShowMessageAsync($"Diagram Mermaid zapisano do:\n{outputPath}", OLEMSGICON.OLEMSGICON_INFO);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"Nie udało się wygenerować diagramu.\n\n{ex.Message}", OLEMSGICON.OLEMSGICON_CRITICAL);
        }
    }

    private static string BuildDefaultConfig(string solutionPath)
    {
        var solutionFileName = Path.GetFileName(solutionPath);
        var inferredPrefix = Path.GetFileNameWithoutExtension(solutionFileName);

        return string.Format(CultureInfo.InvariantCulture,
            """
            solution: {0}

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
            """,
            solutionFileName,
            inferredPrefix);
    }

    private string GetSelectedPath()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var dte = package.GetService(typeof(SDTE)) as DTE;
        if (dte?.SelectedItems is null || dte.SelectedItems.Count == 0)
        {
            return string.Empty;
        }

        var selectedItem = dte.SelectedItems.Item(1);

        if (selectedItem.ProjectItem is not null)
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

        if (selectedItem.Project is not null)
        {
            return selectedItem.Project.FullName ?? string.Empty;
        }

        return string.Empty;
    }

    private async Task ShowMessageAsync(string message, OLEMSGICON icon)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var shell = await package.GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
        if (shell is null)
        {
            return;
        }

        _ = shell.ShowMessageBox(
            dwCompRole: 0,
            rclsidComp: Guid.Empty,
            pszTitle: "slnx-mermaid",
            pszText: message,
            pszHelpFile: string.Empty,
            dwHelpContextID: 0,
            msgbtn: OLEMSGBUTTON.OLEMSGBUTTON_OK,
            msgdefbtn: OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
            msgicon: icon,
            fSysAlert: 0,
            pnResult: out _);
    }
}
