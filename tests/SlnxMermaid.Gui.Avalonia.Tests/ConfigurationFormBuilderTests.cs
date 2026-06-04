using SlnxMermaid.Core.Config;
using SlnxMermaid.Gui.Avalonia.Services;
using SlnxMermaid.Gui.Avalonia.ViewModels.Form;

namespace SlnxMermaid.Gui.Avalonia.Tests;

public sealed class ConfigurationFormBuilderTests
{
    [Fact]
    public void Build_WhenPropertyIsEnum_ShouldCreateEnumFieldViewModel()
    {
        var config = new TestConfiguration();
        var fields = new ConfigurationFormBuilder().Build(config);

        Assert.IsType<EnumFieldViewModel>(Assert.Single(fields.Where(field => field.Name == nameof(TestConfiguration.Mode))));
    }

    [Fact]
    public void Build_WhenEnumHasValues_ShouldExposeEveryEnumValueAutomatically()
    {
        var config = new TestConfiguration();
        var field = Assert.IsType<EnumFieldViewModel>(new ConfigurationFormBuilder().Build(config).Single(item => item.Name == nameof(TestConfiguration.Mode)));

        Assert.Equal(Enum.GetValues<TestUiMode>().Cast<object>(), field.Values);
        Assert.Contains(TestUiMode.System, field.Values);
    }

    [Fact]
    public void Build_WhenPropertyIsString_ShouldCreateTextFieldViewModel()
    {
        var fields = new ConfigurationFormBuilder().Build(new TestConfiguration());

        Assert.IsType<TextFieldViewModel>(fields.Single(field => field.Name == nameof(TestConfiguration.Name)));
    }

    [Fact]
    public void Build_WhenPropertyIsBool_ShouldCreateBooleanFieldViewModel()
    {
        var fields = new ConfigurationFormBuilder().Build(new TestConfiguration());

        Assert.IsType<BooleanFieldViewModel>(fields.Single(field => field.Name == nameof(TestConfiguration.Enabled)));
    }

    [Fact]
    public void Build_WhenPropertyIsNestedObject_ShouldCreateObjectFieldViewModelWithChildFields()
    {
        var fields = new ConfigurationFormBuilder().Build(new TestConfiguration());
        var nested = Assert.IsType<ObjectFieldViewModel>(fields.Single(field => field.Name == nameof(TestConfiguration.Nested)));

        Assert.Contains(nested.Fields, field => field.Name == nameof(NestedConfiguration.Fill) && field is TextFieldViewModel);
    }

    [Fact]
    public void Build_WhenViewModelValueChanges_ShouldUpdateSourceModel()
    {
        var config = new TestConfiguration();
        var fields = new ConfigurationFormBuilder().Build(config);
        var text = Assert.IsType<TextFieldViewModel>(fields.Single(field => field.Name == nameof(TestConfiguration.Name)));
        var mode = Assert.IsType<EnumFieldViewModel>(fields.Single(field => field.Name == nameof(TestConfiguration.Mode)));

        text.Value = "Changed";
        mode.SelectedValue = TestUiMode.System;

        Assert.Equal("Changed", config.Name);
        Assert.Equal(TestUiMode.System, config.Mode);
    }

    [Fact]
    public void ToYaml_AfterViewModelValueChanges_ShouldSerializeUpdatedConfiguration()
    {
        var config = new SlnxMermaidConfig { Solution = "./sample.slnx" };
        var fields = new ConfigurationFormBuilder().Build(config);
        var solution = Assert.IsType<TextFieldViewModel>(fields.Single(field => field.Name == nameof(SlnxMermaidConfig.Solution)));

        solution.Value = "./changed.slnx";
        var yaml = config.ToYaml();

        Assert.Contains("solution: ./changed.slnx", yaml);
    }

    [Fact]
    public void GuiAssembly_ShouldNotDeclareConfigurationModelDuplicates()
    {
        var guiAssembly = typeof(ConfigurationFormBuilder).Assembly;
        var duplicatedModels = guiAssembly.GetTypes()
            .Where(type => type.Namespace != null)
            .Where(type => type.Namespace.Contains("Config", StringComparison.OrdinalIgnoreCase))
            .Where(type => !type.Name.EndsWith("ViewModel", StringComparison.Ordinal))
            .Where(type => type.Name.EndsWith("Configuration", StringComparison.Ordinal) || type.Name.EndsWith("Config", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(duplicatedModels);
    }

    private sealed class TestConfiguration
    {
        public string? Name { get; set; }

        public bool Enabled { get; set; }

        public TestUiMode Mode { get; set; } = TestUiMode.Dark;

        public NestedConfiguration Nested { get; set; } = new();
    }

    private sealed class NestedConfiguration
    {
        public string? Fill { get; set; }
    }

    private enum TestUiMode
    {
        Light,
        Dark,
        System
    }
}
