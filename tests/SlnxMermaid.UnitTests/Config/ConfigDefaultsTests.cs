using SlnxMermaid.Core.Config;

namespace SlnxMermaid.Core.Tests.Config;

public class ConfigDefaultsTests
{
    [Fact]
    public void SlnxMermaidConfig_Defaults_ShouldInitializeNestedConfigs()
    {
        var config = new SlnxMermaidConfig { Solution = "sample.slnx" };

        Assert.NotNull(config.Diagram);
        Assert.NotNull(config.Filters);
        Assert.NotNull(config.Naming);
        Assert.NotNull(config.Output);
    }

    [Fact]
    public void DiagramConfig_DefaultDirection_ShouldBeTd()
    {
        var config = new DiagramConfig();

        Assert.Equal("TD", config.Direction);
    }

    [Fact]
    public void FilterConfig_DefaultExclude_ShouldBeEmptyList()
    {
        var config = new FilterConfig();

        Assert.Empty(config.Exclude);
    }

    [Fact]
    public void NamingConfig_DefaultAliases_ShouldBeEmptyDictionary()
    {
        var config = new NamingConfig();

        Assert.Empty(config.Aliases);
        Assert.Null(config.StripPrefix);
    }

    [Fact]
    public void OutputConfig_DefaultFile_ShouldBeNull()
    {
        var config = new OutputConfig();

        Assert.Null(config.File);
    }
}
