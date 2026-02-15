using SlnxMermaid.Core.Extensions;

namespace SlnxMermaid.Core.Tests.Extensions;

public class MarkdownTextExtensionsWrapCodeForMarkdownWithLanguageTests
{
    [Fact]
    public void WrapCodeForMarkdown_WithCustomLanguage_ShouldUseProvidedLanguage()
    {
        const string mermaid = "graph LR";
        const string language = "text";

        var result = mermaid.WrapCodeForMarkdown(language);

        Assert.StartsWith($"```{language}{Environment.NewLine}", result, StringComparison.Ordinal);
        Assert.Contains($"{mermaid}{Environment.NewLine}", result, StringComparison.Ordinal);
        Assert.EndsWith("```", result, StringComparison.Ordinal);
    }
}
