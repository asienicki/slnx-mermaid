using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Emit;
using SlnxMermaid.Core.Filtering;
using SlnxMermaid.Core.Graph;
using SlnxMermaid.Core.Naming;

namespace SlnxMermaid.UnitTests.Emit;

public class MermaidEmitterUiStyleTests
{
    [Fact]
    public void Emit_WithUiAndMissingMode_ShouldUseDefaultDarkMode()
    {
        var result = Emit([Node("MinimalApi")], new UiConfig { Mode = null! });

        Assert.Contains("classDef cls_blue fill:#1565C0,stroke:#90CAF9,color:#FFFFFF", result);
        Assert.Contains("class MinimalApi cls_blue", result);
    }

    [Fact]
    public void Emit_WithLightMode_ShouldUseLightPalette()
    {
        var result = Emit([Node("MinimalApi")], new UiConfig { Mode = "light" });

        Assert.Contains("classDef cls_blue fill:#E3F2FD,stroke:#1976D2,color:#000000", result);
    }

    [Fact]
    public void Emit_WhenSemanticIsMissing_ShouldUseSemanticDefaults()
    {
        var result = Emit([Node("Application")], new UiConfig());

        Assert.Contains("classDef cls_green fill:#2E7D32,stroke:#A5D6A7,color:#FFFFFF", result);
        Assert.Contains("class Application cls_green", result);
    }

    [Fact]
    public void Emit_WhenExactMappingOverrideExists_ShouldUseMappedPaletteColor()
    {
        var ui = new UiConfig { Mappings = { ["MinimalApi"] = "red" } };

        var result = Emit([Node("MinimalApi")], ui);

        Assert.Contains("classDef cls_red fill:#B71C1C,stroke:#EF9A9A,color:#FFFFFF", result);
        Assert.Contains("class MinimalApi cls_red", result);
    }

    [Fact]
    public void Emit_WhenWildcardMappingOverrideExists_ShouldUseMostSpecificWildcardMapping()
    {
        var ui = new UiConfig
        {
            Mappings =
            {
                ["*Model*"] = "gray",
                ["*UserModel"] = "purple"
            }
        };

        var result = Emit([Node("UserModel")], ui);

        Assert.Contains("classDef cls_purple fill:#6A1B9A,stroke:#CE93D8,color:#FFFFFF", result);
        Assert.Contains("class UserModel cls_purple", result);
    }

    [Fact]
    public void Emit_WhenExactAndWildcardMappingsMatch_ShouldPreferExactMapping()
    {
        var ui = new UiConfig
        {
            Mappings =
            {
                ["*Model*"] = "gray",
                ["UserModel"] = "red"
            }
        };

        var result = Emit([Node("UserModel")], ui);

        Assert.Contains("classDef cls_red fill:#B71C1C,stroke:#EF9A9A,color:#FFFFFF", result);
        Assert.Contains("class UserModel cls_red", result);
    }

    [Fact]
    public void Emit_WhenPartialStyleObjectExists_ShouldMergeMissingFieldsFromSemanticBaseStyle()
    {
        var ui = new UiConfig
        {
            Mappings =
            {
                ["Application"] = new Dictionary<string, object> { ["fill"] = "#141414" }
            }
        };

        var result = Emit([Node("Application")], ui);

        Assert.Contains("classDef cls_green_custom_141414 fill:#141414,stroke:#A5D6A7,color:#FFFFFF", result);
    }

    [Fact]
    public void Emit_WhenFullStyleObjectExists_ShouldUseFullCustomStyle()
    {
        var ui = new UiConfig
        {
            Mappings =
            {
                ["Infrastructure"] = new Dictionary<string, object>
                {
                    ["fill"] = "#EF6C00",
                    ["stroke"] = "#FFCC80",
                    ["color"] = "#FFFFFF"
                }
            }
        };

        var result = Emit([Node("Infrastructure")], ui);

        Assert.Contains("classDef cls_orange_custom_ef6c00 fill:#EF6C00,stroke:#FFCC80,color:#FFFFFF", result);
    }

    [Fact]
    public void Emit_WhenRawHexMappingExists_ShouldUseFillAndSemanticBaseStrokeAndColor()
    {
        var ui = new UiConfig { Mappings = { ["Application"] = "#141414" } };

        var result = Emit([Node("Application")], ui);

        Assert.Contains("classDef cls_green_custom_141414 fill:#141414,stroke:#A5D6A7,color:#FFFFFF", result);
    }

    [Fact]
    public void Emit_WhenModeIsUnknown_ShouldThrowClearError()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Emit([Node("Application")], new UiConfig { Mode = "sepia" }));

        Assert.Contains("Unknown ui.mode", ex.Message);
    }

    [Fact]
    public void Emit_WhenPaletteColorIsUnknown_ShouldThrowClearError()
    {
        var ui = new UiConfig { Mappings = { ["Application"] = "teal" } };

        var ex = Assert.Throws<InvalidOperationException>(() => Emit([Node("Application")], ui));

        Assert.Contains("Unknown palette color name", ex.Message);
    }

    [Fact]
    public void Emit_WhenHexIsInvalid_ShouldThrowClearError()
    {
        var ui = new UiConfig
        {
            Mappings =
            {
                ["Application"] = new Dictionary<string, object> { ["fill"] = "141414" }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => Emit([Node("Application")], ui));

        Assert.Contains("Invalid hex value", ex.Message);
    }


    [Fact]
    public void Emit_WhenMappingKeyIsEmpty_ShouldThrowClearError()
    {
        var ui = new UiConfig { Mappings = { [""] = "blue" } };

        var ex = Assert.Throws<InvalidOperationException>(() => Emit([Node("Application")], ui));

        Assert.Contains("Mapping keys cannot be empty", ex.Message);
    }

    [Fact]
    public void Emit_WhenStyleFieldIsUnsupported_ShouldThrowClearError()
    {
        var ui = new UiConfig
        {
            Mappings =
            {
                ["Application"] = new Dictionary<string, object> { ["border"] = "#141414" }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => Emit([Node("Application")], ui));

        Assert.Contains("Invalid or unsupported style field", ex.Message);
    }

    [Fact]
    public void Emit_WhenMappingValueTypeIsUnsupported_ShouldThrowClearError()
    {
        var ui = new UiConfig { Mappings = { ["Application"] = 42 } };

        var ex = Assert.Throws<InvalidOperationException>(() => Emit([Node("Application")], ui));

        Assert.Contains("Invalid mapping value type", ex.Message);
    }

    [Fact]
    public void Emit_WhenNoMappingOrSemanticRoleMatches_ShouldUseDeterministicFallbackColor()
    {
        var first = Emit([Node("Billing")], new UiConfig());
        var second = Emit([Node("Billing")], new UiConfig());

        Assert.Equal(first, second);
        Assert.Contains("class Billing cls_", first);
    }

    [Fact]
    public void Emit_WithUi_ShouldEmitClassDefinitionsOnceAndAssignEveryNodeAfterDefinitions()
    {
        var app = Node("Application");
        var appAlias = Node("MyApp.Application");
        app.Dependencies.Add(appAlias);

        var result = Emit([app, appAlias], new UiConfig());

        Assert.Equal(1, CountOccurrences(result, "classDef cls_green "));
        Assert.Contains("class Application cls_green", result);
        Assert.Contains("class MyApp.Application cls_green", result);
        Assert.True(result.IndexOf("classDef", StringComparison.Ordinal) < result.IndexOf("class Application", StringComparison.Ordinal));
    }

    private static string Emit(IEnumerable<ProjectNode> nodes, UiConfig ui) =>
        CreateEmitter().Emit(nodes, new DiagramConfig { Direction = "TD", OrderDependenciesByDepth = false }, ui);

    private static ProjectNode Node(string id) => new(id, id + ".csproj");

    private static MermaidEmitter CreateEmitter() =>
        new(new NameTransformer(new NamingConfig()), new ProjectFilter([]));

    private static int CountOccurrences(string value, string find)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(find, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += find.Length;
        }

        return count;
    }
}
