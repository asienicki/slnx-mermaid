using SlnxMermaid.CLI.Exceptions;

namespace SlnxMermaid.Core.Tests.Exceptions;

public class ConfigurationFileNotFoundExceptionConstructorTests
{
    [Fact]
    public void Constructor_ShouldSetFilePathAndMessage()
    {
        const string path = "config.yml";

        var exception = new ConfigurationFileNotFoundException(path);

        Assert.Equal(path, exception.FilePath);
        Assert.Contains(path, exception.Message, StringComparison.Ordinal);
    }
}
