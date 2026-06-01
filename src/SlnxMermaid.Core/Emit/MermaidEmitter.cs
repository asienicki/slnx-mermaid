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
            if (i > 0 && orderedEdges[i].Group != orderedEdges[i - 1].Group)
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
            .OrderBy(node => GetProjectRolePriority(node.Id))
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .ToList();

        var allowedIds = new HashSet<string>(allowedNodes.Select(node => node.Id), StringComparer.Ordinal);
        var dependenciesById = allowedNodes.ToDictionary(
            node => node.Id,
            node => node.Dependencies
                .Where(dependency => allowedIds.Contains(dependency.Id))
                .Select(dependency => dependency.Id)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(GetProjectRolePriority)
                .ThenBy(id => id, StringComparer.Ordinal)
                .ToList(),
            StringComparer.Ordinal);

        var layerById = GetTopologicalLayers(allowedNodes, dependenciesById);

        return dependenciesById
            .SelectMany(source => source.Value.Select(dependency => new OrderedEdge(
                _naming.Transform(source.Key),
                _naming.Transform(dependency),
                layerById[source.Key],
                GetProjectRolePriority(source.Key),
                source.Key,
                GetProjectRolePriority(dependency),
                dependency)))
            .Distinct(OrderedEdgeIdentityComparer.Instance)
            .OrderBy(edge => edge.FromProjectRolePriority)
            .ThenBy(edge => edge.Layer)
            .ThenBy(edge => edge.FromId, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToProjectRolePriority)
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
            .OrderBy(GetProjectRolePriority)
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
                        .OrderBy(GetProjectRolePriority)
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
            .OrderBy(GetProjectRolePriority)
            .ThenBy(id => id, StringComparer.Ordinal))
        {
            layerById[unresolvedId] = unresolvedLayer;
        }

        return layerById;
    }

    private static int GetProjectRolePriority(string projectId)
    {
        var normalized = projectId.Replace('_', '.').Replace('-', '.');
        var parts = normalized.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Any(IsMainEntryPointPart))
            return 0;

        if (parts.Any(IsWorkerEntryPointPart))
            return 1;

        if (parts.Any(part => IsRolePart(part, "Application") || IsRolePart(part, "App")))
            return 2;

        if (parts.Any(part =>
            IsRolePart(part, "Domain") ||
            IsRolePart(part, "Core") ||
            IsRolePart(part, "Shared") ||
            IsRolePart(part, "Common")))
            return 3;

        if (parts.Any(part =>
            IsRolePart(part, "Infrastructure") ||
            IsRolePart(part, "Infra") ||
            IsRolePart(part, "Repository") ||
            IsRolePart(part, "Repositories")))
            return 4;

        if (parts.Any(part =>
            IsRolePart(part, "DataAccess") ||
            IsRolePart(part, "Persistence") ||
            IsRolePart(part, "Data") ||
            IsRolePart(part, "Database") ||
            IsRolePart(part, "Db") ||
            IsRolePart(part, "Storage")))
            return 5;

        if (parts.Any(IsSecondaryEntryPointPart))
            return 6;

        return 7;
    }

    private static bool IsMainEntryPointPart(string part)
    {
        return
            IsRolePart(part, "Api") ||
            IsRolePart(part, "MinimalApi") ||
            IsRolePart(part, "Web") ||
            IsRolePart(part, "Ui") ||
            IsRolePart(part, "Mvc") ||
            IsRolePart(part, "Host") ||
            IsRolePart(part, "Service") ||
            IsRolePart(part, "Function") ||
            IsRolePart(part, "Functions") ||
            IsRolePart(part, "FunctionApp");
    }

    private static bool IsWorkerEntryPointPart(string part)
    {
        return
            IsRolePart(part, "Worker") ||
            IsRolePart(part, "Workers") ||
            IsRolePart(part, "Job") ||
            IsRolePart(part, "Jobs");
    }

    private static bool IsSecondaryEntryPointPart(string part)
    {
        return
            IsRolePart(part, "Seeder") ||
            IsRolePart(part, "Seeders") ||
            IsRolePart(part, "Migrator") ||
            IsRolePart(part, "Migrators") ||
            IsRolePart(part, "Migration") ||
            IsRolePart(part, "Migrations") ||
            IsRolePart(part, "Tool") ||
            IsRolePart(part, "Tools") ||
            IsRolePart(part, "Console") ||
            IsRolePart(part, "Cli");
    }

    private static bool IsRolePart(string part, string role)
    {
        return string.Equals(part, role, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class OrderedEdge
    {
        public OrderedEdge(
            string from,
            string to,
            int layer,
            int fromProjectRolePriority,
            string fromId,
            int toProjectRolePriority,
            string toId)
        {
            From = from;
            To = to;
            Layer = layer;
            FromProjectRolePriority = fromProjectRolePriority;
            FromId = fromId;
            ToProjectRolePriority = toProjectRolePriority;
            ToId = toId;
        }

        public string From { get; }
        public string To { get; }
        public int Layer { get; }
        public int Group { get { return FromProjectRolePriority; } }
        public int FromProjectRolePriority { get; }
        public string FromId { get; }
        public int ToProjectRolePriority { get; }
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
