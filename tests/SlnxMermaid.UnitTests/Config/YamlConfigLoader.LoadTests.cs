using SlnxMermaid.Core.Config;

namespace SlnxMermaid.UnitTests.Config;

public class YamlConfigLoaderLoadTests
{
    [Fact]
    public void Load_WhenYamlIsValid_ShouldDeserializeConfig()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
solution: ./src/sample.slnx
output:
  file: ./out/diagram.md
diagram:
  direction: LR
  includeTransitiveDependencies: false
filters:
  exclude:
    - Test
naming:
  stripPrefix: Company.
  aliases:
    Sample: S
""");

            var result = YamlConfigLoader.Load(path);

            Assert.Equal("./src/sample.slnx", result.Solution);
            Assert.Equal("./out/diagram.md", result.Output.File);
            Assert.Equal("LR", result.Diagram.Direction);
            Assert.False(result.Diagram.IncludeTransitiveDependencies);
            Assert.Single(result.Filters.Exclude);
            Assert.Equal("Company.", result.Naming.StripPrefix);
            Assert.Equal("S", result.Naming.Aliases["Sample"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WhenIncludeTransitiveDependenciesIsMissing_ShouldUseDefaultTrue()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
solution: ./src/sample.slnx
diagram:
  direction: LR
output:
  file: ./out/diagram.md
""");

            var result = YamlConfigLoader.Load(path);

            Assert.True(result.Diagram.IncludeTransitiveDependencies);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid()}.yml");

        Assert.Throws<FileNotFoundException>(() => YamlConfigLoader.Load(path));
    }
}
