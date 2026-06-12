using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlnxMermaid.Core.Config;
using SlnxMermaid.Gui.Avalonia.Services;
using SlnxMermaid.Gui.Avalonia.ViewModels.Form;

namespace SlnxMermaid.Gui.Avalonia.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IConfigurationFormBuilder _formBuilder;
    private readonly IConfigurationValidator _validator;
    private readonly IClipboardService _clipboardService;

    public MainViewModel(IConfigurationFormBuilder formBuilder, IConfigurationValidator validator, IClipboardService clipboardService)
    {
        _formBuilder = formBuilder;
        _validator = validator;
        _clipboardService = clipboardService;
        Configuration = new SlnxMermaidConfig();
        RebuildForm();
        ValidateAndPreview();
    }

    public SlnxMermaidConfig Configuration { get; private set; }

    public ObservableCollection<FormFieldViewModel> Fields { get; } = new();

    public ObservableCollection<ConfigurationSectionViewModel> Sections { get; } = new();

    public ObservableCollection<FormFieldViewModel> PrimaryFields { get; } = new();

    public ObservableCollection<FormFieldViewModel> LeftColumnFields { get; } = new();

    public ObservableCollection<FormFieldViewModel> RightColumnFields { get; } = new();

    public ObservableCollection<string> ValidationErrors { get; } = new();

    [ObservableProperty]
    private string? yamlPreview;

    [ObservableProperty]
    private string validationStatus = string.Empty;

    [ObservableProperty]
    private bool hasValidationErrors;

    [ObservableProperty]
    private bool isValidationErrorsExpanded;

    [ObservableProperty]
    private string validationErrorsSummary = string.Empty;

    [ObservableProperty]
    private string configPath = "slnx-mermaid.yml";

    [RelayCommand]
    private void LoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            ValidationErrors.Clear();
            ValidationErrors.Add($"File not found: {ConfigPath}");
            ValidationStatus = "Load failed";
            UpdateValidationErrorsBar();
            return;
        }

        Configuration = YamlConfigLoader.Load(ConfigPath);
        RebuildForm();
        ValidateAndPreview();
    }

    [RelayCommand]
    private void SaveConfig()
    {
        File.WriteAllText(ConfigPath, Configuration.ToYaml());
        ValidateAndPreview();
    }

    [RelayCommand]
    private void ValidateConfig() => ValidateAndPreview();

    [RelayCommand]
    private void GenerateYamlPreview() => YamlPreview = Configuration.ToYaml();

    [RelayCommand]
    private async Task CopyYaml()
    {
        YamlPreview = Configuration.ToYaml();
        await _clipboardService.SetTextAsync(YamlPreview ?? string.Empty);
    }

    private void RebuildForm()
    {
        Fields.Clear();
        PrimaryFields.Clear();
        LeftColumnFields.Clear();
        RightColumnFields.Clear();
        Sections.Clear();

        var generalFields = new List<FormFieldViewModel>();

        foreach (var field in _formBuilder.Build(Configuration))
        {
            field.FieldChanged += (_, _) => ValidateAndPreview();
            Fields.Add(field);

            if (field is ObjectFieldViewModel objectField)
            {
                Sections.Add(new ConfigurationSectionViewModel(
                    objectField.Name,
                    GetSectionHeader(objectField),
                    objectField.Description,
                    objectField.Fields));
            }
            else
            {
                generalFields.Add(field);
            }

            if (field.Name == nameof(SlnxMermaidConfig.Solution))
                PrimaryFields.Add(field);
            else if (field.Name == nameof(SlnxMermaidConfig.Diagram) || field.Name == nameof(SlnxMermaidConfig.Filters))
                LeftColumnFields.Add(field);
            else
                RightColumnFields.Add(field);
        }

        if (generalFields.Count > 0)
        {
            Sections.Insert(0, new ConfigurationSectionViewModel(
                "General",
                "General",
                "Core configuration file settings.",
                generalFields));
        }
    }

    private void ValidateAndPreview()
    {
        YamlPreview = Configuration.ToYaml();
        ValidationErrors.Clear();
        var validationBaseDirectory = ResolveConfigBaseDirectory(ConfigPath);
        var result = _validator.Validate(Configuration, validationBaseDirectory);
        foreach (var error in result.Errors)
            ValidationErrors.Add(error);

        ValidationStatus = result.IsValid ? "Valid" : $"Invalid ({result.Errors.Count})";
        UpdateValidationErrorsBar();
    }

    private void UpdateValidationErrorsBar()
    {
        HasValidationErrors = ValidationErrors.Count > 0;
        if (!HasValidationErrors)
            IsValidationErrorsExpanded = false;

        ValidationErrorsSummary = ValidationErrors.Count switch
        {
            0 => string.Empty,
            1 => "1 validation error",
            _ => $"{ValidationErrors.Count} validation errors"
        };
    }

    private static string GetSectionHeader(ObjectFieldViewModel field) =>
        string.Equals(field.Name, nameof(SlnxMermaidConfig.Ui), StringComparison.Ordinal)
            ? "UI"
            : field.DisplayName;

    private static string ResolveConfigBaseDirectory(string? configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            return Directory.GetCurrentDirectory();

        return Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? Directory.GetCurrentDirectory();
    }
}
