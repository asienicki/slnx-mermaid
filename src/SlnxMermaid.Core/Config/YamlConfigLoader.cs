using System.IO;
using SlnxMermaid.CLI.Exceptions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlnxMermaid.Core.Config
{
    public static class YamlConfigLoader
    {
        public static SlnxMermaidConfig Load(string path)
        {
            using (var reader = File.OpenText(path))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var result = deserializer.Deserialize<SlnxMermaidConfig>(reader);

                if (result == null)
                    throw new YamlDeserializeException(path);

                return result;
            }
        }
    }
}