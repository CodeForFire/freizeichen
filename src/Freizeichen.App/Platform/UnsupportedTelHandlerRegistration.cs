using Freizeichen.AppLogic;

namespace Freizeichen.App.Platform;

/// <summary>Any other OS .NET runs on (FreeBSD): Freizeichen dials there, but registers no handler.</summary>
internal sealed class UnsupportedTelHandlerRegistration : ITelHandlerRegistration
{
    public bool IsDefaultHandler() => false;

    public Task MakeDefaultAsync(CancellationToken cancellationToken = default) =>
        throw new PlatformNotSupportedException("Freizeichen registers as the tel: handler on Windows, macOS and Linux only.");
}
