using System.Collections.Generic;

namespace SlnxMermaid.Core.Graph
{
public sealed class ProjectNode
{
    public string Id { get; }
    public string Path { get; }
    public HashSet<ProjectNode> Dependencies { get; } = new HashSet<ProjectNode>();

    public ProjectNode(string id, string path)
    {
        Id = id;
        Path = path;
    }
}
}
