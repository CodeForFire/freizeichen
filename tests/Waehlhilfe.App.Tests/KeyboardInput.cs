using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Waehlhilfe.App.Tests;

internal static class KeyboardInput
{
    // Press and release, as a real key does, then let the dispatcher run what the key started.
    // A key that closes the window (Escape on IsCancel) does so on key-down; the release then has
    // no window left to go to.
    public static void Press(this Window window, PhysicalKey key)
    {
        var closed = false;
        void OnClosed(object? sender, EventArgs e) => closed = true;
        window.Closed += OnClosed;
        window.KeyPressQwerty(key, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        if (!closed)
        {
            window.KeyReleaseQwerty(key, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }

        window.Closed -= OnClosed;
    }

    // Space on a focused button is how a keyboard user presses it; it fires on key-up.
    public static void Click(this Window window, string buttonName)
    {
        var button = window.FindControl<Button>(buttonName);
        Assert.NotNull(button);
        Assert.True(button.Focus(), $"{buttonName} did not take focus.");
        window.Press(PhysicalKey.Space);
    }
}
