using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SlnxMermaid.Gui.Avalonia.ViewModels.Form;

namespace SlnxMermaid.Gui.Avalonia.Views;

public partial class FilePathFieldView : UserControl
{
    public FilePathFieldView() => InitializeComponent();

    private async void ChooseFile(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not FilePathFieldViewModel viewModel)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select solution file",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Solution files") { Patterns = new[] { "*.sln", "*.slnx" } },
                FilePickerFileTypes.All
            }
        });

        var selected = files.FirstOrDefault();
        if (selected != null)
            viewModel.Value = selected.TryGetLocalPath() ?? selected.Path.LocalPath;
    }
}
