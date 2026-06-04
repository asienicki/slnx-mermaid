using System.Collections.Generic;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed partial class EnumFieldViewModel : FormFieldViewModel
{
    public EnumFieldViewModel(string name, string displayName, string description, Type valueType, IReadOnlyList<object> values, object? initialValue, object? source = null, PropertyInfo? property = null)
        : base(name, displayName, description, valueType, source, property)
    {
        Values = values;
        selectedValue = initialValue;
    }

    public IReadOnlyList<object> Values { get; }

    [ObservableProperty]
    private object? selectedValue;

    partial void OnSelectedValueChanged(object? value) => SetSourceValue(value);
}
