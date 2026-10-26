using Freizeichen.App.Platform;
using Freizeichen.AppLogic;
using Freizeichen.AppLogic.ViewModels;
using Freizeichen.Modem;

namespace Freizeichen.App;

/// <summary>Wires Freizeichen by hand: no container.</summary>
internal sealed class CompositionRoot
{
    private readonly JsonSettingsStore _store = new JsonSettingsStore(JsonSettingsStore.DefaultPath());
    private readonly AtDialer _dialer = new AtDialer(new SerialPorts());
    private readonly ITelHandlerRegistration _registration = CreateRegistration();

    public SettingsViewModel Settings() => new(_store, _dialer, SerialPorts.List, _registration);

    // Settings are read for every dial, so a change in the settings window applies to the next click.
    public DialViewModel Dial(string link) => new(link, _store.Load(), _dialer);

    private static ITelHandlerRegistration CreateRegistration()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsTelHandlerRegistration();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacTelHandlerRegistration();
        }

        return new UnsupportedTelHandlerRegistration();
    }
}
