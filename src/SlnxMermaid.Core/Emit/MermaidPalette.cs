using System;
using System.Collections.Generic;
using System.Linq;

namespace SlnxMermaid.Core.Emit
{
    public sealed class MermaidPalette
    {
        private readonly Dictionary<string, MermaidNodeStyle> _styles;

        private MermaidPalette(Dictionary<string, MermaidNodeStyle> styles)
        {
            _styles = styles;
        }

        public static MermaidPalette DarkModePalette { get; private set; } = new MermaidPalette(new Dictionary<string, MermaidNodeStyle>(StringComparer.OrdinalIgnoreCase)
    {
        { "blue", new MermaidNodeStyle("#1565C0", "#90CAF9", "#FFFFFF") },
        { "green", new MermaidNodeStyle("#2E7D32", "#A5D6A7", "#FFFFFF") },
        { "yellow", new MermaidNodeStyle("#F9A825", "#FFF59D", "#000000") },
        { "orange", new MermaidNodeStyle("#EF6C00", "#FFCC80", "#FFFFFF") },
        { "pink", new MermaidNodeStyle("#AD1457", "#F48FB1", "#FFFFFF") },
        { "purple", new MermaidNodeStyle("#6A1B9A", "#CE93D8", "#FFFFFF") },
        { "gray", new MermaidNodeStyle("#37474F", "#B0BEC5", "#FFFFFF") },
        { "red", new MermaidNodeStyle("#B71C1C", "#EF9A9A", "#FFFFFF") }
    });

        public static MermaidPalette LightModePalette { get; private set; } = new MermaidPalette(new Dictionary<string, MermaidNodeStyle>(StringComparer.OrdinalIgnoreCase)
    {
        { "blue", new MermaidNodeStyle("#E3F2FD", "#1976D2", "#000000") },
        { "green", new MermaidNodeStyle("#E8F5E9", "#388E3C", "#000000") },
        { "yellow", new MermaidNodeStyle("#FFFDE7", "#FBC02D", "#000000") },
        { "orange", new MermaidNodeStyle("#FFF3E0", "#F57C00", "#000000") },
        { "pink", new MermaidNodeStyle("#FCE4EC", "#C2185B", "#000000") },
        { "purple", new MermaidNodeStyle("#F3E5F5", "#7B1FA2", "#000000") },
        { "gray", new MermaidNodeStyle("#ECEFF1", "#607D8B", "#000000") },
        { "red", new MermaidNodeStyle("#FFEBEE", "#D32F2F", "#000000") }
    });

        public IEnumerable<string> Names
        {
            get { return _styles.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase); }
        }

        public bool TryGet(string name, out MermaidNodeStyle style)
        {
            return _styles.TryGetValue(name, out style);
        }

        public MermaidNodeStyle Get(string name)
        {
            MermaidNodeStyle style;
            if (!TryGet(name, out style))
                throw new InvalidOperationException("Unknown palette color name '" + name + "'.");

            return style;
        }

        public static MermaidPalette ForMode(string mode)
        {
            var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "dark" : mode.Trim();

            if (string.Equals(normalizedMode, "dark", StringComparison.OrdinalIgnoreCase))
                return DarkModePalette;

            if (string.Equals(normalizedMode, "light", StringComparison.OrdinalIgnoreCase))
                return LightModePalette;

            throw new InvalidOperationException("Unknown ui.mode '" + mode + "'. Supported values are 'dark' and 'light'.");
        }
    }
}
