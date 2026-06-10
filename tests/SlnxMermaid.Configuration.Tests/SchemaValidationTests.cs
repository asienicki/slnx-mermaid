using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using YamlDotNet.Serialization;

namespace SlnxMermaid.Configuration.Tests;

public sealed class SchemaValidationTests
{
    [Fact]
    public void GeneratedSchema_ShouldMatchCommittedSchema()
    {
        var committedSchema = File.ReadAllText(GetRepositoryPath("schemas", "slnx-mermaid.schema.json"));

        Assert.Equal(ConfigurationSchemaGenerator.Generate(), committedSchema);
    }

    [Fact]
    public void SchemaFile_ShouldBeValidJsonSchema()
    {
        var schemaText = File.ReadAllText(GetRepositoryPath("schemas", "slnx-mermaid.schema.json"));

        Assert.NotNull(JsonNode.Parse(schemaText));
        Assert.NotNull(JsonSchema.FromText(schemaText));
    }

    [Fact]
    public void ExampleConfig_ShouldConformToSchema()
    {
        var result = ValidateYaml(File.ReadAllText(GetRepositoryPath("examples", "slnx-mermaid.yml")));

        Assert.True(result.IsValid, FormatErrors(result));
    }

    [Fact]
    public void Validate_WhenDiagramDirectionIsInvalid_ShouldFail()
    {
        const string yaml = """
            solution: Sample.slnx
            diagram:
              direction: diagonal
            output:
              file: dependency-graph.md
            """;

        var result = ValidateYaml(yaml);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUnknownRootPropertyExists_ShouldFail()
    {
        const string yaml = """
            solution: Sample.slnx
            unsupportedOption: true
            output:
              file: dependency-graph.md
            """;

        var result = ValidateYaml(yaml);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUnknownNestedPropertyExists_ShouldFail()
    {
        const string yaml = """
            solution: Sample.slnx
            diagram:
              direction: TD
              unsupportedOption: true
            output:
              file: dependency-graph.md
            """;

        var result = ValidateYaml(yaml);

        Assert.False(result.IsValid);
    }

    private static EvaluationResults ValidateYaml(string yaml)
    {
        var schemaText = File.ReadAllText(GetRepositoryPath("schemas", "slnx-mermaid.schema.json"));
        var schema = JsonSchema.FromText(schemaText);
        var yamlObject = new DeserializerBuilder().Build().Deserialize<object>(yaml);
        var json = JsonSerializer.Serialize(ConvertYamlValue(yamlObject));
        var node = JsonNode.Parse(json);

        return schema.Evaluate(node, new EvaluationOptions { OutputFormat = OutputFormat.List });
    }

    private static object? ConvertYamlValue(object? value)
    {
        if (value is null)
            return null;

        if (value is IDictionary<object, object> objectDictionary)
        {
            return objectDictionary.ToDictionary(
                pair => Convert.ToString(pair.Key, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                pair => ConvertYamlValue(pair.Value));
        }

        if (value is IDictionary<string, object> stringDictionary)
            return stringDictionary.ToDictionary(pair => pair.Key, pair => ConvertYamlValue(pair.Value));

        if (value is IEnumerable<object> sequence && value is not string)
            return sequence.Select(ConvertYamlValue).ToArray();

        return value;
    }

    private static string GetRepositoryPath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SlnxMermaid.slnx")))
            directory = directory.Parent;

        if (directory == null)
            throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");

        return Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
    }

    private static string FormatErrors(EvaluationResults results)
    {
        return $"Schema validation failed: {results}";
    }
}
