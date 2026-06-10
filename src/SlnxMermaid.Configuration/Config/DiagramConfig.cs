namespace SlnxMermaid.Core.Config;

public sealed class DiagramConfig
{
    [ConfigurationDescription("Mermaid graph direction: top-down (TD), left-to-right (LR), bottom-to-top (BT), or right-to-left (RL).")]
    [ConfigurationAllowedValues("TD", "LR", "BT", "RL")]
    public string? Direction { get; set; } = "TD";

    [ConfigurationDescription("When true, include both direct and indirect project dependencies. When false, include only direct project dependencies.")]
    public bool IncludeTransitiveDependencies { get; set; } = false;

    [ConfigurationDescription("When true, order emitted dependencies by dependency depth. When false, use the legacy alphabetical edge order.")]
    public bool OrderDependenciesByDepth { get; set; } = true;
}
