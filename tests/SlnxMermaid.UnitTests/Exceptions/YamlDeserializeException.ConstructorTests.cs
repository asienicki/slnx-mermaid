using SlnxMermaid.CLI.Exceptions;

namespace SlnxMermaid.Core.Tests.Exceptions;

public class YamlDeserializeExceptionConstructorTests
{
    [Fact]
    public void Constructor_ShouldSetFilePathAndMessage()
    {
        const string path = "slnx-mermaid.yml";

        var exception = new YamlDeserializeException(path);

        Assert.Equal(path, exception.FilePath);
        Assert.Contains(path, exception.Message, StringComparison.Ordinal);
    }
}
