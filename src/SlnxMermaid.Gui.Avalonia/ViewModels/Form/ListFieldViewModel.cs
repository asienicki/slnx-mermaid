using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections;

namespace SlnxMermaid.Gui.Avalonia.ViewModels.Form;

public sealed class ListFieldViewModel : FormFieldViewModel
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
