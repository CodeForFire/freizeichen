using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Microsoft.Extensions.Time.Testing;
using Waehlhilfe.App.Views;
using Waehlhilfe.AppLogic.Tests;
using Waehlhilfe.AppLogic.ViewModels;
using Waehlhilfe.Domain;

namespace Waehlhilfe.App.Tests;

public sealed class DialWindowTests
{
    private readonly FakeDialer _dialer = new();
    private readonly FakeTimeProvider _time = new();

    private DialWindow Open(string link = "tel:+49 89 1234567", bool armed = true)
    {
        var settings = DialSettings.Default with { PortName = "COM7" };
        var window = new DialWindow { DataContext = new DialViewModel(link, settings, _dialer, _time) };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        if (armed)
        {
            _time.Advance(DialViewModel.ArmDelay);
            Dispatcher.UIThread.RunJobs();
        }

        return window;
    }

    [AvaloniaFact]
    public void Shows_the_number_that_will_be_dialled()
    {
        var window = Open();

        Assert.Equal("0891234567", window.FindControl<TextBlock>("NumberText")?.Text);
        Assert.Empty(_dialer.Dialled);
    }

    [AvaloniaFact]
    public void Enter_dials()
    {
        var window = Open();

        window.Press(PhysicalKey.Enter);

        Assert.Equal([("COM7", "0891234567")], _dialer.Dialled);
    }

    // The window takes focus when a link arrives; an Enter that was meant for the program the
    // Lagebuchführer was typing in must not place the call.
    [AvaloniaFact]
    public void Enter_right_after_the_window_opens_does_not_dial()
    {
        var window = Open(armed: false);

        window.Press(PhysicalKey.Enter);

        Assert.Empty(_dialer.Dialled);
        Assert.False(window.FindControl<Button>("DialButton")?.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void Escape_closes_without_dialling()
    {
        var window = Open();

        window.Press(PhysicalKey.Escape);

        Assert.False(window.IsVisible);
        Assert.Empty(_dialer.Dialled);
    }

    [AvaloniaFact]
    public void Closes_itself_after_a_successful_dial()
    {
        var window = Open();

        window.Press(PhysicalKey.Enter);
        _time.Advance(DialViewModel.CloseDelay);
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.IsVisible);
    }

    // Closing the window — title bar, or a newer link replacing it — must not leave a dial running
    // that nobody can see any more.
    [AvaloniaFact]
    public void Closing_the_window_during_a_dial_cancels_it()
    {
        _dialer.Hold();
        var window = Open();
        window.Press(PhysicalKey.Enter);

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.True(_dialer.LastToken.IsCancellationRequested);
    }

    [AvaloniaFact]
    public void A_refused_link_cannot_be_dialled_with_enter()
    {
        var window = Open("tel:*21*015112345678%23");

        window.Press(PhysicalKey.Enter);

        Assert.Empty(_dialer.Dialled);
        Assert.False(window.FindControl<Button>("DialButton")?.IsEffectivelyEnabled);
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
