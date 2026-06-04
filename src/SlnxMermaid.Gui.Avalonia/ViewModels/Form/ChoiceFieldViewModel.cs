using System.Collections.Generic;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed partial class ChoiceFieldViewModel : FormFieldViewModel
{
    public ChoiceFieldViewModel(string name, string displayName, string description, Type valueType, IReadOnlyList<string> values, string? initialValue, object? source = null, PropertyInfo? property = null)
        : base(name, displayName, description, valueType, source, property)
    {
        Values = values;
        selectedValue = string.IsNullOrWhiteSpace(initialValue) && values.Count > 0 ? values[0] : initialValue;

        if (string.IsNullOrWhiteSpace(initialValue) && selectedValue != null)
            SetSourceValue(selectedValue);
    }

    public IReadOnlyList<string> Values { get; }

    [ObservableProperty]
    private string? selectedValue;

    partial void OnSelectedValueChanged(string? value) => SetSourceValue(value);
}
