using System;
using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using SlnxMermaid.Gui.Avalonia.Services;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public abstract partial class FormFieldViewModel : ObservableObject
{
    private readonly object? _source;
    private readonly PropertyInfo? _property;

    protected FormFieldViewModel(string name, string displayName, string description, Type valueType, object? source = null, PropertyInfo? property = null)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
        ValueType = valueType;
        _source = source;
        _property = property;
    }

    public string Name { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public Type ValueType { get; }

    public event EventHandler? FieldChanged;

    protected void SetSourceValue(object? value)
    {
        if (_source == null || _property == null)
            return;

        _property.SetValue(_source, ValueConversion.ConvertTo(value, _property.PropertyType));
        FieldChanged?.Invoke(this, EventArgs.Empty);
    }

    protected void NotifyFieldChanged() => FieldChanged?.Invoke(this, EventArgs.Empty);
}
