using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        SlnxMermaidConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (config.Diagram == null)
            throw new ArgumentNullException(nameof(config.Diagram));

        var nodeList = nodes == null ? new List<ProjectNode>() : nodes.ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"graph {config.Diagram.Direction}");

        if (config.Diagram.OrderDependenciesByDepth)
        {
            var orderedEdges = OrderEdgesByDependencyDepth(nodeList).ToList();

            if (orderedEdges.Count > 0)
                sb.AppendLine();

            for (var i = 0; i < orderedEdges.Count; i++)
            {
                if (i > 0 && !string.Equals(orderedEdges[i].SourceId, orderedEdges[i - 1].SourceId, StringComparison.Ordinal))
                    sb.AppendLine();

                sb.AppendLine($"    {orderedEdges[i].From} --> {orderedEdges[i].To}");
            }
        }
        else
        {
            foreach (var edge in GetLegacySortedEdges(nodeList))
                sb.AppendLine($"    {edge.From} --> {edge.To}");
        }

        if (config.Ui != null)
            AppendNodeStyles(sb, nodeList, config.Ui);

        return sb.ToString();
    }

    public string Emit(
        IEnumerable<ProjectNode> nodes,
        DiagramConfig diagram)
    {
        if (diagram == null)
            throw new ArgumentNullException(nameof(diagram));

        return Emit(nodes, new SlnxMermaidConfig
        {
            Diagram = diagram,
            Ui = null
        });
    }

    private void AppendNodeStyles(StringBuilder sb, IEnumerable<ProjectNode> nodes, UiConfig ui)
    {
        var visibleNodes = nodes
            .Where(node => _filter.IsAllowed(node.Id))
            .OrderBy(node => _naming.Transform(node.Id), StringComparer.Ordinal)
            .ToList();

        if (visibleNodes.Count == 0)
            return;

        var resolver = new MermaidNodeStyleResolver(ui);
        var classNamesByStyle = new Dictionary<MermaidNodeStyle, string>();
        var classDefinitions = new List<KeyValuePair<string, MermaidNodeStyle>>();
        var usedClassNames = new HashSet<string>(StringComparer.Ordinal);
        var assignments = new List<KeyValuePair<string, string>>();

        foreach (var node in visibleNodes)
        {
            var resolved = resolver.Resolve(node.Id);
            string className;
            if (!classNamesByStyle.TryGetValue(resolved.Style, out className))
            {
                className = CreateClassName(resolved);
                var originalClassName = className;
                var suffix = 2;
                while (usedClassNames.Contains(className))
                {
                    className = originalClassName + "_" + suffix;
                    suffix++;
                }

                usedClassNames.Add(className);
                classNamesByStyle[resolved.Style] = className;
                classDefinitions.Add(new KeyValuePair<string, MermaidNodeStyle>(className, resolved.Style));
            }

            assignments.Add(new KeyValuePair<string, string>(_naming.Transform(node.Id), className));
        }

        sb.AppendLine();

        foreach (var definition in classDefinitions)
        {
            var style = definition.Value;
            sb.AppendLine($"    classDef {definition.Key} fill:{style.Fill},stroke:{style.Stroke},color:{style.Color}");
        }

        sb.AppendLine();

        foreach (var assignment in assignments)
            sb.AppendLine($"    class {assignment.Key} {assignment.Value}");
    }

    private static string CreateClassName(MermaidNodeResolvedStyle resolved)
    {
        var baseName = string.IsNullOrWhiteSpace(resolved.BaseName) ? "style" : SanitizeClassToken(resolved.BaseName);
        if (!resolved.Custom)
            return "cls_" + baseName;

        return "cls_" + baseName + "_custom_" + resolved.Style.Fill.TrimStart('#').ToLowerInvariant();
    }

    private static string SanitizeClassToken(string value)
    {
        var sanitized = Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9_]+", "_").Trim('_');
        return string.IsNullOrEmpty(sanitized) ? "style" : sanitized;
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
