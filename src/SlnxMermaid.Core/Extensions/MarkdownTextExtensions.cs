namespace SlnxMermaid.Core.Extensions;

public static class MarkdownTextExtensions 
{
    public static string WrapCodeForMarkdown(this string mermaid)
    {
        return WrapCodeForMarkdown(mermaid, "mermaid");
    }

    public static string WrapCodeForMarkdown(this string mermaid, string mdCodeLang)
    {
        return
            $"```{mdCodeLang}{Environment.NewLine}" +
            $"{mermaid}{Environment.NewLine}" +
            $"```";
    }
}
