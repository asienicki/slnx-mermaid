using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlnxMermaid.Core.Config;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed partial class DictionaryFieldViewModel : FormFieldViewModel
{
    private readonly IDictionary? _sourceDictionary;

    public DictionaryFieldViewModel(string name, string displayName, string description, Type valueType, IDictionary? sourceDictionary, bool usesColorEditor = false)
        : base(name, displayName, description, valueType)
    {
        UsesColorEditor = usesColorEditor;
        _sourceDictionary = sourceDictionary;
        if (usesColorEditor)
        {
            newEntryValue = ColorChoices[0];
            newEntryCustomHexColor = "#000000";
        }

        Entries = new ObservableCollection<DictionaryEntryViewModel>();
        if (sourceDictionary != null)
        {
            foreach (DictionaryEntry entry in sourceDictionary)
                Entries.Add(CreateEntry(entry.Key?.ToString() ?? string.Empty, entry.Value));
        }

        Entries.CollectionChanged += OnCollectionChanged;
        if (UsesColorEditor)
            NewEntryValue = ColorChoices[0];
    }

    public ObservableCollection<DictionaryEntryViewModel> Entries { get; }

    public bool UsesColorEditor { get; }

    public IReadOnlyList<string> ColorChoices { get; } = UiPalette.Names.Concat(new[] { "custom" }).ToArray();

    [ObservableProperty]
    private string? newEntryKey;

    [ObservableProperty]
    private string? newEntryValue;

    [ObservableProperty]
    private string? newEntryCustomHexColor;

    [ObservableProperty]
    private DictionaryEntryViewModel? selectedEntry;

    [RelayCommand]
    private void AddEntry()
    {
        if (string.IsNullOrWhiteSpace(NewEntryKey))
            return;

        var value = GetNewEntryValue();
        var existing = Entries.FirstOrDefault(entry => string.Equals(entry.Key, NewEntryKey, StringComparison.Ordinal));
        if (existing != null)
        {
            existing.Value = value;
        }
        else
        {
            Entries.Add(CreateEntry(NewEntryKey, value));
        }

        NewEntryKey = string.Empty;
        NewEntryValue = UsesColorEditor ? ColorChoices[0] : string.Empty;
        NewEntryCustomHexColor = UsesColorEditor ? "#000000" : string.Empty;
    }

    [RelayCommand]
    private void RemoveSelectedEntry()
    {
        RemoveEntry(SelectedEntry);
        SelectedEntry = null;
    }

    [RelayCommand]
    private void RemoveEntry(DictionaryEntryViewModel? entry)
    {
        if (entry == null)
            return;

        Entries.Remove(entry);
        if (SelectedEntry == entry)
            SelectedEntry = null;
    }

    partial void OnNewEntryValueChanged(string? value)
    {
        if (UsesColorEditor && !string.Equals(value, "custom", StringComparison.OrdinalIgnoreCase))
            NewEntryCustomHexColor = string.Empty;
    }

    private string GetNewEntryValue()
    {
        if (UsesColorEditor && string.Equals(NewEntryValue, "custom", StringComparison.OrdinalIgnoreCase))
            return NewEntryCustomHexColor ?? string.Empty;

        return NewEntryValue ?? string.Empty;
    }

    private DictionaryEntryViewModel CreateEntry(string key, object? value)
    {
        var entry = new DictionaryEntryViewModel(key, value, UsesColorEditor);
        entry.PropertyChanged += (_, _) => SyncToSource();
        return entry;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
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
