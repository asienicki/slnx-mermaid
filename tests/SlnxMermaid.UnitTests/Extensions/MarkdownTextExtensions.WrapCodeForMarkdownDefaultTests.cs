using SlnxMermaid.Core.Extensions;

namespace SlnxMermaid.Core.Tests.Extensions;

public class MarkdownTextExtensionsWrapCodeForMarkdownDefaultTests
{
    [Fact]
    public void WrapCodeForMarkdown_WithoutLanguage_ShouldUseMermaidLanguage()
    {
        const string mermaid = "graph TD";

        var result = mermaid.WrapCodeForMarkdown();

        Assert.StartsWith($"```mermaid{Environment.NewLine}", result, StringComparison.Ordinal);
        Assert.Contains($"graph TD{Environment.NewLine}", result, StringComparison.Ordinal);
        Assert.EndsWith("```", result, StringComparison.Ordinal);
    }
}
