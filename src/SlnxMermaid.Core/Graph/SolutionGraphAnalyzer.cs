using Microsoft.Build.Graph;

namespace SlnxMermaid.Core.Graph;

public sealed class SolutionGraphAnalyzer
{
    public IReadOnlyCollection<ProjectNode> Analyze(string solutionPath)
    {
        var graph = new ProjectGraph(solutionPath);

        var nodes = graph.ProjectNodes
            .Select(n => new ProjectNode(
                ToId(n.ProjectInstance.FullPath),
                n.ProjectInstance.FullPath))
            .ToList();

        var byPath = nodes.ToDictionary(n => n.Path);

        foreach (var node in graph.ProjectNodes)
        {
            var from = byPath[node.ProjectInstance.FullPath];

            foreach (var dep in node.ProjectReferences)
            {
                if (byPath.TryGetValue(dep.ProjectInstance.FullPath, out var to))
                    from.Dependencies.Add(to);
            }
        }

        return nodes;
    }

    private static string ToId(string projectPath) =>
        Path.GetFileNameWithoutExtension(projectPath)
            .Replace('.', '_')
            .Replace('-', '_');
}
