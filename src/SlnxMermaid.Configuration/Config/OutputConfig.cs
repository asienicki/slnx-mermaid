namespace SlnxMermaid.Core.Config;

public sealed class OutputConfig
{
    [ConfigurationDescription("Target Markdown file path for the generated fenced Mermaid diagram. Relative paths are resolved from the configuration file location and may include supported path placeholders.")]
    [ConfigurationRequired]
    [ConfigurationStringConstraint(MinLength = 1)]
    public string? File { get; set; }
}
