using System.Threading.Channels;

namespace Freizeichen.Modem.Tests;

/// <summary>
/// A phone that answers each command with scripted lines. A command without a script stays
/// silent, which is what the DL500A does while the PC holds its audio connection.
/// </summary>
internal sealed class ScriptedAtChannel : IAtChannel
{
    private readonly Dictionary<string, string[]> _script = new(StringComparer.Ordinal);
    private readonly Channel<string> _incoming = Channel.CreateUnbounded<string>();

    public List<string> Written { get; } = [];

    public bool IsDisposed { get; private set; }

    public ScriptedAtChannel Answer(string command, params string[] lines)
    {
        _script[command] = lines;
        return this;
    }

    // The DL500A echoes every command before answering it.
    public static ScriptedAtChannel Dl500a() => new ScriptedAtChannel()
        .Answer("AT", "AT", "OK")
        .Answer("ATD 0891234567", "ATD 0891234567", "OK", string.Empty, "CLIP:EXTERN", string.Empty, "CONNECT");

    public Task WriteLineAsync(string line, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (WriteFailure is not null)
        {
            throw WriteFailure;
        }

        Written.Add(line);
        if (_script.TryGetValue(line, out var lines))
        {
            foreach (var answer in lines)
            {
                _incoming.Writer.TryWrite(answer);
            }
        }

        return Task.CompletedTask;
    }

    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return await _incoming.Reader.WaitToReadAsync(cancellationToken) && _incoming.Reader.TryRead(out var line)
            ? line
            : null;
    }

    public void Hangup() => _incoming.Writer.TryComplete();

    /// <summary>The Bluetooth link breaks: every later read throws, as a serial stream does.</summary>
    public void Break(Exception error) => _incoming.Writer.TryComplete(error);

    /// <summary>The link is already gone when the next command is written.</summary>
    public Exception? WriteFailure { get; set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

internal sealed class FakeChannelFactory : IAtChannelFactory
{
    private readonly Func<IAtChannel> _open;

    public FakeChannelFactory(IAtChannel channel) => _open = () => channel;

    public FakeChannelFactory(Exception failure) => _open = () => throw failure;

    public List<string> Opened { get; } = [];

    public Task<IAtChannel> OpenAsync(string portName, CancellationToken cancellationToken = default)
    {
        Opened.Add(portName);
        return Task.FromResult(_open());
    }
}
