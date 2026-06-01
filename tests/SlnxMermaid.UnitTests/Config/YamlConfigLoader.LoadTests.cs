using SlnxMermaid.Core.Config;

namespace SlnxMermaid.UnitTests.Config;

public class YamlConfigLoaderLoadTests
{
    [Fact]
    public void Load_WhenYamlIsValid_ShouldDeserializeConfig()
    {
        var path = GetConfigPath("valid-config.yml");

        var result = YamlConfigLoader.Load(path);

        Assert.Equal("./src/sample.slnx", result.Solution);
        Assert.Equal("./out/diagram.md", result.Output.File);
        Assert.Equal("LR", result.Diagram.Direction);
        Assert.False(result.Diagram.IncludeTransitiveDependencies);
        Assert.True(result.Diagram.OrderDependenciesByRole);
        Assert.Single(result.Filters.Exclude);
        Assert.Equal("Company.", result.Naming.StripPrefix);
        Assert.Equal("S", result.Naming.Aliases["Sample"]);
    }

    [Fact]
    public void Load_WhenIncludeTransitiveDependenciesIsMissing_ShouldUseDefaultFalse()
    {
        var path = GetConfigPath("missing-transitive-dependencies-config.yml");

        var result = YamlConfigLoader.Load(path);

        Assert.False(result.Diagram.IncludeTransitiveDependencies);
        Assert.False(result.Diagram.OrderDependenciesByRole);
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid()}.yml");

        Assert.Throws<FileNotFoundException>(() => YamlConfigLoader.Load(path));
    }

    private static string GetConfigPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "Config", fileName);
}
