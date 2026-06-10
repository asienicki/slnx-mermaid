using System.Collections.Generic;

namespace SlnxMermaid.Core.Config;

public sealed class UiConfig
{
    [ConfigurationDisplayName("UI mode")]
    [ConfigurationDescription("Default Mermaid color palette mode used when resolving semantic and fallback styles.")]
    [ConfigurationAllowedValues("dark", "light")]
    public string? Mode { get; set; } = "dark";

    [ConfigurationDescription("Overrides palette colors assigned to semantic project roles detected from normalized project ids.")]
    [ConfigurationDictionaryValue(ConfigurationDictionaryValueKind.PaletteColor)]
    public Dictionary<string, string>? Semantic { get; set; } = new();

    [ConfigurationDescription("Per-project style overrides keyed by normalized project id. Keys may contain * wildcards and are case-sensitive.")]
    [ConfigurationDictionaryValue(ConfigurationDictionaryValueKind.ProjectStyle)]
    public Dictionary<string, object>? Mappings { get; set; } = new();
}
