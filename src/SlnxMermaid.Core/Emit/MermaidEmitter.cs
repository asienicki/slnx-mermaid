using System.Text;
using SlnxMermaid.Core.Graph;
using SlnxMermaid.Core.Naming;
using SlnxMermaid.Core.Filtering;

namespace SlnxMermaid.Core.Emit;

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

        var edges = new HashSet<(string From, string To)>();

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
                edges.Add((from, to));
            }
        }

        foreach (var (from, to) in edges.OrderBy(e => e.From).ThenBy(e => e.To))
            sb.AppendLine($"    {from} --> {to}");

        return sb.ToString();
    }
}
