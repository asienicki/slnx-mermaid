using SlnxMermaid.Core.Exceptions;

namespace SlnxMermaid.UnitTests.Exceptions;

public class DiagramOutputPathMissingExceptionConstructorTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultMessage()
    {
        var exception = new DiagramOutputPathMissingException();

        Assert.Equal("Diagram output file path is not configured.", exception.Message);
    }
}
