using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Freizeichen.App.Views;
using Freizeichen.AppLogic.Tests;
using Freizeichen.AppLogic.ViewModels;
using Freizeichen.Domain;
using Freizeichen.Modem;
using Microsoft.Extensions.Time.Testing;

namespace Freizeichen.App.Tests;

// The source of every image in the README (docs/screenshots). `make screenshots` sets RENDER_OUT;
// a plain test run captures nothing and only checks that each window shows the state the README
// describes.
public sealed class ReadmeScreenshotTests
{
    // Fictional: 089 123456-7 is no one's number.
    private const string Link = "tel:+49 89 1234567";

    private readonly FakeDialer _dialer = new();
    private readonly FakeTimeProvider _time = new();

    [AvaloniaFact]
    public void Dial_window_asks_before_it_dials()
    {
        var window = OpenDialWindow();

        Assert.Equal("0891234567", window.FindControl<SelectableTextBlock>("NumberText")?.Text);
        Assert.Empty(_dialer.Dialled);
        Capture(window, "dial.png");
    }

    [AvaloniaFact]
    public void Dial_window_explains_a_phone_that_does_not_answer()
    {
        _dialer.Result = DialResult.NoResponse;
        var window = OpenDialWindow();

        window.Press(PhysicalKey.Enter);

        Assert.Contains("als Audiogerät verbunden", window.FindControl<TextBlock>("StatusText")?.Text, StringComparison.Ordinal);
        Capture(window, "dial-timeout.png");
    }

    [AvaloniaFact]
    public void Settings_window_shows_a_phone_that_answers()
    {
        var store = new FakeSettingsStore(DialSettings.Default with { PortName = "COM7" });
        var registration = new FakeRegistration { IsDefault = true };
        var vm = new SettingsViewModel(store, _dialer, () => ["COM3", "COM7"], registration);
        var window = new SettingsWindow { DataContext = vm };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        _dialer.Result = DialResult.Answered;
        vm.TestConnectionCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["COM7"], _dialer.Probed);
        Assert.True(window.FindControl<TextBlock>("IsDefaultText")?.IsVisible);
        Capture(window, "settings.png");
    }

    private static void Capture(Window window, string name)
    {
        var dir = Environment.GetEnvironmentVariable("RENDER_OUT");
        if (string.IsNullOrWhiteSpace(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(Path.Join(dir, name), new PngBitmapEncoderOptions());
    }

    private DialWindow OpenDialWindow()
    {
        var settings = DialSettings.Default with { PortName = "COM7" };
        var window = new DialWindow { DataContext = new DialViewModel(Link, settings, _dialer, _time) };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        _time.Advance(DialViewModel.ArmDelay);
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
