using System;

namespace SlnxMermaid.Core.Config;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurationDisplayNameAttribute : Attribute
{
    public ConfigurationDisplayNameAttribute(string displayName) => DisplayName = displayName;

    public string DisplayName { get; }
}
