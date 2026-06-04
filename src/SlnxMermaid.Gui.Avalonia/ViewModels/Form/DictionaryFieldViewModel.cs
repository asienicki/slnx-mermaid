using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed class DictionaryFieldViewModel : FormFieldViewModel
{
    private readonly IDictionary? _sourceDictionary;

    public DictionaryFieldViewModel(string name, string displayName, string description, Type valueType, IDictionary? sourceDictionary)
        : base(name, displayName, description, valueType)
    {
        _sourceDictionary = sourceDictionary;
        Entries = new ObservableCollection<DictionaryEntryViewModel>();
        if (sourceDictionary != null)
        {
            foreach (DictionaryEntry entry in sourceDictionary)
                Entries.Add(CreateEntry(entry.Key?.ToString() ?? string.Empty, entry.Value));
        }

        Entries.CollectionChanged += OnCollectionChanged;
    }

    public ObservableCollection<DictionaryEntryViewModel> Entries { get; }

    private DictionaryEntryViewModel CreateEntry(string key, object? value)
    {
        var entry = new DictionaryEntryViewModel(key, value);
        entry.PropertyChanged += (_, _) => SyncToSource();
        return entry;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (DictionaryEntryViewModel entry in e.NewItems)
                entry.PropertyChanged += (_, _) => SyncToSource();
        }

        SyncToSource();
    }

    private void SyncToSource()
    {
        if (_sourceDictionary != null)
        {
            _sourceDictionary.Clear();
            foreach (var entry in Entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key))
                    _sourceDictionary[entry.Key] = entry.Value ?? string.Empty;
            }
        }

        NotifyFieldChanged();
    }
}
