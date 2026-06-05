using System.Reflection;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed partial class FilePathFieldViewModel : TextFieldViewModel
{
    public FilePathFieldViewModel(string name, string displayName, string description, Type valueType, string? initialValue, object? source = null, PropertyInfo? property = null)
        : base(name, displayName, description, valueType, initialValue, source, property)
    {
    }
}
