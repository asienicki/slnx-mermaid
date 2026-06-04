using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed partial class BooleanFieldViewModel : FormFieldViewModel
{
    public BooleanFieldViewModel(string name, string displayName, string description, Type valueType, bool initialValue, object? source = null, PropertyInfo? property = null)
        : base(name, displayName, description, valueType, source, property)
    {
        value = initialValue;
    }

    [ObservableProperty]
    private bool value;

    partial void OnValueChanged(bool value) => SetSourceValue(value);
}
