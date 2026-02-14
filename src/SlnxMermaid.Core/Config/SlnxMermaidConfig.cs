namespace SlnxMermaid.Core.Config
{
public sealed class SlnxMermaidConfig
{
    public string Solution { get; set; }

    public DiagramConfig Diagram { get; set; } = new DiagramConfig();

    public FilterConfig Filters { get; set; } = new FilterConfig();

    public NamingConfig Naming { get; set; } = new NamingConfig();

    public OutputConfig Output { get; set; } = new OutputConfig();
}
}
