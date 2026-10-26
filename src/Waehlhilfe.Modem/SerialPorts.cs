using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Ports;

namespace Waehlhilfe.Modem;

/// <summary>
/// The phone's Bluetooth SPP service as the OS exposes it: an outgoing COM port on Windows,
/// <c>/dev/cu.*</c> on macOS, <c>/dev/rfcomm*</c> (after <c>rfcomm bind</c>) on Linux. Opening
/// the port is what makes the OS bring up the RFCOMM link.
/// </summary>
public sealed class SerialPorts : IAtChannelFactory
{
    private const int BaudRate = 115200;

    public static IReadOnlyList<string> List()
    {
        IEnumerable<string> names = SerialPort.GetPortNames();
        if (OperatingSystem.IsMacOS())
        {
            names = names.Concat(Directory.EnumerateFiles("/dev", "cu.*"));
        }
        else if (OperatingSystem.IsLinux())
        {
            names = names.Concat(Directory.EnumerateFiles("/dev", "rfcomm*"));
        }

        return Filter(names, OperatingSystem.IsMacOS());
    }

    internal static IReadOnlyList<string> Filter(IEnumerable<string> names, bool isMacOS)
    {
        var ports = names.Distinct(StringComparer.Ordinal);
        if (isMacOS)
        {
            // The callout device (cu.*) opens without waiting for carrier detect; the tty.* twin
            // blocks. The two built-in ports are never a phone.
            ports = ports.Where(p => p.StartsWith("/dev/cu.", StringComparison.Ordinal)
                && !p.EndsWith("Bluetooth-Incoming-Port", StringComparison.Ordinal)
                && !p.EndsWith("debug-console", StringComparison.Ordinal));
        }

        return [.. ports
            .OrderBy(p => p.Contains("rfcomm", StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(p => p.TrimEnd("0123456789".ToCharArray()), StringComparer.Ordinal)
            .ThenBy(TrailingNumber)];
    }

    /// <returns>Why the port did not open, or <c>null</c> for an exception that is not about the port.</returns>
    internal static PortFailure? Classify(Exception error) => error switch
    {
        FileNotFoundException or ArgumentException => PortFailure.Missing,
        UnauthorizedAccessException => PortFailure.Busy,
        IOException or TimeoutException => PortFailure.Unreachable,
        _ => null,
    };

    [SuppressMessage("Reliability", "CA2000", Justification = "Ownership of the port passes to the returned channel, or it is disposed here when opening fails.")]
    public async Task<IAtChannel> OpenAsync(string portName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        var port = new SerialPort(portName, BaudRate)
        {
            DtrEnable = true,
            RtsEnable = true,
            WriteTimeout = (int)AtDialer.ConnectTimeout.TotalMilliseconds,
        };

        // Open blocks while the OS sets up the Bluetooth link, so it runs off the caller's
        // thread. If the caller gives up first, the port is closed once Open has returned.
        var open = Task.Run(port.Open, CancellationToken.None);
        try
        {
            await open.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _ = open.ContinueWith(_ => port.Dispose(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            throw;
        }
        catch (Exception e) when (Classify(e) is { } failure)
        {
            port.Dispose();
            throw new PortUnavailableException(portName, failure, e);
        }

        return new StreamAtChannel(port.BaseStream, port);
    }

    private static int TrailingNumber(string name)
    {
        var digits = name[name.TrimEnd("0123456789".ToCharArray()).Length..];
        return digits.Length == 0 ? 0 : int.Parse(digits, CultureInfo.InvariantCulture);
    }
}
