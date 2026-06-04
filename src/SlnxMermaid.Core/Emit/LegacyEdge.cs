namespace SlnxMermaid.Core.Emit
{
    internal sealed class LegacyEdge
    {
        internal LegacyEdge(string from, string to)
        {
            From = from;
            To = to;
        }

        internal string From { get; }
        internal string To { get; }
    }
}
