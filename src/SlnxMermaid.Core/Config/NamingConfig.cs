namespace SlnxMermaid.Core.Config;

public sealed class NamingConfig
{
    public string? StripPrefix { get; init; }
    public Dictionary<string, string> Aliases { get; init; } = new();
}
