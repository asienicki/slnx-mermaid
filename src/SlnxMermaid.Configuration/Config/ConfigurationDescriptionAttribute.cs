using System;

namespace SlnxMermaid.Core.Config;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class ConfigurationDescriptionAttribute : Attribute
{
    public ConfigurationDescriptionAttribute(string description) => Description = description;

    public string Description { get; }
}
