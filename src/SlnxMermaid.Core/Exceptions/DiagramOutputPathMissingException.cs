using System;

namespace SlnxMermaid.CLI.Exceptions
{
    public sealed class DiagramOutputPathMissingException : Exception
    {
        public DiagramOutputPathMissingException()
            : base("Diagram output file path is not configured.")
        {
        }
    }
}
