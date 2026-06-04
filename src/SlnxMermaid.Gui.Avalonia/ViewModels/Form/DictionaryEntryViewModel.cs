using CommunityToolkit.Mvvm.ComponentModel;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed partial class DictionaryEntryViewModel : ObservableObject
{
    public DictionaryEntryViewModel(string key, object? value)
    {
        this.key = key;
        this.value = value?.ToString();
    }

    [ObservableProperty]
    private string key;

    [ObservableProperty]
    private string? value;
}
