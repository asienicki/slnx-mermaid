using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed partial class TextFieldViewModel : FormFieldViewModel
{
    public TextFieldViewModel(string name, string displayName, string description, Type valueType, string? initialValue, object? source = null, PropertyInfo? property = null)
        : base(name, displayName, description, valueType, source, property)
    {
        value = initialValue;
    }

    [ObservableProperty]
    private string? value;

    partial void OnValueChanged(string? value) => SetSourceValue(value);
}
