using System;

namespace SlnxMermaid.Core.Config;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurationDescriptionAttribute : Attribute
{
    public ConfigurationDescriptionAttribute(string description) => Description = description;

    public string Description { get; }
}
