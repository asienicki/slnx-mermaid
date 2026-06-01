using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        string direction)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"graph {direction}");

        var orderedEdges = OrderEdgesForReadability(nodes).ToList();

        for (var i = 0; i < orderedEdges.Count; i++)
        {
            if (i > 0 && orderedEdges[i].Layer != orderedEdges[i - 1].Layer)
                sb.AppendLine();

            sb.AppendLine($"    {orderedEdges[i].From} --> {orderedEdges[i].To}");
        }

        return sb.ToString();
    }

    private IEnumerable<OrderedEdge> OrderEdgesForReadability(IEnumerable<ProjectNode> nodes)
    {
        var allowedNodes = nodes
            .Where(node => _filter.IsAllowed(node.Id))
            .GroupBy(node => node.Id, StringComparer.Ordinal)
            .Select(group => group.OrderBy(node => node.Path, StringComparer.OrdinalIgnoreCase).First())
            .OrderBy(node => GetProjectLayer(node.Id))
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .ToList();

        var allowedIds = new HashSet<string>(allowedNodes.Select(node => node.Id), StringComparer.Ordinal);
        var dependenciesById = allowedNodes.ToDictionary(
            node => node.Id,
            node => node.Dependencies
                .Where(dependency => allowedIds.Contains(dependency.Id))
                .Select(dependency => dependency.Id)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(GetProjectLayer)
                .ThenBy(id => id, StringComparer.Ordinal)
                .ToList(),
            StringComparer.Ordinal);

        var layerById = GetTopologicalLayers(allowedNodes, dependenciesById);

        return dependenciesById
            .SelectMany(source => source.Value.Select(dependency => new OrderedEdge(
                _naming.Transform(source.Key),
                _naming.Transform(dependency),
                layerById[source.Key],
                GetProjectLayer(source.Key),
                source.Key,
                GetProjectLayer(dependency),
                dependency)))
            .Distinct(OrderedEdgeIdentityComparer.Instance)
            .OrderBy(edge => edge.Layer)
            .ThenBy(edge => edge.FromProjectLayer)
            .ThenBy(edge => edge.FromId, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToProjectLayer)
            .ThenBy(edge => edge.ToId, StringComparer.Ordinal);
    }

    private static Dictionary<string, int> GetTopologicalLayers(
        IReadOnlyCollection<ProjectNode> nodes,
        IReadOnlyDictionary<string, List<string>> dependenciesById)
    {
        var incomingCountById = nodes.ToDictionary(node => node.Id, node => 0, StringComparer.Ordinal);

        foreach (var dependencyId in dependenciesById.SelectMany(pair => pair.Value))
            incomingCountById[dependencyId]++;

        var layerById = nodes.ToDictionary(node => node.Id, node => 0, StringComparer.Ordinal);
        var queue = incomingCountById
            .Where(pair => pair.Value == 0)
            .Select(pair => pair.Key)
            .OrderBy(GetProjectLayer)
            .ThenBy(id => id, StringComparer.Ordinal)
            .ToList();

        while (queue.Count > 0)
        {
            var current = queue[0];
            queue.RemoveAt(0);

            foreach (var dependencyId in dependenciesById[current])
            {
                layerById[dependencyId] = Math.Max(layerById[dependencyId], layerById[current] + 1);
                incomingCountById[dependencyId]--;

                if (incomingCountById[dependencyId] == 0)
                {
                    queue.Add(dependencyId);
                    queue = queue
                        .OrderBy(GetProjectLayer)
                        .ThenBy(id => id, StringComparer.Ordinal)
                        .ToList();
                }
            }
        }

        var deepestResolvedLayer = layerById.Count == 0 ? 0 : layerById.Values.Max();

        var unresolvedLayer = deepestResolvedLayer + 1;

        foreach (var unresolvedId in incomingCountById
            .Where(pair => pair.Value > 0)
            .Select(pair => pair.Key)
            .OrderBy(GetProjectLayer)
            .ThenBy(id => id, StringComparer.Ordinal))
        {
            layerById[unresolvedId] = unresolvedLayer;
        }

        return layerById;
    }

    private static int GetProjectLayer(string projectId)
    {
        var normalized = projectId.Replace('_', '.').Replace('-', '.');
        var parts = normalized.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Any(IsPresentationPart))
            return 0;

        if (parts.Any(part => IsLayerPart(part, "Application") || IsLayerPart(part, "App")))
            return 1;

        if (parts.Any(part =>
            IsLayerPart(part, "Infrastructure") ||
            IsLayerPart(part, "Infra") ||
            IsLayerPart(part, "Persistence") ||
            IsLayerPart(part, "Repository") ||
            IsLayerPart(part, "Repositories")))
            return 2;

        if (parts.Any(part =>
            IsLayerPart(part, "DataAccess") ||
            IsLayerPart(part, "Data") ||
            IsLayerPart(part, "Database") ||
            IsLayerPart(part, "Db") ||
            IsLayerPart(part, "Storage")))
            return 3;

        if (parts.Any(part =>
            IsLayerPart(part, "Domain") ||
            IsLayerPart(part, "Core") ||
            IsLayerPart(part, "Shared") ||
            IsLayerPart(part, "Common")))
            return 4;

        return 5;
    }

    private static bool IsPresentationPart(string part)
    {
        return
            IsLayerPart(part, "Api") ||
            IsLayerPart(part, "MinimalApi") ||
            IsLayerPart(part, "Web") ||
            IsLayerPart(part, "Ui") ||
            IsLayerPart(part, "Mvc") ||
            IsLayerPart(part, "Worker") ||
            IsLayerPart(part, "Workers") ||
            IsLayerPart(part, "Console") ||
            IsLayerPart(part, "Cli") ||
            IsLayerPart(part, "Function") ||
            IsLayerPart(part, "Functions") ||
            IsLayerPart(part, "FunctionApp") ||
            IsLayerPart(part, "Service") ||
            IsLayerPart(part, "Host");
    }

    private static bool IsLayerPart(string part, string layer)
    {
        return string.Equals(part, layer, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class OrderedEdge
    {
        public OrderedEdge(
            string from,
            string to,
            int layer,
            int fromProjectLayer,
            string fromId,
            int toProjectLayer,
            string toId)
        {
            From = from;
            To = to;
            Layer = layer;
            FromProjectLayer = fromProjectLayer;
            FromId = fromId;
            ToProjectLayer = toProjectLayer;
            ToId = toId;
        }

        public string From { get; }
        public string To { get; }
        public int Layer { get; }
        public int FromProjectLayer { get; }
        public string FromId { get; }
        public int ToProjectLayer { get; }
        public string ToId { get; }
    }

    private sealed class OrderedEdgeIdentityComparer : IEqualityComparer<OrderedEdge>
    {
        public static readonly OrderedEdgeIdentityComparer Instance = new OrderedEdgeIdentityComparer();

        public bool Equals(OrderedEdge x, OrderedEdge y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;

            return string.Equals(x.From, y.From, StringComparison.Ordinal) &&
                string.Equals(x.To, y.To, StringComparison.Ordinal);
        }

        public int GetHashCode(OrderedEdge obj)
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
