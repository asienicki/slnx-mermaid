using System.Collections.Generic;

namespace SlnxMermaid.Core.Config
{
public sealed class NamingConfig
{
    public string StripPrefix { get; set; }
    public Dictionary<string, string> Aliases { get; set; } = new Dictionary<string, string>();
}
}
