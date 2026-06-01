using System;
using System.Collections.Generic;
using System.Linq;
using SlnxMermaid.Core.Filtering;
using SlnxMermaid.Core.Graph;
using SlnxMermaid.Core.Naming;

namespace SlnxMermaid.Core.Emit
{
internal sealed class DependencyDepthGraph
{
    private readonly IReadOnlyDictionary<string, List<string>> _dependenciesById;
    private readonly IReadOnlyCollection<string> _nodeIds;
    private readonly Dictionary<string, int> _depthById = new Dictionary<string, int>(StringComparer.Ordinal);

    internal DependencyDepthGraph(
        IReadOnlyCollection<string> nodeIds,
        IReadOnlyDictionary<string, List<string>> dependenciesById)
    {
        _nodeIds = nodeIds;
        _dependenciesById = dependenciesById;
    }

    internal static DependencyDepthGraph Create(
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

    internal IEnumerable<DepthOrderedEdge> GetDepthOrderedEdges(NameTransformer naming)
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
}
