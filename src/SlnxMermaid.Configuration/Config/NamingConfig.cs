using System.Collections.Generic;

namespace SlnxMermaid.Core.Config;

public sealed class NamingConfig
{
    [ConfigurationDescription("Prefix removed from normalized project ids before aliases are applied and labels are emitted.")]
    public string? StripPrefix { get; set; }

    [ConfigurationDescription("Maps transformed project names to explicit Mermaid node ids. Aliases are applied after normalization and stripPrefix.")]
    [ConfigurationDictionaryValue(ConfigurationDictionaryValueKind.String)]
    public Dictionary<string, string> Aliases { get; set; } = new();
}
