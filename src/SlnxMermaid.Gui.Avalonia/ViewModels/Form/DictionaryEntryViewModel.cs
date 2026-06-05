using System.Collections;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed partial class DictionaryEntryViewModel : ObservableObject
{
    private static readonly Regex HexColorPattern = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);
    private bool _updatingColorFields;

    public DictionaryEntryViewModel(string key, object? value, bool usesColorEditor = false)
    {
        UsesColorEditor = usesColorEditor;
        this.key = key;
        this.value = NormalizeValue(value);

        if (usesColorEditor)
        {
            if (IsHexColor(this.value))
            {
                selectedColor = "custom";
                customHexColor = this.value;
            }
            else
            {
                selectedColor = this.value;
                customHexColor = string.Empty;
            }
        }
    }

    public bool UsesColorEditor { get; }

    [ObservableProperty]
    private string key;

    [ObservableProperty]
    private string? value;

    [ObservableProperty]
    private string? selectedColor;

    [ObservableProperty]
    private string? customHexColor;

    partial void OnValueChanged(string? value)
    {
        if (!UsesColorEditor || _updatingColorFields)
            return;

        _updatingColorFields = true;
        if (IsHexColor(value))
        {
            SelectedColor = "custom";
            CustomHexColor = value;
        }
        else
        {
            SelectedColor = value;
            CustomHexColor = string.Empty;
        }
        _updatingColorFields = false;
    }

    partial void OnSelectedColorChanged(string? value)
    {
        if (!UsesColorEditor || _updatingColorFields)
            return;

        _updatingColorFields = true;
        Value = string.Equals(value, "custom", StringComparison.OrdinalIgnoreCase)
            ? CustomHexColor
            : value;
        _updatingColorFields = false;
    }

    partial void OnCustomHexColorChanged(string? value)
    {
        if (!UsesColorEditor || _updatingColorFields || !string.Equals(SelectedColor, "custom", StringComparison.OrdinalIgnoreCase))
            return;

        _updatingColorFields = true;
        Value = value;
        _updatingColorFields = false;
    }

    private static string? NormalizeValue(object? value)
    {
        if (value is IDictionary dictionary)
        {
            if (dictionary.Contains("fill") && dictionary["fill"] is object fill)
                return fill.ToString();
        }

        return value?.ToString();
    }

    private static bool IsHexColor(string? value) => value != null && HexColorPattern.IsMatch(value);
}
