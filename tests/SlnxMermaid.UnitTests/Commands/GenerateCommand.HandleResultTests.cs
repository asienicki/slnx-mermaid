using System.Reflection;
using SlnxMermaid.Cli;
using SlnxMermaid.CLI.Exceptions;

namespace SlnxMermaid.Core.Tests.Commands;

public class GenerateCommandHandleResultTests
{
    [Fact]
    public async Task HandleResult_WhenOutputPathIsMissing_ShouldThrowDiagramOutputPathMissingException()
    {
        var exception = await Assert.ThrowsAsync<TargetInvocationException>(() =>
            InvokeHandleResultAsync(null, "graph TD", _ => { }, CancellationToken.None));

        Assert.IsType<DiagramOutputPathMissingException>(exception.InnerException);
    }

    [Fact]
    public async Task HandleResult_WhenOutputPathProvided_ShouldWriteWrappedMarkdownAndEmitMessages()
    {
        var root = Path.Combine(Path.GetTempPath(), $"result-{Guid.NewGuid()}");
        var outputPath = Path.Combine(root, "out", "diagram.md");
        var messages = new List<string>();

        try
        {
            await InvokeHandleResultAsync(outputPath, "graph TD", messages.Add, CancellationToken.None);

            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("```mermaid", content, StringComparison.Ordinal);
            Assert.Contains("graph TD", content, StringComparison.Ordinal);
            Assert.Equal(2, messages.Count);
            Assert.Contains("Diagram written to", messages[1], StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static Task InvokeHandleResultAsync(string? outputPath, string mermaid, Action<string> markupLine, CancellationToken cancellationToken)
    {
        var method = typeof(GenerateCommand)
            .GetMethod("HandleResult", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (Task)method.Invoke(null, [outputPath, mermaid, markupLine, cancellationToken])!;
    }
}
