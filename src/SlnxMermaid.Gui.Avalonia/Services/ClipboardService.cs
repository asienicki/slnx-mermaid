using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace SlnxMermaid.Gui.Avalonia.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}

public sealed class ClipboardService : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow })
        {
            var topLevel = TopLevel.GetTopLevel(mainWindow);
            if (topLevel?.Clipboard != null)
                await topLevel.Clipboard.SetTextAsync(text);
        }
    }
}
