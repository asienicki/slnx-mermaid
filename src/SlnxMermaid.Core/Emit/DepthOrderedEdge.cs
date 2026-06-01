namespace SlnxMermaid.Core.Emit
{
internal sealed class DepthOrderedEdge
{
    internal DepthOrderedEdge(
        string rootId,
        string sourceId,
        string from,
        string to)
    {
        RootId = rootId;
        SourceId = sourceId;
        From = from;
        To = to;
    }

    internal string RootId { get; }
    internal string SourceId { get; }
    internal string From { get; }
    internal string To { get; }
}
}
