using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Extensions;

namespace SlnxMermaid.Core.Tests.Extensions;

public class SlnxMermaidConfigExtensionsNormalizeTests
{
    [Fact]
    public void Normalize_WhenConfigIsNull_ShouldThrowArgumentNullException()
    {
        SlnxMermaidConfig? config = null;

        var exception = Assert.Throws<ArgumentNullException>(() => config!.Normalize("slnx-mermaid.yml"));

        Assert.Equal("config", exception.ParamName);
    }

    [Fact]
    public void Normalize_WhenPathsAreRelative_ShouldConvertToAbsoluteUsingConfigDirectory()
    {
        var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var configPath = Path.Combine(baseDir, "configs", "slnx-mermaid.yml");
        var config = CreateConfig(
            solutionPath: Path.Combine("src", "Sample.slnx"),
            outputFile: Path.Combine("out", "diagram.md"));

        var expectedBaseDir = Path.GetDirectoryName(Path.GetFullPath(configPath));
        var expectedSolution = Path.GetFullPath(Path.Combine(expectedBaseDir!, "src", "Sample.slnx"));
        var expectedOutput = Path.GetFullPath(Path.Combine(expectedBaseDir!, "out", "diagram.md"));

        var result = config.Normalize(configPath);

        Assert.Same(config, result);
        Assert.Equal(expectedSolution, config.Solution);
        Assert.Equal(expectedOutput, config.Output.File);
    }

    [Fact]
    public void Normalize_WhenSolutionAndOutputAreWhitespace_ShouldLeaveThemUnchanged()
    {
        var config = CreateConfig(solutionPath: "   ", outputFile: "");

        config.Normalize("slnx-mermaid.yml");

        Assert.Equal("   ", config.Solution);
        Assert.Equal("", config.Output.File);
    }

    [Fact]
    public void Normalize_WhenOutputContainsDatePlaceholder_ShouldReplaceIt()
    {
        var config = CreateConfig(
            solutionPath: Path.GetFullPath(Path.Combine(Path.GetTempPath(), "sample.slnx")),
            outputFile: "diagram-{date}.md");

        config.Normalize("slnx-mermaid.yml");

        Assert.NotNull(config.Output.File);
        Assert.DoesNotContain("{date}", config.Output.File);
        Assert.Contains("diagram-", config.Output.File, StringComparison.Ordinal);
        Assert.EndsWith(".md", config.Output.File, StringComparison.Ordinal);
    }

    private static SlnxMermaidConfig CreateConfig(string solutionPath, string? outputFile) =>
        new()
        {
            Solution = solutionPath,
            Output = new OutputConfig
            {
                File = outputFile
            }
        };
}
