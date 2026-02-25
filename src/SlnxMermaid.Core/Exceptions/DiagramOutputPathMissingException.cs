using System;

namespace SlnxMermaid.Core.Exceptions
{
    public sealed class DiagramOutputPathMissingException : Exception
    {
        public DiagramOutputPathMissingException()
            : base("Diagram output file path is not configured.")
        {
        }
    }
}
