namespace SlnxMermaid.Core.Config
{
public sealed class DiagramConfig
{
    public string Direction { get; set; } = "TD";

    public bool IncludeTransitiveDependencies { get; set; } = true;
}
}
