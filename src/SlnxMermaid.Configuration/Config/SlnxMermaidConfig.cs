namespace SlnxMermaid.Core.Config;

[ConfigurationDescription("Configuration for generating Mermaid dependency diagrams from .sln and .slnx files.")]
public sealed class SlnxMermaidConfig
{
    [ConfigurationDescription("Path to the .sln or .slnx file to analyze. Relative paths are resolved from the configuration file location.")]
    [ConfigurationRequired]
    [ConfigurationStringConstraint(MinLength = 1)]
    public string? Solution { get; set; }

    [ConfigurationDescription("Controls Mermaid graph generation behavior.")]
    public DiagramConfig? Diagram { get; set; } = new();

    [ConfigurationDescription("Filters that remove projects from the emitted dependency graph.")]
    public FilterConfig? Filters { get; set; } = new();

    [ConfigurationDescription("Controls display names and Mermaid node ids after project file names are normalized.")]
    public NamingConfig? Naming { get; set; } = new();

    [ConfigurationDescription("Controls where generated Mermaid markdown is written.")]
    [ConfigurationRequired]
    public OutputConfig? Output { get; set; } = new();

    [ConfigurationDescription("Controls color palette mode, semantic role colors, and per-project style overrides.")]
    public UiConfig? Ui { get; set; } = new();
}
