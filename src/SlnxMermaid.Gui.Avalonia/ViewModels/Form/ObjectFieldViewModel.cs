using System.Collections.ObjectModel;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed class ObjectFieldViewModel : FormFieldViewModel
{
    public ObjectFieldViewModel(string name, string displayName, string description, Type valueType, IEnumerable<FormFieldViewModel> fields)
        : base(name, displayName, description, valueType)
    {
        Fields = new ObservableCollection<FormFieldViewModel>(fields);
        foreach (var field in Fields)
            field.FieldChanged += (_, _) => NotifyFieldChanged();
    }

    public ObservableCollection<FormFieldViewModel> Fields { get; }
}
