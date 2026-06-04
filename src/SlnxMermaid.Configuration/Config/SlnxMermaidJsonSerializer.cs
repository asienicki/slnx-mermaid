using System.Text.Json;

namespace SlnxMermaid.Core.Config;

public static class SlnxMermaidJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string ToJson(this SlnxMermaidConfig config) => JsonSerializer.Serialize(config, Options);

    public static SlnxMermaidConfig? FromJson(string json) => JsonSerializer.Deserialize<SlnxMermaidConfig>(json, Options);
}
