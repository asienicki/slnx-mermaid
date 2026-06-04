using System.Collections.Generic;

namespace SlnxMermaid.Core.Config;

public sealed class UiConfig
{
    [ConfigurationDisplayName("UI mode")]
    [ConfigurationDescription("Controls default color palette mode. Supported values are dark and light.")]
    public string? Mode { get; set; } = "dark";

    public Dictionary<string, string>? Semantic { get; set; } = new();

    public Dictionary<string, object>? Mappings { get; set; } = new();
}
