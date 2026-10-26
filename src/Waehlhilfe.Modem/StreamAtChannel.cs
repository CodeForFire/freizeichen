using System.Text;

namespace Waehlhilfe.Modem;

/// <summary>AT lines over a byte stream: commands end in CR LF, answers end in CR, LF or both.</summary>
public sealed class StreamAtChannel : IAtChannel
{
    private readonly Stream _stream;
    private readonly IDisposable? _owner;
    private readonly StreamReader _reader;

    /// <param name="stream">The connection to the phone.</param>
    /// <param name="owner">What else to dispose with the channel, such as the serial port.</param>
    public StreamAtChannel(Stream stream, IDisposable? owner = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        _owner = owner;
        _reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
    }

    public async Task WriteLineAsync(string line, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(line);

        // Defence in depth behind TelUri: a second line here would be a second AT command.
        if (!line.All(c => c is >= ' ' and <= '~'))
        {
            throw new ArgumentException("An AT command is one line of printable ASCII.", nameof(line));
        }

        var bytes = Encoding.ASCII.GetBytes(line + "\r\n");
        await _stream.WriteAsync(bytes, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }

    // WaitAsync on top: a serial stream does not always honour cancellation of a pending read,
    // and the dialer's timeouts must hold regardless. Disposing the port ends the read.
    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken = default) =>
        await _reader.ReadLineAsync(cancellationToken).AsTask().WaitAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        _reader.Dispose();
        await _stream.DisposeAsync();
        _owner?.Dispose();
    }
}
