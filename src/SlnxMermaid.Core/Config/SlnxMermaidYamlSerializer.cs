using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlnxMermaid.Core.Config
{
    public static class SlnxMermaidYamlSerializer
    {
        public static string ToYaml(this SlnxMermaidConfig config)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return serializer.Serialize(config);
        }
    }
}