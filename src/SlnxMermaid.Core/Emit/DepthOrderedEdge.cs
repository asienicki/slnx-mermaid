namespace SlnxMermaid.Core.Emit
{
internal sealed class DepthOrderedEdge
{
    internal DepthOrderedEdge(
        string rootId,
        string from,
        string to)
    {
        RootId = rootId;
        From = from;
        To = to;
    }

    internal string RootId { get; }
    internal string From { get; }
    internal string To { get; }
}
}
