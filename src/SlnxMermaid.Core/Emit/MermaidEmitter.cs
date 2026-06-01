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

    private sealed class DependencyDepthGraph
    {
        private readonly IReadOnlyDictionary<string, List<string>> _dependenciesById;
        private readonly IReadOnlyCollection<string> _nodeIds;
        private readonly Dictionary<string, int> _depthById = new Dictionary<string, int>(StringComparer.Ordinal);

        private DependencyDepthGraph(
            IReadOnlyCollection<string> nodeIds,
            IReadOnlyDictionary<string, List<string>> dependenciesById)
        {
            _nodeIds = nodeIds;
            _dependenciesById = dependenciesById;
        }

        public static DependencyDepthGraph Create(
            IEnumerable<ProjectNode> nodes,
            ProjectFilter filter)
        {
            var allowedNodes = nodes
                .Where(node => filter.IsAllowed(node.Id))
                .GroupBy(node => node.Id, StringComparer.Ordinal)
                .Select(group => group.OrderBy(node => node.Path, StringComparer.OrdinalIgnoreCase).First())
                .OrderBy(node => node.Id, StringComparer.Ordinal)
                .ToList();

            var allowedIds = new HashSet<string>(allowedNodes.Select(node => node.Id), StringComparer.Ordinal);
            var dependenciesById = allowedNodes.ToDictionary(
                node => node.Id,
                node => node.Dependencies
                    .Where(dependency => allowedIds.Contains(dependency.Id))
                    .Select(dependency => dependency.Id)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToList(),
                StringComparer.Ordinal);

            return new DependencyDepthGraph(allowedIds.ToList(), dependenciesById);
        }

        public IEnumerable<DepthOrderedEdge> GetDepthOrderedEdges(NameTransformer naming)
        {
            var emittedEdges = new HashSet<LegacyEdge>(LegacyEdgeComparer.Instance);

            foreach (var rootId in GetTraversalRoots())
            {
                foreach (var edge in Traverse(rootId, rootId, naming, emittedEdges, new HashSet<string>(StringComparer.Ordinal)))
                    yield return edge;
            }
        }

        private IEnumerable<string> GetTraversalRoots()
        {
            var incomingCountById = _nodeIds.ToDictionary(id => id, id => 0, StringComparer.Ordinal);

            foreach (var dependencyId in _dependenciesById.SelectMany(pair => pair.Value))
                incomingCountById[dependencyId]++;

            var rootIds = incomingCountById
                .Where(pair => pair.Value == 0)
                .Select(pair => pair.Key)
                .OrderByDescending(GetDependencyDepth)
                .ThenBy(id => id, StringComparer.Ordinal)
                .ToList();

            var nonRootIds = _nodeIds
                .Except(rootIds, StringComparer.Ordinal)
                .OrderByDescending(GetDependencyDepth)
                .ThenBy(id => id, StringComparer.Ordinal);

            return rootIds.Concat(nonRootIds);
        }

        private IEnumerable<DepthOrderedEdge> Traverse(
            string sourceId,
            string rootId,
            NameTransformer naming,
            ISet<LegacyEdge> emittedEdges,
            ISet<string> activePath)
        {
            if (!activePath.Add(sourceId))
                yield break;

            foreach (var dependencyId in _dependenciesById[sourceId]
                .OrderByDescending(GetDependencyDepth)
                .ThenBy(id => id, StringComparer.Ordinal))
            {
                var edgeKey = new LegacyEdge(sourceId, dependencyId);

                if (emittedEdges.Add(edgeKey))
                {
                    yield return new DepthOrderedEdge(
                        rootId,
                        naming.Transform(sourceId),
                        naming.Transform(dependencyId));
                }

                foreach (var childEdge in Traverse(dependencyId, rootId, naming, emittedEdges, activePath))
                    yield return childEdge;
            }

            activePath.Remove(sourceId);
        }

        private int GetDependencyDepth(string nodeId)
        {
            int depth;

            if (_depthById.TryGetValue(nodeId, out depth))
                return depth;

            depth = GetDependencyDepth(nodeId, new HashSet<string>(StringComparer.Ordinal));
            _depthById[nodeId] = depth;

            return depth;
        }

        private int GetDependencyDepth(string nodeId, ISet<string> activePath)
        {
            int depth;

            if (_depthById.TryGetValue(nodeId, out depth))
                return depth;

            if (!activePath.Add(nodeId))
                return 0;

            depth = 0;

            foreach (var dependencyId in _dependenciesById[nodeId])
                depth = Math.Max(depth, 1 + GetDependencyDepth(dependencyId, activePath));

            activePath.Remove(nodeId);
            _depthById[nodeId] = depth;

            return depth;
        }
    }

    private sealed class LegacyEdge
    {
        public LegacyEdge(string from, string to)
        {
            From = from;
            To = to;
        }

        public string From { get; }
        public string To { get; }
    }

    private sealed class DepthOrderedEdge
    {
        public DepthOrderedEdge(
            string rootId,
            string from,
            string to)
        {
            RootId = rootId;
            From = from;
            To = to;
        }

        public string RootId { get; }
        public string From { get; }
        public string To { get; }
    }

    private sealed class LegacyEdgeComparer : IEqualityComparer<LegacyEdge>
    {
        public static readonly LegacyEdgeComparer Instance = new LegacyEdgeComparer();

        public bool Equals(LegacyEdge x, LegacyEdge y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;

            return string.Equals(x.From, y.From, StringComparison.Ordinal) &&
                string.Equals(x.To, y.To, StringComparison.Ordinal);
        }

        public int GetHashCode(LegacyEdge obj)
        {
            unchecked
            {
                return ((obj.From != null ? StringComparer.Ordinal.GetHashCode(obj.From) : 0) * 397) ^
                    (obj.To != null ? StringComparer.Ordinal.GetHashCode(obj.To) : 0);
            }
        }
    }
}
}
