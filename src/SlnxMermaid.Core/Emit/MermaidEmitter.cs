using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Filtering;
using SlnxMermaid.Core.Graph;
using SlnxMermaid.Core.Naming;

namespace SlnxMermaid.Core.Emit
{
public sealed class MermaidEmitter
{
    private readonly NameTransformer _naming;
    private readonly ProjectFilter _filter;

    public MermaidEmitter(
        NameTransformer naming,
        ProjectFilter filter)
    {
        _naming = naming;
        _filter = filter;
    }

    public string Emit(
        IEnumerable<ProjectNode> nodes,
        DiagramConfig diagram)
    {
        if (diagram == null)
            throw new ArgumentNullException(nameof(diagram));

        var sb = new StringBuilder();
        sb.AppendLine($"graph {diagram.Direction}");

        if (diagram.OrderDependenciesByDepth)
        {
            var orderedEdges = OrderEdgesByDependencyDepth(nodes).ToList();

            for (var i = 0; i < orderedEdges.Count; i++)
            {
                if (i > 0 && !string.Equals(orderedEdges[i].RootId, orderedEdges[i - 1].RootId, StringComparison.Ordinal))
                    sb.AppendLine();

                sb.AppendLine($"    {orderedEdges[i].From} --> {orderedEdges[i].To}");
            }
        }
        else
        {
            foreach (var edge in GetLegacySortedEdges(nodes))
                sb.AppendLine($"    {edge.From} --> {edge.To}");
        }

        return sb.ToString();
    }

    private IEnumerable<LegacyEdge> GetLegacySortedEdges(IEnumerable<ProjectNode> nodes)
    {
        var edges = new HashSet<LegacyEdge>(LegacyEdgeComparer.Instance);

        foreach (var node in nodes)
        {
            if (!_filter.IsAllowed(node.Id))
                continue;

            var from = _naming.Transform(node.Id);

            foreach (var depId in node.Dependencies.Select(dep => dep.Id))
            {
                if (!_filter.IsAllowed(depId))
                    continue;

                var to = _naming.Transform(depId);
                edges.Add(new LegacyEdge(from, to));
            }
        }

        return edges
            .OrderBy(edge => edge.From, StringComparer.Ordinal)
            .ThenBy(edge => edge.To, StringComparer.Ordinal);
    }

    private IEnumerable<DepthOrderedEdge> OrderEdgesByDependencyDepth(IEnumerable<ProjectNode> nodes)
    {
        return DependencyDepthGraph.Create(nodes, _filter)
            .GetDepthOrderedEdges(_naming);
    }

}
}
