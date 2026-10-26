using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Waehlhilfe.AppLogic;

namespace Waehlhilfe.App.Platform;

/// <summary>The bundle declares <c>tel</c> in CFBundleURLTypes; Launch Services picks the default.</summary>
[SupportedOSPlatform("macos")]
internal sealed partial class MacTelHandlerRegistration : ITelHandlerRegistration
{
    /// <summary>Must match CFBundleIdentifier in packaging/macos/build-dmg.sh.</summary>
    internal const string BundleId = "io.github.deepdiver1975.waehlhilfe";

    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CoreServices = "/System/Library/Frameworks/CoreServices.framework/CoreServices";
    private const uint Utf8 = 0x08000100;

    public bool IsDefaultHandler()
    {
        var scheme = CFStringCreateWithCString(IntPtr.Zero, "tel", Utf8);
        try
        {
            var handler = LSCopyDefaultHandlerForURLScheme(scheme);
            try
            {
                return string.Equals(ToManaged(handler), BundleId, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Release(handler);
            }
        }
        finally
        {
            Release(scheme);
        }
    }

    public Task MakeDefaultAsync(CancellationToken cancellationToken = default)
    {
        var scheme = CFStringCreateWithCString(IntPtr.Zero, "tel", Utf8);
        var bundle = CFStringCreateWithCString(IntPtr.Zero, BundleId, Utf8);
        try
        {
            var status = LSSetDefaultHandlerForURLScheme(scheme, bundle);
            return status == 0
                ? Task.CompletedTask
                : throw new InvalidOperationException($"LSSetDefaultHandlerForURLScheme returned {status}.");
        }
        finally
        {
            Release(bundle);
            Release(scheme);
        }
    }

    private static string? ToManaged(IntPtr text)
    {
        if (text == IntPtr.Zero)
        {
            return null;
        }

        var buffer = new byte[256];
        return CFStringGetCString(text, buffer, buffer.Length, Utf8)
            ? Encoding.UTF8.GetString(buffer, 0, Array.IndexOf(buffer, (byte)0) is var end and >= 0 ? end : buffer.Length)
            : null;
    }

    private static void Release(IntPtr reference)
    {
        if (reference != IntPtr.Zero)
        {
            CFRelease(reference);
        }
    }

    [LibraryImport(CoreFoundation, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr CFStringCreateWithCString(IntPtr allocator, string text, uint encoding);

    [LibraryImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool CFStringGetCString(IntPtr text, [Out] byte[] buffer, nint bufferSize, uint encoding);

    [LibraryImport(CoreFoundation)]
    private static partial void CFRelease(IntPtr reference);

    [LibraryImport(CoreServices)]
    private static partial IntPtr LSCopyDefaultHandlerForURLScheme(IntPtr scheme);

    [LibraryImport(CoreServices)]
    private static partial int LSSetDefaultHandlerForURLScheme(IntPtr scheme, IntPtr bundleId);
}
