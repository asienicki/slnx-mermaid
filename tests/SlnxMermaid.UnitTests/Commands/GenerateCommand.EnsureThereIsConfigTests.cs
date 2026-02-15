using System.Reflection;
using SlnxMermaid.Cli;
using SlnxMermaid.CLI.Exceptions;

namespace SlnxMermaid.Core.Tests.Commands;

public class GenerateCommandEnsureThereIsConfigTests
{
    [Fact]
    public void EnsureThereIsConfig_WhenFileExists_ShouldReturnAbsolutePath()
    {
        var configFileName = $"slnx-mermaid-{Guid.NewGuid()}.yml";
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), configFileName);
        File.WriteAllText(absolutePath, "solution: sample.slnx");

        try
        {
            var result = (string)InvokeEnsureThereIsConfig(configFileName)!;

            Assert.Equal(Path.GetFullPath(absolutePath), result);
        }
        finally
        {
            File.Delete(absolutePath);
        }
    }

    [Fact]
    public void EnsureThereIsConfig_WhenFileMissing_ShouldThrowConfigurationFileNotFoundException()
    {
        var missing = $"missing-{Guid.NewGuid()}.yml";

        var exception = Assert.Throws<TargetInvocationException>(() => InvokeEnsureThereIsConfig(missing));

        var inner = Assert.IsType<ConfigurationFileNotFoundException>(exception.InnerException);
        Assert.Contains(missing, inner.FilePath, StringComparison.Ordinal);
    }

    private static object? InvokeEnsureThereIsConfig(string? configFile)
    {
        var method = typeof(GenerateCommand)
            .GetMethod("EnsureThereIsConfig", BindingFlags.NonPublic | BindingFlags.Static)!;

        return method.Invoke(null, [configFile]);
    }
}
