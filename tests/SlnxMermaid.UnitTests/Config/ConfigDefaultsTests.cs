using SlnxMermaid.Core.Config;

namespace SlnxMermaid.UnitTests.Config;

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
        Assert.NotNull(config.Ui);
    }

    [Fact]
    public void DiagramConfig_DefaultDirection_ShouldBeTd()
    {
        var config = new DiagramConfig();

        Assert.Equal("TD", config.Direction);
    }

    [Fact]
    public void DiagramConfig_DefaultIncludeTransitiveDependencies_ShouldBeFalse()
    {
        var config = new DiagramConfig();

        Assert.False(config.IncludeTransitiveDependencies);
    }

    [Fact]
    public void DiagramConfig_DefaultOrderDependenciesByDepth_ShouldBeTrue()
    {
        var config = new DiagramConfig();

        Assert.True(config.OrderDependenciesByDepth);
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
    public void UiConfig_DefaultMode_ShouldBeDark()
    {
        var config = new UiConfig();

        Assert.Equal("dark", config.Mode);
    }

    [Fact]
    public void OutputConfig_DefaultFile_ShouldBeNull()
    {
        var config = new OutputConfig();

        Assert.Null(config.File);
    }
}
