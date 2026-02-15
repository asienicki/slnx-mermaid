using System;
using System.Collections.Generic;
using SlnxMermaid.Core.Config;

namespace SlnxMermaid.Core.Naming
{
public sealed class NameTransformer
{
    private readonly string _stripPrefix;
    private readonly Dictionary<string, string> _aliases;

    public NameTransformer(NamingConfig namingConfig)
    {
        _stripPrefix = namingConfig.StripPrefix;
        _aliases = namingConfig.Aliases;
    }

    public string Transform(string rawName)
    {
        var name = rawName;

        if (!string.IsNullOrEmpty(_stripPrefix) &&
            name.StartsWith(_stripPrefix, StringComparison.Ordinal))
        {
            name = name.Substring(_stripPrefix.Length);
        }

        return _aliases != null && _aliases.TryGetValue(name, out var alias)
            ? alias
            : name;
    }
}
}
