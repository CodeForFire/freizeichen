using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Waehlhilfe.App.Views;

namespace Waehlhilfe.App;

internal sealed partial class WaehlhilfeApp : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new SettingsWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
