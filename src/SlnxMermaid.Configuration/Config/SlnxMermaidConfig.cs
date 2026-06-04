namespace SlnxMermaid.Core.Config;

public sealed class SlnxMermaidConfig
{
    public string? Solution { get; set; }

    public DiagramConfig? Diagram { get; set; } = new();

    public FilterConfig? Filters { get; set; } = new();

    public NamingConfig? Naming { get; set; } = new();

    public OutputConfig? Output { get; set; } = new();

    public UiConfig? Ui { get; set; } = new();
}
