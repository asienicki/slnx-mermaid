using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace SlnxMermaid.Gui.Avalonia.Views;

public sealed class YamlPreviewView : UserControl
{
    private static readonly Regex KeyPattern = new("^(\\s*)([A-Za-z0-9_\"'*.-]+)(\\s*:)(.*)$", RegexOptions.Compiled);
    private static readonly Regex ListPattern = new("^(\\s*-\\s*)(.*)$", RegexOptions.Compiled);
    private readonly StackPanel _lines = new() { Spacing = 0 };

    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<YamlPreviewView, string?>(nameof(Text));

    public YamlPreviewView()
    {
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = _lines
        };
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
            Render(change.NewValue as string);
    }

    private void Render(string? text)
    {
        _lines.Children.Clear();
        foreach (var line in (text ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            _lines.Children.Add(CreateLine(line));
    }

    private static TextBlock CreateLine(string line)
    {
        var block = new TextBlock
        {
            FontFamily = new FontFamily("Consolas, Menlo, Monaco, monospace"),
            FontSize = 13,
            TextWrapping = TextWrapping.NoWrap,
            Margin = new Thickness(0, 0, 0, 1)
        };

        if (string.IsNullOrWhiteSpace(line))
        {
            block.Inlines!.Add(new Run(" "));
            return block;
        }

        var trimmedStart = line.TrimStart();
        if (trimmedStart.StartsWith('#'))
        {
            AddRun(block, line, "#6A9955");
            return block;
        }

        var keyMatch = KeyPattern.Match(line);
        if (keyMatch.Success)
        {
            AddRun(block, keyMatch.Groups[1].Value, "#D4D4D4");
            AddRun(block, keyMatch.Groups[2].Value, "#4FC1FF");
            AddRun(block, keyMatch.Groups[3].Value, "#D4D4D4");
            AddValueRuns(block, keyMatch.Groups[4].Value);
            return block;
        }

        var listMatch = ListPattern.Match(line);
        if (listMatch.Success)
        {
            AddRun(block, listMatch.Groups[1].Value, "#D4D4D4");
            AddValueRuns(block, listMatch.Groups[2].Value);
            return block;
        }

        AddValueRuns(block, line);
        return block;
    }

    private static void AddValueRuns(TextBlock block, string value)
    {
        var commentIndex = value.IndexOf('#');
        if (commentIndex >= 0)
        {
            AddScalarRun(block, value[..commentIndex]);
            AddRun(block, value[commentIndex..], "#6A9955");
            return;
        }

        AddScalarRun(block, value);
    }

    private static void AddScalarRun(TextBlock block, string value)
    {
        var trimmed = value.Trim();
        var color = trimmed switch
        {
            "true" or "false" => "#569CD6",
            _ when trimmed.StartsWith('"') || trimmed.StartsWith('\'') => "#CE9178",
            _ when Regex.IsMatch(trimmed, "^#[0-9A-Fa-f]{6}$") => "#CE9178",
            _ => "#DCDCAA"
        };
        AddRun(block, value, color);
    }

    private static void AddRun(TextBlock block, string text, string color)
    {
        block.Inlines!.Add(new Run(text) { Foreground = Brush.Parse(color) });
    }
}
