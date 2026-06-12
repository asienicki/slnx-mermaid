using System;

namespace SlnxMermaid.Core.Config;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurationRequiredAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurationAllowedValuesAttribute : Attribute
{
    public ConfigurationAllowedValuesAttribute(params string[] values) => Values = values;

    public string[] Values { get; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurationStringConstraintAttribute : Attribute
{
    public int MinLength { get; set; }

    public string? Pattern { get; set; }
}

public enum ConfigurationDictionaryValueKind
{
    String,
    PaletteColor,
    ProjectStyle
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurationDictionaryValueAttribute : Attribute
{
    public ConfigurationDictionaryValueAttribute(ConfigurationDictionaryValueKind kind) => Kind = kind;

    public ConfigurationDictionaryValueKind Kind { get; }
}
