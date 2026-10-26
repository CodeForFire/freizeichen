using Waehlhilfe.AppLogic;

namespace Waehlhilfe.App.Platform;

/// <summary>Linux: Wählhilfe dials there (it is where the DL500A is tested), but ships no handler registration.</summary>
internal sealed class UnsupportedTelHandlerRegistration : ITelHandlerRegistration
{
    public bool IsDefaultHandler() => false;

    public Task MakeDefaultAsync(CancellationToken cancellationToken = default) =>
        throw new PlatformNotSupportedException("Wählhilfe registers as the tel: handler on Windows and macOS only.");
}
