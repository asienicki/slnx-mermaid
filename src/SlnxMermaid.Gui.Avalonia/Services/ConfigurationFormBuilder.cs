using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SlnxMermaid.Core.Config;
using SlnxMermaid.Gui.Avalonia.ViewModels.Form;

namespace SlnxMermaid.Gui.Avalonia.Services;

public interface IConfigurationFormBuilder
{
    IReadOnlyList<FormFieldViewModel> Build(object configuration);
}

public sealed class ConfigurationFormBuilder : IConfigurationFormBuilder
{
    public IReadOnlyList<FormFieldViewModel> Build(object configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        return BuildObjectFields(configuration);
    }

    private IReadOnlyList<FormFieldViewModel> BuildObjectFields(object instance) => instance.GetType()
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
        .Select(property => BuildField(instance, property))
        .ToArray();

    private FormFieldViewModel BuildField(object owner, PropertyInfo property)
    {
        var value = property.GetValue(owner);
        var valueType = property.PropertyType;
        var effectiveType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        var name = property.Name;
        var displayName = property.GetCustomAttribute<ConfigurationDisplayNameAttribute>()?.DisplayName ?? ToDisplayName(name);
        var description = property.GetCustomAttribute<ConfigurationDescriptionAttribute>()?.Description ?? string.Empty;

        if (effectiveType == typeof(string))
        {
            if (owner is OutputConfig && string.Equals(name, nameof(OutputConfig.File), StringComparison.Ordinal) && string.IsNullOrWhiteSpace(value?.ToString()))
            {
                value = "dependency-graph-mermaid.md";
                property.SetValue(owner, value);
            }

            if (owner is SlnxMermaidConfig && string.Equals(name, nameof(SlnxMermaidConfig.Solution), StringComparison.Ordinal))
                return new FilePathFieldViewModel(name, displayName, description, valueType, value?.ToString(), owner, property);

            if (owner is DiagramConfig && string.Equals(name, nameof(DiagramConfig.Direction), StringComparison.Ordinal))
                return new ChoiceFieldViewModel(name, displayName, description, valueType, new[] { "TD", "LR", "BT", "RL" }, value?.ToString(), owner, property);

            return new TextFieldViewModel(name, displayName, description, valueType, value?.ToString(), owner, property);
        }

        if (effectiveType == typeof(bool))
            return new BooleanFieldViewModel(name, displayName, description, valueType, value is true, owner, property);

        if (effectiveType.IsEnum)
            return new EnumFieldViewModel(name, displayName, description, valueType, Enum.GetValues(effectiveType).Cast<object>().ToArray(), value, owner, property);

        if (IsNumeric(effectiveType))
            return new NumericFieldViewModel(name, displayName, description, valueType, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), owner, property);

        if (typeof(IDictionary).IsAssignableFrom(effectiveType))
            return new DictionaryFieldViewModel(name, displayName, description, valueType, EnsureDictionary(owner, property, value), IsColorDictionary(owner, name));

        if (typeof(IList).IsAssignableFrom(effectiveType))
            return new ListFieldViewModel(name, displayName, description, valueType, EnsureList(owner, property, value));

        var nested = EnsureNestedObject(owner, property, value, effectiveType);
        return new ObjectFieldViewModel(name, displayName, description, valueType, BuildObjectFields(nested));
    }

    private static bool IsColorDictionary(object owner, string name) => owner is UiConfig
        && (string.Equals(name, nameof(UiConfig.Semantic), StringComparison.Ordinal)
            || string.Equals(name, nameof(UiConfig.Mappings), StringComparison.Ordinal));

    private static IDictionary? EnsureDictionary(object owner, PropertyInfo property, object? value)
    {
        if (value is IDictionary dictionary)
            return dictionary;

        var created = Activator.CreateInstance(property.PropertyType);
        property.SetValue(owner, created);
        return created as IDictionary;
    }

    private static IList? EnsureList(object owner, PropertyInfo property, object? value)
    {
        if (value is IList list)
            return list;

        var created = Activator.CreateInstance(property.PropertyType);
        property.SetValue(owner, created);
        return created as IList;
    }

    private static object EnsureNestedObject(object owner, PropertyInfo property, object? value, Type effectiveType)
    {
        if (value != null)
            return value;

        var created = Activator.CreateInstance(effectiveType) ?? throw new InvalidOperationException($"Cannot create configuration section {property.Name}.");
        property.SetValue(owner, created);
        return created;
    }

    private static bool IsNumeric(Type type) => type == typeof(int) || type == typeof(double) || type == typeof(decimal);

    private static string ToDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var chars = new List<char> { name[0] };
        for (var i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && !char.IsWhiteSpace(name[i - 1]))
                chars.Add(' ');
            chars.Add(name[i]);
        }

        return new string(chars.ToArray());
    }
}
