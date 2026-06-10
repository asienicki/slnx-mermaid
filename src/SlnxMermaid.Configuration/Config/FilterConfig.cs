using System.Collections.Generic;

namespace SlnxMermaid.Core.Config;

public sealed class FilterConfig
{
    [ConfigurationDescription("Case-insensitive substrings matched against normalized project ids. Matching projects are excluded from nodes, edges, and styles.")]
    public List<string> Exclude { get; set; } = new();
}
