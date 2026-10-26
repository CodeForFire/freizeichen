using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Freizeichen.App.Views;
using Freizeichen.AppLogic.Tests;
using Freizeichen.AppLogic.ViewModels;
using Freizeichen.Domain;

namespace Freizeichen.App.Tests;

public sealed class SettingsWindowTests
{
    private readonly FakeDialer _dialer = new();
    private readonly FakeRegistration _registration = new();
    private readonly FakeSettingsStore _store = new(DialSettings.Default with { PortName = "COM7" });

    private SettingsWindow Open()
    {
        var window = new SettingsWindow
        {
            DataContext = new SettingsViewModel(_store, _dialer, () => ["COM3", "COM7"], _registration),
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void Settings_window_is_titled_with_the_app_name()
    {
        Assert.Equal("Freizeichen", Open().Title);
    }

    [AvaloniaFact]
    public void Shows_the_stored_port_and_dialling_rules()
    {
        var window = Open();

        Assert.Equal("COM7", window.FindControl<ComboBox>("PortBox")?.SelectedItem);
        Assert.Equal("49", window.FindControl<TextBox>("CountryCodeBox")?.Text);
        Assert.Equal(string.Empty, window.FindControl<TextBox>("PrefixBox")?.Text);
    }

    [AvaloniaFact]
    public void Test_connection_probes_the_phone_without_calling()
    {
        var window = Open();

        window.Click("TestConnectionButton");

        Assert.Equal(["COM7"], _dialer.Probed);
        Assert.Empty(_dialer.Dialled);
    }

    [AvaloniaFact]
    public void Save_stores_what_is_shown()
    {
        var window = Open();
        window.FindControl<TextBox>("PrefixBox")!.Text = "0";

        window.Click("SaveButton");

        Assert.Equal("0", _store.Stored.OutsideLinePrefix);
    }

    [AvaloniaFact]
    public void Says_whether_freizeichen_opens_tel_links()
    {
        var window = Open();
        Assert.True(window.FindControl<TextBlock>("NotDefaultText")?.IsVisible);

        window.Click("MakeDefaultButton");

        Assert.True(window.FindControl<TextBlock>("IsDefaultText")?.IsVisible);
        Assert.False(window.FindControl<TextBlock>("NotDefaultText")?.IsVisible);
    }

    [AvaloniaFact]
    public void Every_button_has_a_name_a_screen_reader_can_say()
    {
        var window = Open();

        foreach (var button in window.GetLogicalDescendants().OfType<Button>())
        {
            Assert.False(
                string.IsNullOrWhiteSpace(button.Content as string) && string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)),
                $"{button.Name} has neither text nor an automation name.");
        }
    }
}
