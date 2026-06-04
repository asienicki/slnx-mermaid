using System;

namespace SlnxMermaid.Core.Exceptions
{
    public sealed class SolutionNotFoundException : Exception
    {
        public string FilePath { get; }

        public SolutionNotFoundException(string filePath) : base($"Solution not found: {filePath}")
        {
            FilePath = filePath;
        }
    }
}
