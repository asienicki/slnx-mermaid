using SlnxMermaid.Core.Config;
using SlnxMermaid.Gui.Avalonia.Services;
using SlnxMermaid.Gui.Avalonia.ViewModels;
using SlnxMermaid.Gui.Avalonia.ViewModels.Form;

namespace SlnxMermaid.UnitTests.Gui.Avalonia;

public sealed class ConfigurationFormBuilderTests
{
    [Fact]
    public void Build_WhenPropertyIsEnum_ShouldCreateEnumFieldViewModel()
    {
        var config = new TestConfiguration();
        var fields = new ConfigurationFormBuilder().Build(config);

        Assert.IsType<EnumFieldViewModel>(Assert.Single(fields, field => field.Name == nameof(TestConfiguration.Mode)));
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
    public void Build_WhenSolutionProperty_ShouldCreateFilePathFieldViewModel()
    {
        var fields = new ConfigurationFormBuilder().Build(new SlnxMermaidConfig());

        Assert.IsType<FilePathFieldViewModel>(fields.Single(field => field.Name == nameof(SlnxMermaidConfig.Solution)));
    }

    [Fact]
    public void Build_WhenDiagramDirectionProperty_ShouldCreateTextFieldViewModel()
    {
        var fields = new ConfigurationFormBuilder().Build(new DiagramConfig());

        Assert.IsType<TextFieldViewModel>(fields.Single(field => field.Name == nameof(DiagramConfig.Direction)));
    }

    [Fact]
    public void Build_WhenUiModeProperty_ShouldUseAllowedValuesFromConfigurationMetadata()
    {
        var fields = new ConfigurationFormBuilder().Build(new UiConfig());
        var mode = Assert.IsType<ChoiceFieldViewModel>(fields.Single(field => field.Name == nameof(UiConfig.Mode)));

        Assert.Equal(new[] { "dark", "light" }, mode.Values);
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
        var solution = Assert.IsType<FilePathFieldViewModel>(fields.Single(field => field.Name == nameof(SlnxMermaidConfig.Solution)));

        solution.Value = "./changed.slnx";
        var yaml = config.ToYaml();

        Assert.Contains("solution: ./changed.slnx", yaml);
    }

    [Fact]
    public void ListField_AddItem_ShouldUpdateSourceList()
    {
        var filters = new FilterConfig();
        var field = Assert.IsType<ListFieldViewModel>(new ConfigurationFormBuilder().Build(filters).Single(item => item.Name == nameof(FilterConfig.Exclude)));

        field.NewItemValue = "Tests";
        field.AddItemCommand.Execute(null);

        Assert.Contains("Tests", filters.Exclude);
    }

    [Fact]
    public void DictionaryField_AddEntry_ShouldUpdateSourceDictionary()
    {
        var naming = new NamingConfig();
        var field = Assert.IsType<DictionaryFieldViewModel>(new ConfigurationFormBuilder().Build(naming).Single(item => item.Name == nameof(NamingConfig.Aliases)));

        field.NewEntryKey = "Long.Project.Name";
        field.NewEntryValue = "Short";
        field.AddEntryCommand.Execute(null);

        Assert.Equal("Short", naming.Aliases["Long.Project.Name"]);
    }

    [Fact]
    public void Build_WhenUiColorDictionary_ShouldExposePaletteAndCustomHexEditing()
    {
        var ui = new UiConfig { Semantic = new Dictionary<string, string> { ["application"] = "#112233" } };
        var field = Assert.IsType<DictionaryFieldViewModel>(new ConfigurationFormBuilder().Build(ui).Single(item => item.Name == nameof(UiConfig.Semantic)));

        Assert.True(field.UsesColorEditor);
        Assert.Contains("blue", field.ColorChoices);
        Assert.Contains("custom", field.ColorChoices);
        Assert.Equal("custom", field.Entries.Single().SelectedColor);
        Assert.Equal("#112233", field.Entries.Single().CustomHexColor);

        field.NewEntryKey = "presentation";
        field.NewEntryValue = "custom";
        field.NewEntryCustomHexColor = "#445566";
        field.AddEntryCommand.Execute(null);

        Assert.Equal("#445566", ui.Semantic!["presentation"]);
    }

    [Fact]
    public void DictionaryField_AddEntry_ShouldIgnoreBlankKeysAndUpdateDuplicateKeys()
    {
        var naming = new NamingConfig();
        var field = Assert.IsType<DictionaryFieldViewModel>(new ConfigurationFormBuilder().Build(naming).Single(item => item.Name == nameof(NamingConfig.Aliases)));

        field.NewEntryKey = " ";
        field.NewEntryValue = "Ignored";
        field.AddEntryCommand.Execute(null);

        field.NewEntryKey = "Project";
        field.NewEntryValue = "One";
        field.AddEntryCommand.Execute(null);
        field.NewEntryKey = "Project";
        field.NewEntryValue = "Two";
        field.AddEntryCommand.Execute(null);

        Assert.Single(field.Entries);
        Assert.Equal("Two", naming.Aliases["Project"]);
    }

    [Fact]
    public void Build_WhenOutputFileIsEmpty_ShouldPopulateGuiDefaultValue()
    {
        var output = new OutputConfig();
        var field = Assert.IsType<TextFieldViewModel>(new ConfigurationFormBuilder().Build(output).Single(item => item.Name == nameof(OutputConfig.File)));

        Assert.Equal("dependency-graph-mermaid.md", field.Value);
        Assert.Equal("dependency-graph-mermaid.md", output.File);
    }

    [Fact]
    public void GuiAssembly_ShouldNotDeclareConfigurationModelDuplicates()
    {
        var guiAssembly = typeof(ConfigurationFormBuilder).Assembly;
        var duplicatedModels = guiAssembly.GetTypes()
            .Where(type => type.Namespace != null)
            .Where(type => type.Namespace!.Contains("Config", StringComparison.OrdinalIgnoreCase))
            .Where(type => !type.Name.EndsWith("ViewModel", StringComparison.Ordinal))
            .Where(type => type.Name.EndsWith("Configuration", StringComparison.Ordinal) || type.Name.EndsWith("Config", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(duplicatedModels);
    }

    [Fact]
    public void MainViewModel_ShouldExposeTopLevelConfigurationSectionsAsTabs()
    {
        var viewModel = new MainViewModel(new ConfigurationFormBuilder(), new ConfigurationValidator(), new NoOpClipboardService());

        Assert.Collection(
            viewModel.Sections,
            section =>
            {
                Assert.Equal("General", section.Header);
                Assert.Contains(section.Fields, field => field.Name == nameof(SlnxMermaidConfig.Solution));
            },
            section => Assert.Equal("Diagram", section.Header),
            section => Assert.Equal("Filters", section.Header),
            section => Assert.Equal("Naming", section.Header),
            section => Assert.Equal("Output", section.Header),
            section => Assert.Equal("UI", section.Header));
    }

    [Fact]
    public void MainViewModel_WhenTabbedFieldChanges_ShouldUpdateYamlPreview()
    {
        var viewModel = new MainViewModel(new ConfigurationFormBuilder(), new ConfigurationValidator(), new NoOpClipboardService());
        var outputSection = viewModel.Sections.Single(section => section.Name == nameof(SlnxMermaidConfig.Output));
        var outputFile = Assert.IsType<TextFieldViewModel>(outputSection.Fields.Single(field => field.Name == nameof(OutputConfig.File)));

        outputFile.Value = "docs/new-output.md";

        Assert.Contains("file: docs/new-output.md", viewModel.YamlPreview);
    }

    [Fact]
    public void MainViewModel_WhenValidationHasNoErrors_ShouldHideValidationErrorsBar()
    {
        var validator = new MutableConfigurationValidator();
        var viewModel = new MainViewModel(new ConfigurationFormBuilder(), validator, new NoOpClipboardService());

        Assert.False(viewModel.HasValidationErrors);
        Assert.False(viewModel.IsValidationErrorsExpanded);
        Assert.Equal(string.Empty, viewModel.ValidationErrorsSummary);
    }

    [Fact]
    public void MainViewModel_WhenValidationHasErrors_ShouldShowCollapsedValidationErrorsBarWithSummary()
    {
        var validator = new MutableConfigurationValidator("Missing solution", "Invalid UI mode");
        var viewModel = new MainViewModel(new ConfigurationFormBuilder(), validator, new NoOpClipboardService());

        Assert.True(viewModel.HasValidationErrors);
        Assert.False(viewModel.IsValidationErrorsExpanded);
        Assert.Equal("2 validation errors", viewModel.ValidationErrorsSummary);
        Assert.Equal(new[] { "Missing solution", "Invalid UI mode" }, viewModel.ValidationErrors);
    }

    [Fact]
    public void MainViewModel_WhenErrorsAreResolved_ShouldCollapseAndHideValidationErrorsBar()
    {
        var validator = new MutableConfigurationValidator("Missing solution");
        var viewModel = new MainViewModel(new ConfigurationFormBuilder(), validator, new NoOpClipboardService())
        {
            IsValidationErrorsExpanded = true
        };

        validator.Errors = Array.Empty<string>();
        viewModel.ValidateConfigCommand.Execute(null);

        Assert.False(viewModel.HasValidationErrors);
        Assert.False(viewModel.IsValidationErrorsExpanded);
        Assert.Equal(string.Empty, viewModel.ValidationErrorsSummary);
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

    private sealed class MutableConfigurationValidator(params string[] errors) : IConfigurationValidator
    {
        public IReadOnlyList<string> Errors { get; set; } = errors;

        public ConfigurationValidationResult Validate(SlnxMermaidConfig config, string? baseDirectory = null) => new(Errors);
    }

    private sealed class NoOpClipboardService : IClipboardService
    {
        public Task SetTextAsync(string text) => Task.CompletedTask;
    }
}
