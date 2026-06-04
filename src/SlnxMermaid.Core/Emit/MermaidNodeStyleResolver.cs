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
        private const string ExpectedHexMessage = "Expected #RRGGBB.";
        private const string Black = "#000000";
        private const string White = "#FFFFFF";
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
        private static readonly Regex HexRegex = new Regex("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled, RegexTimeout);
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
                return ApplyTextMapping(text, baseStyle, baseColorName);

            return ApplyStyleMapping(value, baseStyle, baseColorName, projectName);
        }

        private MermaidNodeResolvedStyle ApplyTextMapping(string text, MermaidNodeStyle baseStyle, string baseColorName)
        {
            var trimmedText = text.Trim();
            if (LooksLikeHex(trimmedText) && !IsHex(trimmedText))
                throw new InvalidOperationException("Invalid hex value '" + trimmedText + "'. " + ExpectedHexMessage);

            if (IsHex(trimmedText))
            {
                var normalizedHex = NormalizeHex(trimmedText);
                var style = baseStyle != null
                    ? baseStyle.With(normalizedHex, null, null)
                    : AutoStyle(normalizedHex);
                return new MermaidNodeResolvedStyle(style, baseColorName, true);
            }

            MermaidNodeStyle paletteStyle;
            if (!_palette.TryGet(trimmedText, out paletteStyle))
                throw new InvalidOperationException("Unknown palette color name '" + trimmedText + "'.");

            return new MermaidNodeResolvedStyle(paletteStyle, trimmedText.ToLowerInvariant(), false);
        }

        private MermaidNodeResolvedStyle ApplyStyleMapping(object value, MermaidNodeStyle baseStyle, string baseColorName, string projectName)
        {
            Dictionary<string, object> values;
            if (!TryGetStringObjectDictionary(value, out values))
                throw new InvalidOperationException("Invalid mapping value type for project '" + projectName + "'. Use a palette name, hex value, or style object.");

            string fill = null;
            string stroke = null;
            string color = null;

            foreach (var entry in values)
            {
                var field = entry.Key;
                var normalizedValue = GetNormalizedStyleFieldValue(field, entry.Value);

                if (string.Equals(field, "fill", StringComparison.OrdinalIgnoreCase))
                    fill = normalizedValue;
                else if (string.Equals(field, "stroke", StringComparison.OrdinalIgnoreCase))
                    stroke = normalizedValue;
                else
                    color = normalizedValue;
            }

            var resolvedBase = baseStyle ?? (fill != null ? AutoStyle(fill) : ResolveFallback(projectName));
            var resolved = resolvedBase.With(fill, stroke, color);
            return new MermaidNodeResolvedStyle(resolved, baseColorName ?? FallbackColorName(projectName), fill != null || stroke != null || color != null);
        }

        private static string GetNormalizedStyleFieldValue(string field, object value)
        {
            EnsureSupportedStyleField(field);

            var fieldValue = value as string;
            if (fieldValue == null || !IsHex(fieldValue.Trim()))
                throw new InvalidOperationException("Invalid hex value for style field '" + field + "'. " + ExpectedHexMessage);

            return NormalizeHex(fieldValue);
        }

        private static void EnsureSupportedStyleField(string field)
        {
            if (!string.Equals(field, "fill", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(field, "stroke", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(field, "color", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid or unsupported style field '" + field + "'. Allowed fields are fill, stroke, and color.");
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
                ValidateTextMapping(key, text);
                return;
            }

            Dictionary<string, object> values;
            if (!TryGetStringObjectDictionary(value, out values))
                throw new InvalidOperationException("Invalid mapping value type for mapping '" + key + "'. Use a palette name, hex value, or style object.");

            foreach (var entry in values)
                ValidateStyleField(key, entry.Key, entry.Value);
        }

        private void ValidateTextMapping(string key, string text)
        {
            var trimmedText = text.Trim();
            if (LooksLikeHex(trimmedText) && !IsHex(trimmedText))
                throw new InvalidOperationException("Invalid hex value '" + trimmedText + "' for mapping '" + key + "'. " + ExpectedHexMessage);

            if (IsHex(trimmedText))
                return;

            if (!_palette.TryGet(trimmedText, out _))
                throw new InvalidOperationException("Unknown palette color name '" + trimmedText + "' for mapping '" + key + "'.");
        }

        private static void ValidateStyleField(string key, string field, object value)
        {
            EnsureSupportedStyleField(field);

            var fieldValue = value as string;
            if (fieldValue == null || !IsHex(fieldValue.Trim()))
                throw new InvalidOperationException("Invalid hex value for style field '" + field + "' in mapping '" + key + "'. " + ExpectedHexMessage);
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

        private static string DetectRole(string projectName)
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
            var segmentMatch = roles
                .Where(role => role.Value.Any(term => segments.Any(segment => string.Equals(segment, term, StringComparison.OrdinalIgnoreCase))))
                .Select(role => role.Key)
                .FirstOrDefault();
            if (segmentMatch != null)
                return segmentMatch;

            return roles
                .Where(role => role.Value.Any(term => projectName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(role => role.Key)
                .FirstOrDefault();
        }

        private static IEnumerable<string> SplitSegments(string projectName)
        {
            return projectName.Split(new[] { '.', '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private MermaidNodeStyle ResolveFallback(string projectName)
        {
            return _palette.Get(FallbackColorName(projectName));
        }

        private static string FallbackColorName(string projectName)
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
            return Regex.IsMatch(value, regex, RegexOptions.CultureInvariant, RegexTimeout);
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

        private static bool TryGetStringObjectDictionary(object value, out Dictionary<string, object> result)
        {
            var typed = value as Dictionary<string, object>;
            if (typed != null)
            {
                result = typed;
                return true;
            }

            var dictionary = value as IDictionary;
            if (dictionary == null)
            {
                result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                return false;
            }

            result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dictionary)
                result[Convert.ToString(entry.Key, CultureInfo.InvariantCulture)] = entry.Value;

            return true;
        }

        private static MermaidNodeStyle AutoStyle(string fill)
        {
            var readable = IsLight(fill) ? Black : White;
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
