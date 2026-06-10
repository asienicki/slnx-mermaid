using System;
using System.Collections.Generic;
using System.Linq;
using SlnxMermaid.Core.Config;

namespace SlnxMermaid.Core.Emit
{
    public sealed class MermaidPalette
    {
        private const string Black = "#000000";
        private const string White = "#FFFFFF";

        private readonly Dictionary<string, MermaidNodeStyle> _styles;

        private MermaidPalette(Dictionary<string, MermaidNodeStyle> styles)
        {
            _styles = styles;
        }

        public static MermaidPalette DarkModePalette { get; private set; } = new MermaidPalette(new Dictionary<string, MermaidNodeStyle>(StringComparer.OrdinalIgnoreCase)
    {
        { UiPalette.Blue, new MermaidNodeStyle("#1565C0", "#90CAF9", White) },
        { UiPalette.Green, new MermaidNodeStyle("#2E7D32", "#A5D6A7", White) },
        { UiPalette.Yellow, new MermaidNodeStyle("#F9A825", "#FFF59D", Black) },
        { UiPalette.Orange, new MermaidNodeStyle("#EF6C00", "#FFCC80", White) },
        { UiPalette.Pink, new MermaidNodeStyle("#AD1457", "#F48FB1", White) },
        { UiPalette.Purple, new MermaidNodeStyle("#6A1B9A", "#CE93D8", White) },
        { UiPalette.Gray, new MermaidNodeStyle("#37474F", "#B0BEC5", White) },
        { UiPalette.Red, new MermaidNodeStyle("#B71C1C", "#EF9A9A", White) }
    });

        public static MermaidPalette LightModePalette { get; private set; } = new MermaidPalette(new Dictionary<string, MermaidNodeStyle>(StringComparer.OrdinalIgnoreCase)
    {
        { UiPalette.Blue, new MermaidNodeStyle("#E3F2FD", "#1976D2", Black) },
        { UiPalette.Green, new MermaidNodeStyle("#E8F5E9", "#388E3C", Black) },
        { UiPalette.Yellow, new MermaidNodeStyle("#FFFDE7", "#FBC02D", Black) },
        { UiPalette.Orange, new MermaidNodeStyle("#FFF3E0", "#F57C00", Black) },
        { UiPalette.Pink, new MermaidNodeStyle("#FCE4EC", "#C2185B", Black) },
        { UiPalette.Purple, new MermaidNodeStyle("#F3E5F5", "#7B1FA2", Black) },
        { UiPalette.Gray, new MermaidNodeStyle("#ECEFF1", "#607D8B", Black) },
        { UiPalette.Red, new MermaidNodeStyle("#FFEBEE", "#D32F2F", Black) }
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
