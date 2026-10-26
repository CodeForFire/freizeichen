using Avalonia.Headless.XUnit;
using Waehlhilfe.App.Views;

namespace Waehlhilfe.App.Tests;

public sealed class SettingsWindowTests
{
    [AvaloniaFact]
    public void Settings_window_is_titled_with_the_app_name()
    {
        var window = new SettingsWindow();

        window.Show();

        Assert.Equal("Wählhilfe", window.Title);
    }
}
