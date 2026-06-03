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
        if (naming == null)
            throw new ArgumentNullException(nameof(naming));

        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        _naming = naming;
        _filter = filter;
    }

    public string Emit(
        IEnumerable<ProjectNode> nodes,
        SlnxMermaidConfig config)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        return Emit(nodes, config.Diagram, config.Ui);
    }

    private string Emit(
        IEnumerable<ProjectNode> nodes,
        DiagramConfig diagram,
        UiConfig ui)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        if (diagram == null)
            throw new ArgumentNullException(nameof(diagram));

        var nodeList = nodes.ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"graph {diagram.Direction}");

        if (diagram.OrderDependenciesByDepth)
        {
            var orderedEdges = OrderEdgesByDependencyDepth(nodeList).ToList();

            if (orderedEdges.Count > 0)
                sb.AppendLine();

            for (var i = 0; i < orderedEdges.Count; i++)
            {
                if (i > 0 && !string.Equals(orderedEdges[i].SourceId, orderedEdges[i - 1].SourceId, StringComparison.Ordinal))
                    sb.AppendLine();

                AppendEdge(sb, orderedEdges[i]);
            }
        }
        else
        {
            foreach (var edge in GetLegacySortedEdges(nodeList))
                AppendEdge(sb, edge);
        }

        if (ui != null)
            AppendNodeStyles(sb, nodeList, ui);

        return sb.ToString();
    }

    private void AppendNodeStyles(StringBuilder sb, IEnumerable<ProjectNode> nodes, UiConfig ui)
    {
        if (sb == null)
            throw new ArgumentNullException(nameof(sb));

        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        if (ui == null)
            throw new ArgumentNullException(nameof(ui));

        var visibleNodes = nodes
            .Where(node => _filter.IsAllowed(node.Id))
            .OrderBy(node => _naming.Transform(node.Id), StringComparer.Ordinal);

        var resolver = new MermaidNodeStyleResolver(ui);
        // Keep style state scoped to a single Emit call so concurrent callers never share mutable collections.
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
                className = CreateUniqueClassName(resolved, usedClassNames);
                classNamesByStyle[resolved.Style] = className;
                classDefinitions.Add(new KeyValuePair<string, MermaidNodeStyle>(className, resolved.Style));
            }

            assignments.Add(new KeyValuePair<string, string>(_naming.Transform(node.Id), className));
        }

        if (assignments.Count == 0)
        {
            LogWarning("No visible nodes to process. Check your filters and inputs.");
            return;
        }

        sb.AppendLine();

        foreach (var definition in classDefinitions)
            AppendClass(sb, definition);

        sb.AppendLine();

        foreach (var assignment in assignments)
            AppendClassAssignment(sb, assignment);
    }

    private static string CreateUniqueClassName(
        MermaidNodeResolvedStyle resolved,
        HashSet<string> usedClassNames)
    {
        if (resolved == null)
            throw new ArgumentNullException(nameof(resolved));

        if (usedClassNames == null)
            throw new ArgumentNullException(nameof(usedClassNames));

        var className = CreateClassName(resolved);
        var originalClassName = className;
        var suffix = 2;
        while (usedClassNames.Contains(className))
        {
            className = originalClassName + "_" + suffix;
            suffix++;
        }

        usedClassNames.Add(className);
        return className;
    }

    private static string CreateClassName(MermaidNodeResolvedStyle resolved)
    {
        if (resolved == null)
            throw new ArgumentNullException(nameof(resolved));

        var baseName = string.IsNullOrWhiteSpace(resolved.BaseName) ? "style" : SanitizeClassToken(resolved.BaseName);
        if (!resolved.Custom)
            return "cls_" + baseName;

        return "cls_" + baseName + "_custom_" + resolved.Style.Fill.TrimStart('#').ToLowerInvariant();
    }

    private static string SanitizeClassToken(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        // Mermaid class names generated here are intentionally limited to ASCII lowercase letters,
        // digits, and underscores so the emitted classDef/class references remain parser-friendly.
        var sanitized = Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9_]+", "_").Trim('_');
        return string.IsNullOrEmpty(sanitized) ? "style" : sanitized;
    }

    private static void AppendEdge(StringBuilder sb, LegacyEdge edge)
    {
        if (sb == null)
            throw new ArgumentNullException(nameof(sb));

        if (edge == null)
            throw new ArgumentNullException(nameof(edge));

        sb.AppendLine($"    {edge.From} --> {edge.To}");
    }

    private static void AppendEdge(StringBuilder sb, DepthOrderedEdge edge)
    {
        if (sb == null)
            throw new ArgumentNullException(nameof(sb));

        if (edge == null)
            throw new ArgumentNullException(nameof(edge));

        sb.AppendLine($"    {edge.From} --> {edge.To}");
    }

    private static void AppendClass(StringBuilder sb, KeyValuePair<string, MermaidNodeStyle> definition)
    {
        if (sb == null)
            throw new ArgumentNullException(nameof(sb));

        var style = definition.Value;
        sb.AppendLine($"    classDef {definition.Key} fill:{style.Fill},stroke:{style.Stroke},color:{style.Color}");
    }

    private static void AppendClassAssignment(StringBuilder sb, KeyValuePair<string, string> assignment)
    {
        if (sb == null)
            throw new ArgumentNullException(nameof(sb));

        sb.AppendLine($"    class {assignment.Key} {assignment.Value}");
    }

    private static void LogWarning(string message)
    {
        Console.WriteLine("[WARNING]: " + message);
    }

    private IEnumerable<LegacyEdge> GetLegacySortedEdges(IEnumerable<ProjectNode> nodes)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

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
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        return DependencyDepthGraph.Create(nodes, _filter)
            .GetDepthOrderedEdges(_naming);
    }

}
}
