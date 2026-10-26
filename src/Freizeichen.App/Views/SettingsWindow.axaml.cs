using Avalonia.Controls;

namespace Freizeichen.App.Views;

internal sealed partial class SettingsWindow : Window
{
    public SettingsWindow() => InitializeComponent();

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        WindowsTitleBar.ApplyDarkMode(this);
    }
}
