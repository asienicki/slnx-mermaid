using SlnxMermaid.Core.Exceptions;

namespace SlnxMermaid.UnitTests.Exceptions;

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
