using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed partial class ListFieldViewModel : FormFieldViewModel
{
    private readonly IList? _sourceList;

    public ListFieldViewModel(string name, string displayName, string description, Type valueType, IList? sourceList)
        : base(name, displayName, description, valueType)
    {
        _sourceList = sourceList;
        Items = new ObservableCollection<object?>();
        if (sourceList != null)
        {
            foreach (var item in sourceList)
                Items.Add(item);
        }

        Items.CollectionChanged += OnCollectionChanged;
    }

    public ObservableCollection<object?> Items { get; }

    [ObservableProperty]
    private string? newItemValue;

    [ObservableProperty]
    private object? selectedItem;

    [RelayCommand]
    private void AddItem()
    {
        Items.Add(NewItemValue ?? string.Empty);
        NewItemValue = string.Empty;
    }

    [RelayCommand]
    private void RemoveSelectedItem()
    {
        RemoveItem(SelectedItem);
        SelectedItem = null;
    }

    [RelayCommand]
    private void RemoveItem(object? item)
    {
        if (item == null)
            return;

        Items.Remove(item);
        if (Equals(SelectedItem, item))
            SelectedItem = null;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_sourceList != null)
        {
            _sourceList.Clear();
            foreach (var item in Items)
                _sourceList.Add(item);
        }

        NotifyFieldChanged();
    }
}
