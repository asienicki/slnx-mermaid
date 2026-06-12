using System.Collections.ObjectModel;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed class ConfigurationSectionViewModel
{
    public ConfigurationSectionViewModel(string name, string header, string description, IEnumerable<FormFieldViewModel> fields)
    {
        Name = name;
        Header = header;
        Description = description;
        Fields = new ObservableCollection<FormFieldViewModel>(fields);
    }

    public string Name { get; }

    public string Header { get; }

    public string Description { get; }

    public ObservableCollection<FormFieldViewModel> Fields { get; }
}
