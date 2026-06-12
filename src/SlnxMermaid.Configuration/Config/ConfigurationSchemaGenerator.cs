using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SlnxMermaid.Core.Config;

public static class ConfigurationSchemaGenerator
{
    public const string SchemaId = "https://raw.githubusercontent.com/asienicki/slnx-mermaid/master/slnx-mermaid.schema.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static string Generate()
    {
        var definitions = new JsonObject();
        var root = BuildObjectSchema(typeof(SlnxMermaidConfig), definitions, includeDescription: true);

        root.Insert(0, "$schema", "http://json-schema.org/draft-07/schema#");
        root.Insert(1, "$id", SchemaId);
        root.Insert(2, "title", "slnx-mermaid configuration");
        root["definitions"] = definitions;
        root["x-generated-by"] = "dotnet run --project tools/SlnxMermaid.SchemaGenerator";

        return root.ToJsonString(SerializerOptions) + "\n";
    }

    private static JsonObject BuildObjectSchema(Type type, JsonObject definitions, bool includeDescription)
    {
        var schema = new JsonObject();
        if (includeDescription && GetDescription(type) is { Length: > 0 } description)
            schema["description"] = description;

        schema["type"] = "object";
        schema["additionalProperties"] = false;

        var properties = new JsonObject();
        var required = new JsonArray();
        var defaults = Activator.CreateInstance(type);

        foreach (var property in GetConfigurationProperties(type))
        {
            properties[ToCamelCase(property.Name)] = BuildPropertySchema(property, defaults, definitions);
            if (property.GetCustomAttribute<ConfigurationRequiredAttribute>() != null)
                required.Add(ToCamelCase(property.Name));
        }

        schema["properties"] = properties;
        if (required.Count > 0)
            schema["required"] = required;

        return schema;
    }

    private static JsonObject BuildPropertySchema(PropertyInfo property, object? defaults, JsonObject definitions)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        JsonObject schema;

        if (type == typeof(string))
            schema = BuildStringSchema(property);
        else if (type == typeof(bool))
            schema = new JsonObject { ["type"] = "boolean" };
        else if (type.IsEnum)
            schema = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = BuildStringArray(Enum.GetNames(type))
            };
        else if (IsInteger(type))
            schema = new JsonObject { ["type"] = "integer" };
        else if (IsNumber(type))
            schema = new JsonObject { ["type"] = "number" };
        else if (IsDictionary(type))
            schema = BuildDictionarySchema(property, definitions);
        else if (IsList(type))
            schema = BuildListSchema(type);
        else
            schema = BuildReferenceSchema(type, definitions);

        var description = GetDescription(property);
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException($"Configuration property '{property.DeclaringType?.Name}.{property.Name}' must have a description.");

        schema.Insert(0, "description", description);
        AddDefault(schema, property, defaults);
        return schema;
    }

    private static JsonObject BuildStringSchema(PropertyInfo property)
    {
        var schema = new JsonObject { ["type"] = "string" };
        var allowedValues = property.GetCustomAttribute<ConfigurationAllowedValuesAttribute>();
        if (allowedValues != null)
            schema["enum"] = BuildStringArray(allowedValues.Values);

        var constraint = property.GetCustomAttribute<ConfigurationStringConstraintAttribute>();
        if (constraint?.MinLength > 0)
            schema["minLength"] = constraint.MinLength;
        if (!string.IsNullOrWhiteSpace(constraint?.Pattern))
            schema["pattern"] = constraint.Pattern;

        return schema;
    }

    private static JsonObject BuildDictionarySchema(PropertyInfo property, JsonObject definitions)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["propertyNames"] = new JsonObject
            {
                ["type"] = "string",
                ["minLength"] = 1
            }
        };

        var valueMetadata = property.GetCustomAttribute<ConfigurationDictionaryValueAttribute>()
            ?? throw new InvalidOperationException($"Dictionary property '{property.DeclaringType?.Name}.{property.Name}' must declare its schema value kind.");
        var kind = valueMetadata.Kind;
        schema["additionalProperties"] = kind switch
        {
            ConfigurationDictionaryValueKind.PaletteColor => BuildPaletteColorSchema(),
            ConfigurationDictionaryValueKind.ProjectStyle => BuildProjectStyleSchema(definitions),
            _ => new JsonObject { ["type"] = "string" }
        };

        return schema;
    }

    private static JsonObject BuildProjectStyleSchema(JsonObject definitions)
    {
        EnsureDefinition(typeof(UiStyleConfig), definitions);
        return new JsonObject
        {
            ["oneOf"] = new JsonArray
            {
                BuildPaletteColorSchema(),
                BuildHexColorSchema(),
                new JsonObject { ["$ref"] = $"#/definitions/{GetDefinitionName(typeof(UiStyleConfig))}" }
            }
        };
    }

    private static JsonObject BuildPaletteColorSchema() => new()
    {
        ["type"] = "string",
        ["enum"] = BuildStringArray(UiPalette.Names)
    };

    private static JsonObject BuildHexColorSchema() => new()
    {
        ["type"] = "string",
        ["pattern"] = "^#[0-9a-fA-F]{6}$"
    };

    private static JsonArray BuildStringArray(IEnumerable<string> values)
    {
        var result = new JsonArray();
        foreach (var value in values)
            result.Add(value);

        return result;
    }

    private static JsonObject BuildListSchema(Type type)
    {
        var itemType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object);
        return new JsonObject
        {
            ["type"] = "array",
            ["items"] = itemType == typeof(string)
                ? new JsonObject { ["type"] = "string" }
                : new JsonObject { ["type"] = "object" }
        };
    }

    private static JsonObject BuildReferenceSchema(Type type, JsonObject definitions)
    {
        EnsureDefinition(type, definitions);
        return new JsonObject { ["$ref"] = $"#/definitions/{GetDefinitionName(type)}" };
    }

    private static void EnsureDefinition(Type type, JsonObject definitions)
    {
        var name = GetDefinitionName(type);
        if (definitions.ContainsKey(name))
            return;

        definitions[name] = null;
        definitions[name] = BuildObjectSchema(type, definitions, includeDescription: true);
    }

    private static void AddDefault(JsonObject schema, PropertyInfo property, object? defaults)
    {
        var value = defaults == null ? null : property.GetValue(defaults);
        if (value == null || IsConfigurationObject(value.GetType()))
            return;

        schema["default"] = JsonSerializer.SerializeToNode(value);
    }

    private static IEnumerable<PropertyInfo> GetConfigurationProperties(Type type) => type
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
        .OrderBy(property => property.MetadataToken);

    private static bool IsDictionary(Type type) => typeof(IDictionary).IsAssignableFrom(type);

    private static bool IsList(Type type) => typeof(IList).IsAssignableFrom(type);

    private static bool IsInteger(Type type) => type == typeof(byte)
        || type == typeof(sbyte)
        || type == typeof(short)
        || type == typeof(ushort)
        || type == typeof(int)
        || type == typeof(uint)
        || type == typeof(long)
        || type == typeof(ulong);

    private static bool IsNumber(Type type) => type == typeof(float)
        || type == typeof(double)
        || type == typeof(decimal);

    private static bool IsConfigurationObject(Type type) => type.Namespace == typeof(SlnxMermaidConfig).Namespace
        && !IsDictionary(type)
        && !IsList(type);

    private static string GetDefinitionName(Type type) => type.Name;

    private static string? GetDescription(MemberInfo member) => member
        .GetCustomAttribute<ConfigurationDescriptionAttribute>()?.Description;

    private static string ToCamelCase(string value) => string.IsNullOrEmpty(value)
        ? value
        : char.ToLowerInvariant(value[0]) + value.Substring(1);
}
