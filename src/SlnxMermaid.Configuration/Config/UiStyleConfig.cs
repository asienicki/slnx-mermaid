namespace SlnxMermaid.Core.Config;

[ConfigurationDescription("Explicit Mermaid node style fields. Omitted fields inherit from the semantic or fallback base style when possible.")]
public sealed class UiStyleConfig
{
    [ConfigurationDescription("Node fill color as #RRGGBB.")]
    [ConfigurationStringConstraint(Pattern = "^#[0-9a-fA-F]{6}$")]
    public string? Fill { get; set; }

    [ConfigurationDescription("Node border color as #RRGGBB.")]
    [ConfigurationStringConstraint(Pattern = "^#[0-9a-fA-F]{6}$")]
    public string? Stroke { get; set; }

    [ConfigurationDescription("Node text color as #RRGGBB.")]
    [ConfigurationStringConstraint(Pattern = "^#[0-9a-fA-F]{6}$")]
    public string? Color { get; set; }
}
