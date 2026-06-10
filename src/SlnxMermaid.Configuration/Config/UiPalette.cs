using System.Collections.Generic;

namespace SlnxMermaid.Core.Config;

public static class UiPalette
{
    public const string Blue = "blue";
    public const string Green = "green";
    public const string Yellow = "yellow";
    public const string Orange = "orange";
    public const string Pink = "pink";
    public const string Purple = "purple";
    public const string Gray = "gray";
    public const string Red = "red";

    public static IReadOnlyList<string> Names { get; } = new[]
    {
        Blue,
        Green,
        Yellow,
        Orange,
        Pink,
        Purple,
        Gray,
        Red
    };
}
