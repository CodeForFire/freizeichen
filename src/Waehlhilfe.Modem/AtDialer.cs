using Waehlhilfe.Domain;

namespace Waehlhilfe.Modem;

/// <summary>
/// One dial is one connection: open the port, <c>AT</c>, <c>ATD &lt;number&gt;</c>, close. The
/// DL500A keeps the call up on the base once the link is gone, so nothing is held open.
/// </summary>
public sealed class AtDialer
{
    /// <summary>Opening the port opens the Bluetooth link, which takes seconds.</summary>
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(15);

    /// <summary>How long to wait for <c>CONNECT</c> after <c>OK</c> before closing anyway.</summary>
    public static readonly TimeSpan ConnectGrace = TimeSpan.FromSeconds(5);

    private readonly IAtChannelFactory _channels;
    private readonly TimeProvider _time;

    public AtDialer(IAtChannelFactory channels, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(channels);
        _channels = channels;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Checks that the phone answers, without placing a call.</summary>
    public Task<DialResult> ProbeAsync(string portName, CancellationToken cancellationToken = default) =>
        RunAsync(portName, dial: null, cancellationToken);

    public Task<DialResult> DialAsync(string portName, DialNumber number, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(number);
        return RunAsync(portName, number, cancellationToken);
    }

    private async Task<DialResult> RunAsync(string portName, DialNumber? dial, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            return DialResult.PortNotSet;
        }

        try
        {
            using var connect = new CancellationTokenSource(ConnectTimeout, _time);
            using var connectOrCancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connect.Token);
            await using var channel = await _channels.OpenAsync(portName, connectOrCancel.Token);

            var answer = await CommandAsync(channel, "AT", connectOrCancel.Token);
            if (answer != DialResult.Answered || dial is null)
            {
                return answer;
            }

            using var response = new CancellationTokenSource(ResponseTimeout, _time);
            using var responseOrCancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, response.Token);
            var result = await CommandAsync(channel, "ATD " + dial.Digits, responseOrCancel.Token);
            if (result == DialResult.Answered)
            {
                await AwaitConnectAsync(channel, cancellationToken);
                return DialResult.Dialled;
            }

            return result;
        }
        catch (PortUnavailableException e)
        {
            return e.Failure switch
            {
                PortFailure.Busy => DialResult.PortBusy,
                PortFailure.Unreachable => DialResult.NoResponse,
                _ => DialResult.PortMissing,
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DialResult.NoResponse;
        }
    }

    // Sends one command and reads up to its final result code; OK and CONNECT come back as
    // Answered. The echo, blank lines and unsolicited codes (RING, CLIP:…) are skipped.
    private static async Task<DialResult> CommandAsync(IAtChannel channel, string command, CancellationToken cancellationToken)
    {
        await channel.WriteLineAsync(command, cancellationToken);
        while (await channel.ReadLineAsync(cancellationToken) is { } line)
        {
            if (FinalResult(line.Trim()) is { } result)
            {
                return result;
            }
        }

        return DialResult.NoResponse;
    }

    // After OK the base dials on its own; CONNECT confirms it. Closing earlier would most likely
    // be fine too, but waiting a moment costs nothing and keeps the link until it is confirmed.
    private async Task AwaitConnectAsync(IAtChannel channel, CancellationToken cancellationToken)
    {
        using var grace = new CancellationTokenSource(ConnectGrace, _time);
        using var graceOrCancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, grace.Token);
        try
        {
            while (await channel.ReadLineAsync(graceOrCancel.Token) is { } line)
            {
                if (FinalResult(line.Trim()) is not null)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The grace period ran out: the call was accepted with OK, so it still counts.
        }
    }

    private static DialResult? FinalResult(string line) => line switch
    {
        "OK" => DialResult.Answered,
        "ERROR" => DialResult.Rejected,
        "BUSY" => DialResult.LineBusy,
        "NO CARRIER" or "NO ANSWER" => DialResult.NoCarrier,
        "NO DIALTONE" => DialResult.NoDialTone,
        _ when line.StartsWith("CONNECT", StringComparison.Ordinal) => DialResult.Answered,
        _ when line.StartsWith("+CME ERROR", StringComparison.Ordinal) => DialResult.Rejected,
        _ => null,
    };
}
