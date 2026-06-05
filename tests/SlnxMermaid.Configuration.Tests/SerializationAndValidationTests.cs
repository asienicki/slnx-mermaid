using SlnxMermaid.Core.Config;

namespace SlnxMermaid.Configuration.Tests;

public sealed class SerializationAndValidationTests
{
    [Fact]
    public void ToYaml_ShouldSerializeSharedConfigurationModel()
    {
        var config = new SlnxMermaidConfig
        {
            Solution = "./sample.slnx",
            Ui = new UiConfig { Mode = "light" }
        };

        var yaml = config.ToYaml();

        Assert.Contains("solution: ./sample.slnx", yaml);
        Assert.Contains("mode: light", yaml);
    }

    [Fact]
    public void ToJson_ShouldSerializeSharedConfigurationModel()
    {
        var config = new SlnxMermaidConfig { Solution = "./sample.slnx" };

        var json = config.ToJson();

        Assert.Contains("\"solution\": \"./sample.slnx\"", json);
    }

    [Fact]
    public void Validate_WhenSolutionIsMissing_ShouldReturnValidationError()
    {
        var result = new ConfigurationValidator().Validate(new SlnxMermaidConfig { Solution = null });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Solution path is required", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenSolutionIsRelative_ShouldResolveItAgainstBaseDirectory()
    {
        var baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(baseDirectory);
        var solutionPath = Path.Combine(baseDirectory, "Sample.slnx");
        File.WriteAllText(solutionPath, string.Empty);

        try
        {
            var result = new ConfigurationValidator().Validate(new SlnxMermaidConfig { Solution = "Sample.slnx" }, baseDirectory);

            Assert.True(result.IsValid);
        }
        finally
        {
            Directory.Delete(baseDirectory, recursive: true);
        }
    }

    [Fact]
    public void Validate_WhenSolutionValueLooksLikeConfigFile_ShouldNotReportMissingSolutionFile()
    {
        var result = new ConfigurationValidator().Validate(new SlnxMermaidConfig { Solution = "slnx-mermaid.yml" });

        Assert.DoesNotContain(result.Errors, error => error.Contains("Solution file does not exist", StringComparison.Ordinal));
    }
}
