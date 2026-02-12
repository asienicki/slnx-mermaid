using SlnxMermaid.CLI.Exceptions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlnxMermaid.Core.Config;

public static class YamlConfigLoader
{
    public static SlnxMermaidConfig Load(string path)
    {
        using var reader = File.OpenText(path);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<SlnxMermaidConfig>(reader)
               ?? throw new YamlDeserializeException(path);
    }
}
