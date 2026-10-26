namespace Waehlhilfe.Modem;

/// <summary>A line-oriented connection to the phone's AT command interpreter.</summary>
public interface IAtChannel : IAsyncDisposable
{
    Task WriteLineAsync(string line, CancellationToken cancellationToken = default);

    /// <returns>The next line without its terminator, or <c>null</c> once the link has dropped.</returns>
    Task<string?> ReadLineAsync(CancellationToken cancellationToken = default);
}

public interface IAtChannelFactory
{
    /// <exception cref="PortUnavailableException">The port does not exist or another program holds it.</exception>
    Task<IAtChannel> OpenAsync(string portName, CancellationToken cancellationToken = default);
}
