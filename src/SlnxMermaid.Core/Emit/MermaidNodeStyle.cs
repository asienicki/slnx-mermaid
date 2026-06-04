using System;

namespace SlnxMermaid.Core.Emit
{
    public sealed class MermaidNodeStyle : IEquatable<MermaidNodeStyle>
    {
        public MermaidNodeStyle(string fill, string stroke, string color)
        {
            Fill = fill;
            Stroke = stroke;
            Color = color;
        }

        public string Fill { get; private set; }

        public string Stroke { get; private set; }

        public string Color { get; private set; }

        public bool Equals(MermaidNodeStyle other)
        {
            if (other == null)
                return false;

            return string.Equals(Fill, other.Fill, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Stroke, other.Stroke, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Color, other.Color, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MermaidNodeStyle);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Fill ?? string.Empty);
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Stroke ?? string.Empty);
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Color ?? string.Empty);
                return hash;
            }
        }

        public MermaidNodeStyle With(string fill, string stroke, string color)
        {
            return new MermaidNodeStyle(fill ?? Fill, stroke ?? Stroke, color ?? Color);
        }
    }
}
