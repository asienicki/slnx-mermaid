using SlnxMermaid.CLI.Exceptions;
using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Extensions;

namespace SlnxMermaid.Core.Tests.Extensions;

public class SlnxMermaidConfigExtensionsValidateTests
{
    [Fact]
    public void Validate_WhenSolutionExists_ShouldReturnSameConfigInstance()
    {
        var solutionPath = Path.GetTempFileName();
        var config = CreateConfig(solutionPath, outputFile: null);

        try
        {
            var result = config.Validate();

            Assert.Same(config, result);
        }
        finally
        {
            File.Delete(solutionPath);
        }
    }

    [Fact]
    public void Validate_WhenSolutionDoesNotExist_ShouldThrowSolutionNotFoundException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid()}.slnx");
        var config = CreateConfig(missingPath, outputFile: null);

        var exception = Assert.Throws<SolutionNotFoundException>(() => config.Validate());

        Assert.Equal(missingPath, exception.FilePath);
        Assert.Contains(missingPath, exception.Message, StringComparison.Ordinal);
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
