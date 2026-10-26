using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Freizeichen.App.Views;

namespace Freizeichen.App;

internal sealed partial class FreizeichenApp : Application
{
    // How long macOS gets to deliver the link that launched the app before the settings open.
    private static readonly TimeSpan LaunchLinkGrace = TimeSpan.FromMilliseconds(500);

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var root = new CompositionRoot();
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

            // Subscribed before anything awaits, so a link that launched the app is not missed.
            // One dial window at a time: a second link replaces the first instead of stacking
            // windows a stray keystroke could confirm.
            var linkArrived = false;
            DialWindow? dialWindow = null;
            if (this.TryGetFeature<IActivatableLifetime>() is { } activation)
            {
                activation.Activated += (_, e) =>
                {
                    if (e is ProtocolActivatedEventArgs { Kind: ActivationKind.OpenUri } link)
                    {
                        linkArrived = true;
                        var previous = dialWindow;
                        dialWindow = new DialWindow { DataContext = root.Dial(link.Uri.OriginalString) };
                        dialWindow.Show();
                        previous?.Close();
                    }
                };
            }

            if (LaunchArguments.TelLink(desktop.Args ?? []) is { } telLink)
            {
                desktop.MainWindow = dialWindow = new DialWindow { DataContext = root.Dial(telLink) };
            }
            else if (OperatingSystem.IsMacOS())
            {
                // A tel: click starts the app with no arguments and the link follows as an Apple
                // event; only when none comes was the app opened by hand.
                DispatcherTimer.RunOnce(
                    () =>
                    {
                        if (!linkArrived)
                        {
                            new SettingsWindow { DataContext = root.Settings() }.Show();
                        }
                    },
                    LaunchLinkGrace);
            }
            else
            {
                desktop.MainWindow = new SettingsWindow { DataContext = root.Settings() };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
