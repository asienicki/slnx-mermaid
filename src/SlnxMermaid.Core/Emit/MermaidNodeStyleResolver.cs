using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SlnxMermaid.Core.Config;

namespace SlnxMermaid.Core.Emit
{
    public sealed class MermaidNodeStyleResolver
    {
        private static readonly Regex HexRegex = new Regex("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
        private static readonly string[] FallbackColorOrder = { "blue", "green", "yellow", "orange", "pink", "purple", "gray", "red" };

        private readonly UiConfig _ui;
        private readonly MermaidPalette _palette;
        private readonly Dictionary<string, string> _semantic;

        public MermaidNodeStyleResolver(UiConfig ui)
        {
            _ui = ui ?? new UiConfig();
            _palette = MermaidPalette.ForMode(_ui.Mode);
            _semantic = BuildSemanticMap(_ui.Semantic);
            ValidateSemanticColors();
            ValidateMappings();
        }

        public MermaidNodeResolvedStyle Resolve(string projectName)
        {
            var role = DetectRole(projectName);
            var baseColorName = role != null ? _semantic[role] : null;
            var baseStyle = baseColorName != null ? _palette.Get(baseColorName) : ResolveFallback(projectName);

            object mappingValue;
            string mappingKey;
            if (TryGetExactMapping(projectName, out mappingKey, out mappingValue) || TryGetWildcardMapping(projectName, out mappingKey, out mappingValue))
                return ApplyMapping(mappingValue, baseColorName != null ? baseStyle : null, baseColorName, projectName);

            if (baseColorName != null)
                return new MermaidNodeResolvedStyle(baseStyle, baseColorName, false);

            return new MermaidNodeResolvedStyle(baseStyle, FallbackColorName(projectName), false);
        }

        private MermaidNodeResolvedStyle ApplyMapping(object value, MermaidNodeStyle baseStyle, string baseColorName, string projectName)
        {
            var text = value as string;
            if (text != null)
            {
                text = text.Trim();
                if (LooksLikeHex(text) && !IsHex(text))
                    throw new InvalidOperationException("Invalid hex value '" + text + "'. Expected #RRGGBB.");

                if (IsHex(text))
                {
                    var style = baseStyle != null
                        ? baseStyle.With(NormalizeHex(text), null, null)
                        : AutoStyle(NormalizeHex(text));
                    return new MermaidNodeResolvedStyle(style, baseColorName, true);
                }

                MermaidNodeStyle paletteStyle;
                if (!_palette.TryGet(text, out paletteStyle))
                    throw new InvalidOperationException("Unknown palette color name '" + text + "'.");

                return new MermaidNodeResolvedStyle(paletteStyle, text.ToLowerInvariant(), false);
            }

            var values = ToStringObjectDictionary(value);
            if (values == null)
                throw new InvalidOperationException("Invalid mapping value type for project '" + projectName + "'. Use a palette name, hex value, or style object.");

            string fill = null;
            string stroke = null;
            string color = null;

            foreach (var entry in values)
            {
                var field = entry.Key;
                if (!string.Equals(field, "fill", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(field, "stroke", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(field, "color", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Invalid or unsupported style field '" + field + "'. Allowed fields are fill, stroke, and color.");

                var fieldValue = entry.Value as string;
                if (fieldValue == null || !IsHex(fieldValue.Trim()))
                    throw new InvalidOperationException("Invalid hex value for style field '" + field + "'. Expected #RRGGBB.");

                if (string.Equals(field, "fill", StringComparison.OrdinalIgnoreCase))
                    fill = NormalizeHex(fieldValue);
                else if (string.Equals(field, "stroke", StringComparison.OrdinalIgnoreCase))
                    stroke = NormalizeHex(fieldValue);
                else
                    color = NormalizeHex(fieldValue);
            }

            var resolvedBase = baseStyle ?? (fill != null ? AutoStyle(fill) : ResolveFallback(projectName));
            var resolved = resolvedBase.With(fill, stroke, color);
            return new MermaidNodeResolvedStyle(resolved, baseColorName ?? FallbackColorName(projectName), fill != null || stroke != null || color != null);
        }

        private static Dictionary<string, string> BuildSemanticMap(Dictionary<string, string> configured)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "presentation", "blue" },
            { "application", "green" },
            { "domain", "yellow" },
            { "infrastructure", "orange" },
            { "dataaccess", "pink" },
            { "tooling", "purple" },
            { "tests", "gray" }
        };

            if (configured != null)
            {
                foreach (var entry in configured)
                    result[entry.Key] = entry.Value;
            }

            return result;
        }

        private void ValidateSemanticColors()
        {
            foreach (var entry in _semantic)
            {
                if (string.IsNullOrWhiteSpace(entry.Value) || !_palette.TryGet(entry.Value, out _))
                    throw new InvalidOperationException("Unknown palette color name '" + entry.Value + "' for semantic role '" + entry.Key + "'.");
            }
        }

        private void ValidateMappings()
        {
            if (_ui.Mappings == null)
                return;

            foreach (var entry in _ui.Mappings)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                    throw new InvalidOperationException("Mapping keys cannot be empty.");

                ValidateMappingValue(entry.Key, entry.Value);
            }
        }

        private void ValidateMappingValue(string key, object value)
        {
            var text = value as string;
            if (text != null)
            {
                text = text.Trim();
                if (LooksLikeHex(text) && !IsHex(text))
                    throw new InvalidOperationException("Invalid hex value '" + text + "' for mapping '" + key + "'. Expected #RRGGBB.");

                if (IsHex(text))
                    return;

                if (!_palette.TryGet(text, out _))
                    throw new InvalidOperationException("Unknown palette color name '" + text + "' for mapping '" + key + "'.");

                return;
            }

            var values = ToStringObjectDictionary(value);
            if (values == null)
                throw new InvalidOperationException("Invalid mapping value type for mapping '" + key + "'. Use a palette name, hex value, or style object.");

            foreach (var entry in values)
            {
                if (!string.Equals(entry.Key, "fill", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(entry.Key, "stroke", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(entry.Key, "color", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Invalid or unsupported style field '" + entry.Key + "'. Allowed fields are fill, stroke, and color.");

                var fieldValue = entry.Value as string;
                if (fieldValue == null || !IsHex(fieldValue.Trim()))
                    throw new InvalidOperationException("Invalid hex value for style field '" + entry.Key + "' in mapping '" + key + "'. Expected #RRGGBB.");
            }
        }

        private bool TryGetExactMapping(string projectName, out string key, out object value)
        {
            key = null;
            value = null;
            if (_ui.Mappings == null)
                return false;

            foreach (var entry in _ui.Mappings)
            {
                if (entry.Key.IndexOf('*') >= 0)
                    continue;

                if (string.Equals(entry.Key, projectName, StringComparison.Ordinal))
                {
                    key = entry.Key;
                    value = entry.Value;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetWildcardMapping(string projectName, out string key, out object value)
        {
            key = null;
            value = null;
            if (_ui.Mappings == null)
                return false;

            var match = _ui.Mappings
                .Where(entry => entry.Key.IndexOf('*') >= 0 && WildcardMatches(entry.Key, projectName))
                .OrderByDescending(entry => Specificity(entry.Key))
                .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                .FirstOrDefault();

            if (match.Key == null)
                return false;

            key = match.Key;
            value = match.Value;
            return true;
        }

        private string DetectRole(string projectName)
        {
            var roles = new List<KeyValuePair<string, string[]>>
        {
            new KeyValuePair<string, string[]>("tests", new[] { "Tests", "Test", "Spec", "Specs" }),
            new KeyValuePair<string, string[]>("presentation", new[] { "Api", "Web", "MinimalApi", "Host", "Gateway" }),
            new KeyValuePair<string, string[]>("application", new[] { "Application" }),
            new KeyValuePair<string, string[]>("domain", new[] { "Domain", "Core" }),
            new KeyValuePair<string, string[]>("infrastructure", new[] { "Infrastructure" }),
            new KeyValuePair<string, string[]>("dataaccess", new[] { "DataAccess", "Persistence", "Database", "Storage" }),
            new KeyValuePair<string, string[]>("tooling", new[] { "Seeder", "Migrator", "Tools", "Tool", "CLI", "Console" })
        };

            var segments = SplitSegments(projectName);
            foreach (var role in roles)
            {
                if (role.Value.Any(term => segments.Any(segment => string.Equals(segment, term, StringComparison.OrdinalIgnoreCase))))
                    return role.Key;
            }

            foreach (var role in roles)
            {
                if (role.Value.Any(term => projectName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                    return role.Key;
            }

            return null;
        }

        private static IEnumerable<string> SplitSegments(string projectName)
        {
            return projectName.Split(new[] { '.', '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private MermaidNodeStyle ResolveFallback(string projectName)
        {
            return _palette.Get(FallbackColorName(projectName));
        }

        private string FallbackColorName(string projectName)
        {
            var hash = StableHash(projectName ?? string.Empty);
            return FallbackColorOrder[hash % FallbackColorOrder.Length];
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var ch in value.ToUpperInvariant())
                {
                    hash ^= ch;
                    hash *= 16777619;
                }

                return (int)(hash & 0x7fffffff);
            }
        }

        private static bool WildcardMatches(string pattern, string value)
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(value, regex, RegexOptions.CultureInvariant);
        }

        private static int Specificity(string pattern)
        {
            return pattern.Count(ch => ch != '*');
        }

        private static bool LooksLikeHex(string value)
        {
            return value != null && value.StartsWith("#", StringComparison.Ordinal);
        }

        private static bool IsHex(string value)
        {
            return value != null && HexRegex.IsMatch(value);
        }

        private static string NormalizeHex(string value)
        {
            return value.Trim().ToUpperInvariant();
        }

        private static Dictionary<string, object> ToStringObjectDictionary(object value)
        {
            var typed = value as Dictionary<string, object>;
            if (typed != null)
                return typed;

            var dictionary = value as IDictionary;
            if (dictionary == null)
                return null;

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dictionary)
                result[Convert.ToString(entry.Key, CultureInfo.InvariantCulture)] = entry.Value;

            return result;
        }

        private static MermaidNodeStyle AutoStyle(string fill)
        {
            var readable = IsLight(fill) ? "#000000" : "#FFFFFF";
            var stroke = readable;
            return new MermaidNodeStyle(fill, stroke, readable);
        }

        private static bool IsLight(string hex)
        {
            var r = int.Parse(hex.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var g = int.Parse(hex.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var b = int.Parse(hex.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var luminance = (0.299 * r) + (0.587 * g) + (0.114 * b);
            return luminance > 186;
        }
    }

    public sealed class MermaidNodeResolvedStyle
    {
        public MermaidNodeResolvedStyle(MermaidNodeStyle style, string baseName, bool custom)
        {
            Style = style;
            BaseName = baseName;
            Custom = custom;
        }

        public MermaidNodeStyle Style { get; private set; }

        public string BaseName { get; private set; }

        public bool Custom { get; private set; }
    }
}
