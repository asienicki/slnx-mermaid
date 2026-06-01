namespace SlnxMermaid.Core.Config
{
public sealed class DiagramConfig
{
    public string Direction { get; set; } = "TD";

    public bool IncludeTransitiveDependencies { get; set; } = false;

    public bool OrderDependenciesByRole { get; set; } = false;
}
}
