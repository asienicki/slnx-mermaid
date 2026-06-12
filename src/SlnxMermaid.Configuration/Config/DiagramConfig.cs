namespace SlnxMermaid.Core.Config;

public sealed class DiagramConfig
{
    [ConfigurationDescription("Value emitted after Mermaid graph. Common values are TD, LR, BT, and RL; other Mermaid-supported values are passed through unchanged.")]
    public string? Direction { get; set; } = "TD";

    [ConfigurationDescription("When true, include both direct and indirect project dependencies. When false, include only direct project dependencies.")]
    public bool IncludeTransitiveDependencies { get; set; } = false;

    [ConfigurationDescription("When true, order emitted dependencies by dependency depth. When false, use the legacy alphabetical edge order.")]
    public bool OrderDependenciesByDepth { get; set; } = true;
}
