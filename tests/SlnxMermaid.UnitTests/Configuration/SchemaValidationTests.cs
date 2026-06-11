using System.Text.Json;
using System.Text.Json.Nodes;
using NJsonSchema;
using NJsonSchema.Validation;
using SlnxMermaid.Core.Config;
using YamlDotNet.Serialization;

namespace SlnxMermaid.UnitTests.Configuration;

public sealed class SchemaValidationTests
{
    [Fact]
    public void GeneratedSchema_ShouldMatchCommittedSchema()
    {
        var committedSchema = File.ReadAllText(GetRepositoryPath("slnx-mermaid.schema.json"));

        Assert.Equal(ConfigurationSchemaGenerator.Generate(), committedSchema);
    }

    [Fact]
    public async Task SchemaFile_ShouldBeValidJsonSchema()
    {
        var schemaText = File.ReadAllText(GetRepositoryPath("slnx-mermaid.schema.json"));

        Assert.NotNull(JsonNode.Parse(schemaText));
        Assert.NotNull(await JsonSchema.FromJsonAsync(schemaText));
    }

    [Fact]
    public async Task ExampleConfig_ShouldConformToSchema()
    {
        var errors = await ValidateYaml(File.ReadAllText(GetRepositoryPath("examples", "slnx-mermaid.yml")));

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Validate_WhenDiagramDirectionIsInvalid_ShouldFail()
    {
        const string yaml = """
            solution: Sample.slnx
            diagram:
              direction: diagonal
            output:
              file: dependency-graph.md
            """;

        var errors = await ValidateYaml(yaml);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task Validate_WhenUnknownRootPropertyExists_ShouldFail()
    {
        const string yaml = """
            solution: Sample.slnx
            unsupportedOption: true
            output:
              file: dependency-graph.md
            """;

        var errors = await ValidateYaml(yaml);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task Validate_WhenUnknownNestedPropertyExists_ShouldFail()
    {
        const string yaml = """
            solution: Sample.slnx
            diagram:
              direction: TD
              unsupportedOption: true
            output:
              file: dependency-graph.md
            """;

        var errors = await ValidateYaml(yaml);

        Assert.NotEmpty(errors);
    }

    private static async Task<ICollection<ValidationError>> ValidateYaml(string yaml)
    {
        var schemaText = File.ReadAllText(GetRepositoryPath("slnx-mermaid.schema.json"));
        var schema = await JsonSchema.FromJsonAsync(schemaText);
        var yamlObject = new DeserializerBuilder()
            .WithAttemptingUnquotedStringTypeDeserialization()
            .Build()
            .Deserialize<object>(yaml);
        var json = JsonSerializer.Serialize(ConvertYamlValue(yamlObject));

        return schema.Validate(json);
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
}
