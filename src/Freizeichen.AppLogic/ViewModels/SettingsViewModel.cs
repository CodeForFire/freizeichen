using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Freizeichen.Domain;
using Freizeichen.Modem;

namespace Freizeichen.AppLogic.ViewModels;

/// <summary>What Freizeichen shows when started on its own: the phone's port, the dialling rules, the tests.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _store;
    private readonly IDialer _dialer;
    private readonly Func<IReadOnlyList<string>> _listPorts;
    private readonly ITelHandlerRegistration _registration;

    [ObservableProperty]
    private string? _selectedPort;

    [ObservableProperty]
    private string _homeCountryCode;

    [ObservableProperty]
    private string _outsideLinePrefix;

    [ObservableProperty]
    private string _testNumber = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isDefaultHandler;

    [ObservableProperty]
    private bool _isBusy;

    public SettingsViewModel(ISettingsStore store, IDialer dialer, Func<IReadOnlyList<string>> listPorts, ITelHandlerRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(dialer);
        ArgumentNullException.ThrowIfNull(listPorts);
        ArgumentNullException.ThrowIfNull(registration);
        _store = store;
        _dialer = dialer;
        _listPorts = listPorts;
        _registration = registration;

        var settings = store.Load();
        _homeCountryCode = settings.HomeCountryCode;
        _outsideLinePrefix = settings.OutsideLinePrefix;
        _selectedPort = string.IsNullOrEmpty(settings.PortName) ? null : settings.PortName;
        RefreshPorts();
        _isDefaultHandler = registration.IsDefaultHandler();
    }

    public ObservableCollection<string> Ports { get; } = [];

    [RelayCommand]
    private void RefreshPorts()
    {
        var keep = SelectedPort;
        Ports.Clear();
        foreach (var port in _listPorts())
        {
            Ports.Add(port);
        }

        // A stored port the OS does not list right now (phone switched off) is still shown.
        if (!string.IsNullOrEmpty(keep) && !Ports.Contains(keep))
        {
            Ports.Add(keep);
        }

        SelectedPort = keep;
    }

    [RelayCommand]
    [SuppressMessage("Design", "CA1031", Justification = "A failed write surfaces in Status.")]
    private void Save()
    {
        if (!TryCurrentSettings(out var settings))
        {
            return;
        }

        try
        {
            _store.Save(settings);
            Show("Gespeichert.", isError: false);
        }
        catch (Exception)
        {
            Show("Speichern fehlgeschlagen.", isError: true);
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        var port = SelectedPort ?? string.Empty;
        await RunAsync(port, () => _dialer.ProbeAsync(port));
    }

    [RelayCommand]
    private async Task TestCallAsync()
    {
        if (!TelUri.TryParse("tel:" + TestNumber, out var uri))
        {
            Show("Bitte eine Rufnummer eingeben.", isError: true);
            return;
        }

        if (TryCurrentSettings(out var settings))
        {
            var number = DialNumber.From(uri, settings);
            await RunAsync(settings.PortName, () => _dialer.DialAsync(settings.PortName, number));
        }
    }

    [RelayCommand]
    [SuppressMessage("Design", "CA1031", Justification = "Whatever the OS refuses surfaces in Status.")]
    private async Task MakeDefaultHandlerAsync()
    {
        try
        {
            await _registration.MakeDefaultAsync();
            IsDefaultHandler = _registration.IsDefaultHandler();
            Show(
                IsDefaultHandler
                    ? "Freizeichen öffnet jetzt tel:-Links."
                    : "Im Systemdialog Freizeichen für tel:-Links auswählen und danach „Prüfen“ drücken.",
                isError: false);
        }
        catch (Exception)
        {
            IsDefaultHandler = _registration.IsDefaultHandler();
            Show("Freizeichen konnte nicht als Standard für tel:-Links eingetragen werden.", isError: true);
        }
    }

    [RelayCommand]
    private void RefreshDefaultHandler() => IsDefaultHandler = _registration.IsDefaultHandler();

    [SuppressMessage("Design", "CA1031", Justification = "Any failure of the test surfaces in Status.")]
    private async Task RunAsync(string port, Func<Task<DialResult>> test)
    {
        IsBusy = true;
        Show("Verbinde …", isError: false);
        try
        {
            var result = await test();
            Show(DialMessages.For(result, port), DialMessages.IsError(result));
        }
        catch (Exception)
        {
            Show("Test fehlgeschlagen. Telefon und Bluetooth prüfen.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryCurrentSettings([NotNullWhen(true)] out DialSettings? settings)
    {
        settings = null;
        if (!DialSettings.IsValidHomeCountryCode(HomeCountryCode))
        {
            Show("Ländervorwahl: eine bis drei Ziffern ohne führende 0, z. B. 49.", isError: true);
            return false;
        }

        if (!DialSettings.IsValidOutsideLinePrefix(OutsideLinePrefix))
        {
            Show("Amtsholung: höchstens vier Ziffern, meist leer oder 0.", isError: true);
            return false;
        }

        settings = new DialSettings(SelectedPort ?? string.Empty, HomeCountryCode, OutsideLinePrefix);
        return true;
    }

    private void Show(string status, bool isError)
    {
        Status = status;
        IsError = isError;
    }
}
