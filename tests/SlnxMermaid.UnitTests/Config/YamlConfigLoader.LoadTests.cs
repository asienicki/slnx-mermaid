using System.Collections;
using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Exceptions;
using YamlDotNet.Core;

namespace SlnxMermaid.UnitTests.Config;

// These tests intentionally exercise partially populated YAML documents. The
// loader preserves null sections/collections so validation can report them
// later, while individual assertions document when a fixture is expected to
// contain those optional values.
#pragma warning disable CS8602, CS8604

public class YamlConfigLoaderLoadTests
{
    [Fact]
    public void Load_WhenYamlIsValid_ShouldDeserializeConfig()
    {
        var path = GetConfigPath("valid-config.yml");

        var result = YamlConfigLoader.Load(path);

        Assert.Equal("./src/sample.slnx", result.Solution);
        Assert.Equal("./out/diagram.md", result.Output.File);
        Assert.Equal("LR", result.Diagram.Direction);
        Assert.False(result.Diagram.IncludeTransitiveDependencies);
        Assert.True(result.Diagram.OrderDependenciesByDepth);
        Assert.Single(result.Filters.Exclude);
        Assert.Equal("Company.", result.Naming.StripPrefix);
        Assert.Equal("S", result.Naming.Aliases["Sample"]);
    }

    [Fact]
    public void Load_WhenAllSupportedValuesExist_ShouldDeserializeEverySection()
    {
        var path = GetConfigPath("all-values-config.yml");

        var result = YamlConfigLoader.Load(path);

        Assert.Equal("../solutions/Company.Sample.slnx", result.Solution);
        Assert.Equal("../docs/architecture/{date}-sample.md", result.Output.File);
        Assert.Equal("BT", result.Diagram.Direction);
        Assert.True(result.Diagram.IncludeTransitiveDependencies);
        Assert.False(result.Diagram.OrderDependenciesByDepth);
        Assert.Equal(new[]
        {
            "*.Tests",
            "Company.Legacy.*",
            "Project With Spaces",
            "Project-With-Dashes",
            "Project_With_Underscores"
        }, result.Filters.Exclude);
        Assert.Equal("Company.Sample.", result.Naming.StripPrefix);
        Assert.Equal("API", result.Naming.Aliases["Company.Sample.Api"]);
        Assert.Equal("App Layer", result.Naming.Aliases["Company.Sample.Application"]);
        Assert.Equal("Infra", result.Naming.Aliases["Company.Sample.Infrastructure"]);
        Assert.Equal("light", result.Ui.Mode);
        Assert.Equal("blue", result.Ui.Semantic["presentation"]);
        Assert.Equal("green", result.Ui.Semantic["application"]);
        Assert.Equal("yellow", result.Ui.Semantic["domain"]);
        Assert.Equal("orange", result.Ui.Semantic["infrastructure"]);
        Assert.Equal("pink", result.Ui.Semantic["dataAccess"]);
        Assert.Equal("purple", result.Ui.Semantic["tooling"]);
        Assert.Equal("gray", result.Ui.Semantic["tests"]);
        Assert.Equal("purple", result.Ui.Mappings["Company.Sample.Api"]);
        Assert.Equal("gray", result.Ui.Mappings["*.Tests"]);
        Assert.Equal("green", result.Ui.Mappings["*Application*"]);
        Assert.Equal("#112233", result.Ui.Mappings["Company.Sample.Domain"]);
        var infrastructureStyle = Assert.IsAssignableFrom<IDictionary>(result.Ui.Mappings["Company.Sample.Infrastructure"]);
        Assert.Equal("#141414", infrastructureStyle["fill"]);
        Assert.Equal("#90CAF9", infrastructureStyle["stroke"]);
        Assert.Equal("#FFFFFF", infrastructureStyle["color"]);
    }

    [Fact]
    public void Load_WhenUiConfigExists_ShouldDeserializeSemanticAndMappings()
    {
        var path = GetConfigPath("ui-config.yml");

        var result = YamlConfigLoader.Load(path);

        Assert.Equal("light", result.Ui.Mode);
        Assert.Equal("red", result.Ui.Semantic["application"]);
        Assert.Equal("blue", result.Ui.Mappings["MinimalApi"]);
        Assert.Equal("gray", result.Ui.Mappings["*Model*"]);
        Assert.NotNull(result.Ui.Mappings["Application"]);
        Assert.NotNull(result.Ui.Mappings["Infrastructure"]);
    }

    [Fact]
    public void Load_WhenIncludeTransitiveDependenciesIsMissing_ShouldUseDefaultFalse()
    {
        var path = GetConfigPath("missing-transitive-dependencies-config.yml");

        var result = YamlConfigLoader.Load(path);

        Assert.False(result.Diagram.IncludeTransitiveDependencies);
        Assert.True(result.Diagram.OrderDependenciesByDepth);
    }

    [Fact]
    public void Load_WhenOnlySolutionExists_ShouldKeepDefaultNestedConfigs()
    {
        var result = LoadFromText("solution: ./minimal.slnx");

        Assert.Equal("./minimal.slnx", result.Solution);
        Assert.Equal("TD", result.Diagram.Direction);
        Assert.False(result.Diagram.IncludeTransitiveDependencies);
        Assert.True(result.Diagram.OrderDependenciesByDepth);
        Assert.Empty(result.Filters.Exclude);
        Assert.Null(result.Naming.StripPrefix);
        Assert.Empty(result.Naming.Aliases);
        Assert.Null(result.Output.File);
        Assert.Equal("dark", result.Ui.Mode);
        Assert.Empty(result.Ui.Semantic);
        Assert.Empty(result.Ui.Mappings);
    }

    [Fact]
    public void Load_WhenSectionsAreEmptyMappings_ShouldKeepSectionDefaults()
    {
        var path = GetConfigPath("empty-sections-config.yml");

        var result = YamlConfigLoader.Load(path);

        Assert.Equal("./src/sample.slnx", result.Solution);
        Assert.NotNull(result.Diagram);
        Assert.Equal("TD", result.Diagram.Direction);
        Assert.False(result.Diagram.IncludeTransitiveDependencies);
        Assert.True(result.Diagram.OrderDependenciesByDepth);
        Assert.NotNull(result.Filters);
        Assert.Empty(result.Filters.Exclude);
        Assert.NotNull(result.Naming);
        Assert.Null(result.Naming.StripPrefix);
        Assert.Empty(result.Naming.Aliases);
        Assert.NotNull(result.Output);
        Assert.Null(result.Output.File);
        Assert.NotNull(result.Ui);
        Assert.Equal("dark", result.Ui.Mode);
        Assert.Empty(result.Ui.Semantic);
        Assert.Empty(result.Ui.Mappings);
    }

    [Fact]
    public void Load_WhenOptionalSectionsAreExplicitNull_ShouldKeepNullSectionsForLaterValidation()
    {
        var result = LoadFromText("""
            solution: ./sample.slnx
            diagram: null
            filters: null
            naming: null
            output: null
            ui: null
            """);

        Assert.Equal("./sample.slnx", result.Solution);
        Assert.Null(result.Diagram);
        Assert.Null(result.Filters);
        Assert.Null(result.Naming);
        Assert.Null(result.Output);
        Assert.Null(result.Ui);
    }

    [Fact]
    public void Load_WhenCollectionValuesAreExplicitNull_ShouldKeepNullCollectionsForLaterValidation()
    {
        var result = LoadFromText("""
            solution: ./sample.slnx
            filters:
              exclude: null
            naming:
              aliases: null
            ui:
              semantic: null
              mappings: null
            """);

        Assert.Equal("./sample.slnx", result.Solution);
        Assert.Null(result.Filters.Exclude);
        Assert.Null(result.Naming.Aliases);
        Assert.Null(result.Ui.Semantic);
        Assert.Null(result.Ui.Mappings);
    }

    [Theory]
    [InlineData("TD")]
    [InlineData("TB")]
    [InlineData("BT")]
    [InlineData("LR")]
    [InlineData("RL")]
    [InlineData("lR")]
    [InlineData("")]
    [InlineData("   ")]
    public void Load_WhenDiagramDirectionHasAnyStringValue_ShouldPreserveValue(string direction)
    {
        var result = LoadFromText($$"""
            solution: ./sample.slnx
            diagram:
              direction: "{{direction}}"
            """);

        Assert.Equal(direction, result.Diagram.Direction);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Load_WhenDiagramBooleanMixChanges_ShouldDeserializeCombination(bool includeTransitiveDependencies, bool orderDependenciesByDepth)
    {
        var result = LoadFromText($$"""
            solution: ./sample.slnx
            diagram:
              includeTransitiveDependencies: {{includeTransitiveDependencies.ToString().ToLowerInvariant()}}
              orderDependenciesByDepth: {{orderDependenciesByDepth.ToString().ToLowerInvariant()}}
            """);

        Assert.Equal(includeTransitiveDependencies, result.Diagram.IncludeTransitiveDependencies);
        Assert.Equal(orderDependenciesByDepth, result.Diagram.OrderDependenciesByDepth);
    }

    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    [InlineData("DaRk")]
    [InlineData("LIGHT")]
    [InlineData("")]
    [InlineData("   ")]
    public void Load_WhenUiModeHasAnyStringValue_ShouldPreserveValue(string mode)
    {
        var result = LoadFromText($$"""
            solution: ./sample.slnx
            ui:
              mode: "{{mode}}"
            """);

        Assert.Equal(mode, result.Ui.Mode);
    }

    [Theory]
    [InlineData("blue")]
    [InlineData("green")]
    [InlineData("yellow")]
    [InlineData("orange")]
    [InlineData("pink")]
    [InlineData("purple")]
    [InlineData("gray")]
    [InlineData("red")]
    [InlineData("GrEeN")]
    [InlineData("unknown-user-value")]
    public void Load_WhenSemanticColorHasAnyStringValue_ShouldPreserveValueForLaterValidation(string color)
    {
        var result = LoadFromText($$"""
            solution: ./sample.slnx
            ui:
              semantic:
                application: "{{color}}"
            """);

        Assert.Equal(color, result.Ui.Semantic["application"]);
    }

    [Fact]
    public void Load_WhenYamlHasMixedCaseStringsAndSpecialCharacters_ShouldPreserveStringsAndDeserializeBooleans()
    {
        var path = GetConfigPath("edge-case-config.yml");

        var result = YamlConfigLoader.Load(path);

        Assert.Equal("./Ścieżka Z Odstępami/Sample Solution.slnx", result.Solution);
        Assert.Equal("./out/Diagram Final {date}.md", result.Output.File);
        Assert.Equal("lR", result.Diagram.Direction);
        Assert.True(result.Diagram.IncludeTransitiveDependencies);
        Assert.False(result.Diagram.OrderDependenciesByDepth);
        Assert.Empty(result.Filters.Exclude);
        Assert.Equal("  Company.  ", result.Naming.StripPrefix);
        Assert.Equal("Alias With Spaces", result.Naming.Aliases["Project With Spaces"]);
        Assert.Equal("mIxEd Alias 123", result.Naming.Aliases["UPPER.case-Mix_123"]);
        Assert.Equal("DaRk", result.Ui.Mode);
        Assert.Equal("GrEeN", result.Ui.Semantic["application"]);
        Assert.Equal("#aBc123", result.Ui.Mappings["Project With Spaces"]);
        Assert.Equal("PuRpLe", result.Ui.Mappings["*case-Mix*"]);
        var objectStyle = Assert.IsAssignableFrom<IDictionary>(result.Ui.Mappings["ObjectStyle"]);
        Assert.Equal("#abcdef", objectStyle["FILL"]);
        Assert.Equal("#123456", objectStyle["Stroke"]);
        Assert.Equal("#FEDCBA", objectStyle["color"]);
    }

    [Fact]
    public void Load_WhenYamlHasDuplicateKeys_ShouldLetLastValueWin()
    {
        var path = GetConfigPath("duplicate-keys-config.yml");

        var result = YamlConfigLoader.Load(path);

        Assert.Equal("./second.slnx", result.Solution);
        Assert.Equal("./second.md", result.Output.File);
        Assert.Equal("RL", result.Diagram.Direction);
        Assert.True(result.Diagram.IncludeTransitiveDependencies);
        Assert.False(result.Diagram.OrderDependenciesByDepth);
        Assert.Equal(new[] { "Second" }, result.Filters.Exclude);
        Assert.Equal("Second.", result.Naming.StripPrefix);
        Assert.Equal("SecondAlias", result.Naming.Aliases["Duplicate"]);
        Assert.Equal("light", result.Ui.Mode);
        Assert.Equal("red", result.Ui.Semantic["application"]);
        Assert.Equal("orange", result.Ui.Mappings["Duplicate.Project"]);
    }

    [Fact]
    public void Load_WhenUnknownPropertiesExist_ShouldIgnoreThem()
    {
        var path = GetConfigPath("unknown-properties-config.yml");

        var result = YamlConfigLoader.Load(path);

        Assert.Equal("./src/sample.slnx", result.Solution);
        Assert.Equal("LR", result.Diagram.Direction);
        Assert.Equal(new[] { "Tests" }, result.Filters.Exclude);
        Assert.Equal("Company.", result.Naming.StripPrefix);
        Assert.Equal("./out/diagram.md", result.Output.File);
        Assert.Equal("dark", result.Ui.Mode);
    }

    [Theory]
    [InlineData("empty-config.yml")]
    [InlineData("comments-only-config.yml")]
    public void Load_WhenYamlDocumentHasNoValues_ShouldThrowYamlDeserializeException(string fileName)
    {
        var path = GetConfigPath(fileName);

        var exception = Assert.Throws<YamlDeserializeException>(() => YamlConfigLoader.Load(path));

        Assert.Equal(path, exception.FilePath);
        Assert.Contains(path, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_WhenYamlIsMalformed_ShouldThrowYamlException()
    {
        var exception = Assert.Throws<YamlException>(() => LoadFromText("solution: [unterminated"));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid()}.yml");

        Assert.Throws<FileNotFoundException>(() => YamlConfigLoader.Load(path));
    }

    private static SlnxMermaidConfig LoadFromText(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"slnx-mermaid-test-{Guid.NewGuid()}.yml");
        File.WriteAllText(path, yaml);

        try
        {
            return YamlConfigLoader.Load(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string GetConfigPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "Config", fileName);
}

#pragma warning restore CS8602, CS8604
