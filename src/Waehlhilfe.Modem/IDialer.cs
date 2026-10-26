using Waehlhilfe.Domain;

namespace Waehlhilfe.Modem;

public interface IDialer
{
    Task<DialResult> DialAsync(string portName, DialNumber number, CancellationToken cancellationToken = default);

    /// <summary>Checks that the phone answers, without placing a call.</summary>
    Task<DialResult> ProbeAsync(string portName, CancellationToken cancellationToken = default);
}
