namespace SlnxMermaid.Core.Config;

public sealed class SlnxMermaidConfig
{
    public required string Solution { get; set; }

    public DiagramConfig Diagram { get; init; } = new();

    public FilterConfig Filters { get; init; } = new();

    public NamingConfig Naming { get; init; } = new();

    public OutputConfig Output { get; set; } = new();
}
