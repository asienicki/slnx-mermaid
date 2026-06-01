using System;
using System.Collections.Generic;

namespace SlnxMermaid.Core.Emit
{
internal sealed class LegacyEdgeComparer : IEqualityComparer<LegacyEdge>
{
    internal static readonly LegacyEdgeComparer Instance = new LegacyEdgeComparer();

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
