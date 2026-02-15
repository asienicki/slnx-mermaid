using SlnxMermaid.CLI.Exceptions;

namespace SlnxMermaid.Core.Tests.Exceptions;

public class SolutionNotFoundExceptionConstructorTests
{
    [Fact]
    public void Constructor_ShouldSetFilePathAndMessage()
    {
        const string path = "solution.slnx";

        var exception = new SolutionNotFoundException(path);

        Assert.Equal(path, exception.FilePath);
        Assert.Contains(path, exception.Message, StringComparison.Ordinal);
    }
}
