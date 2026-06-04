using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlnxMermaid.Core.Config;

public static class SlnxMermaidYamlSerializer
{
    public static string ToYaml(this SlnxMermaidConfig config)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return serializer.Serialize(config);
    }

    public static SlnxMermaidConfig? FromYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<SlnxMermaidConfig>(yaml);
    }
}
