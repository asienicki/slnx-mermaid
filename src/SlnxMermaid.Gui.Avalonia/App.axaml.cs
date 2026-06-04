using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SlnxMermaid.Core.Config;
using SlnxMermaid.Gui.Avalonia.Services;
using SlnxMermaid.Gui.Avalonia.ViewModels;
using SlnxMermaid.Gui.Avalonia.Views;

namespace SlnxMermaid.Gui.Avalonia;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection()
            .AddSingleton<IConfigurationFormBuilder, ConfigurationFormBuilder>()
            .AddSingleton<IConfigurationValidator, ConfigurationValidator>()
            .AddSingleton<IClipboardService, ClipboardService>()
            .AddTransient<MainViewModel>()
            .BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
