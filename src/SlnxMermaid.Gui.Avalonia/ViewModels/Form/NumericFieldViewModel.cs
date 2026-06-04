using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed partial class NumericFieldViewModel : FormFieldViewModel
{
    public NumericFieldViewModel(string name, string displayName, string description, Type valueType, string? initialValue, object? source = null, PropertyInfo? property = null)
        : base(name, displayName, description, valueType, source, property)
    {
        value = initialValue;
    }

    [ObservableProperty]
    private string? value;

    [ObservableProperty]
    private string? validationMessage;

    partial void OnValueChanged(string? value)
    {
        if (ValueConversion.TryConvertTo(value, ValueType, out var converted, out var error))
        {
            ValidationMessage = null;
            SetSourceValue(converted);
        }
        else
        {
            ValidationMessage = error;
            NotifyFieldChanged();
        }
    }
}
