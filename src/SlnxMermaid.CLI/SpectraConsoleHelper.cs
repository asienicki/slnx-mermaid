using Spectre.Console;

namespace SlnxMermaid.CLI
{
    public static class SpectraConsoleHelper
    {
        public static void PrintHeader()
        {
            AnsiConsole.Clear();

            var figlet = new FigletText("slnx-mermaid")
            {
                Color = Color.Green,
                Justification = Justify.Center
            };

            var link = new Markup(
                "[grey]https://github.com/asienicki/slnx-mermaid[/]\n" +
                "[dim]Generate Mermaid architecture diagrams from .NET solution files (.sln / .slnx).\r\nCLI tool for visualizing project dependencies, layers, and architectural rules. Built for CI/CD and docs automation — architecture always in sync with code.[/]"
            )
            {
                Justification = Justify.Center
            };

            var content = new Rows(
                figlet,
                new Rule { Style = Style.Parse("grey") },
                link
            );

            var panel = new Panel(content)
            {
                Border = BoxBorder.Double,
                BorderStyle = new Style(Color.Red),
                Padding = new Padding(1, 1, 1, 1)
            };

            AnsiConsole.Write(panel);
        }
    }
}
