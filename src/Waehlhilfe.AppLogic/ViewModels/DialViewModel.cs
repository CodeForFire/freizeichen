using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Waehlhilfe.Domain;
using Waehlhilfe.Modem;

namespace Waehlhilfe.AppLogic.ViewModels;

/// <summary>
/// The confirm window a <c>tel:</c> click opens. Nothing is dialled until the Lagebuchführer
/// confirms: a link can come from any page or document, and a call is not something a page may
/// place on its own.
/// </summary>
public sealed partial class DialViewModel : ObservableObject
{
    /// <summary>Long enough to read „Gewählt“, short enough not to stand in the way.</summary>
    public static readonly TimeSpan CloseDelay = TimeSpan.FromSeconds(2);

    private readonly IDialer _dialer;
    private readonly TimeProvider _time;
    private readonly string _portName;
    private readonly DialNumber? _number;
    private CancellationTokenSource? _dialing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DialCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _status;

    [ObservableProperty]
    private bool _isError;

    public DialViewModel(string? link, DialSettings settings, IDialer dialer, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dialer);
        _dialer = dialer;
        _time = time ?? TimeProvider.System;
        _portName = settings.PortName;

        if (TelUri.TryParse(link, out var uri))
        {
            _number = DialNumber.From(uri, settings);
        }

        Number = _number?.Digits ?? string.Empty;
        (_status, _isError) = _number is null
            ? ("Kein gültiger tel:-Link – es wird nicht gewählt.", true)
            : string.IsNullOrWhiteSpace(_portName)
                ? (DialMessages.For(DialResult.PortNotSet, _portName), true)
                : ("Über das Telefon wählen?", false);
    }

    public event EventHandler? CloseRequested;

    /// <summary>The digits that will follow <c>ATD</c>; empty for a link that was refused.</summary>
    public string Number { get; }

    public bool CanDial => _number is not null && !string.IsNullOrWhiteSpace(_portName);

    private bool CanStartDial() => CanDial && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanStartDial))]
    [SuppressMessage("Design", "CA1031", Justification = "Any failure of the dial surfaces in Status; the window stays open to show it.")]
    private async Task DialAsync()
    {
        if (!CanStartDial() || _number is null)
        {
            return;
        }

        using var dialing = new CancellationTokenSource();
        _dialing = dialing;
        IsBusy = true;
        IsError = false;
        Status = "Wähle …";
        try
        {
            var result = await _dialer.DialAsync(_portName, _number, dialing.Token);
            Status = DialMessages.For(result, _portName);
            IsError = DialMessages.IsError(result);
            if (!IsError)
            {
                _ = CloseAfterDelayAsync();
            }
        }
        catch (OperationCanceledException)
        {
            Status = "Abgebrochen.";
        }
        catch (Exception)
        {
            Status = "Wählen fehlgeschlagen. Telefon und Bluetooth prüfen.";
            IsError = true;
        }
        finally
        {
            _dialing = null;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _dialing?.Cancel();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task CloseAfterDelayAsync()
    {
        await Task.Delay(CloseDelay, _time);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
