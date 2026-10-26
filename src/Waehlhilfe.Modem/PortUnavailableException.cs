namespace Waehlhilfe.Modem;

public enum PortFailure
{
    /// <summary>No such port: the phone is not paired, or the port name is stale.</summary>
    Missing,

    /// <summary>Another program, or another dial still in progress, holds the port.</summary>
    Busy,

    /// <summary>The port exists but the link behind it did not come up: the phone is off or out of range.</summary>
    Unreachable,
}

public sealed class PortUnavailableException : Exception
{
    public PortUnavailableException()
    {
    }

    public PortUnavailableException(string message)
        : base(message)
    {
    }

    public PortUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public PortUnavailableException(string portName, PortFailure failure, Exception? innerException = null)
        : base($"Port {portName} is unavailable: {failure}.", innerException)
    {
        PortName = portName;
        Failure = failure;
    }

    public string PortName { get; } = string.Empty;

    public PortFailure Failure { get; }
}
