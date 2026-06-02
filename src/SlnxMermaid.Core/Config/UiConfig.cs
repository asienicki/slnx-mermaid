using System.Collections.Generic;

namespace SlnxMermaid.Core.Config
{
public sealed class UiConfig
{
    public string Mode { get; set; } = "dark";

    public Dictionary<string, string> Semantic { get; set; } = new Dictionary<string, string>();

    public Dictionary<string, object> Mappings { get; set; } = new Dictionary<string, object>();
}
}
