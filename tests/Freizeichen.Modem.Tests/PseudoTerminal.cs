using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Freizeichen.Modem.Tests;

/// <summary>
/// A Unix pseudo-terminal: a serial device with no modem control lines, like the one
/// <c>tools/fake-dl500a.py</c> plays the phone on. <see cref="Phone"/> is the far end.
/// </summary>
internal sealed partial class PseudoTerminal : IDisposable
{
    private const int ReadWrite = 2;

    private PseudoTerminal(FileStream phone, string port)
    {
        Phone = phone;
        Port = port;
    }

    /// <summary>The device a program opens, as it would open <c>/dev/rfcomm0</c>.</summary>
    public string Port { get; }

    public FileStream Phone { get; }

    public static PseudoTerminal Open()
    {
        var fd = PosixOpenpt(ReadWrite | NoControllingTerminal());
        if (fd < 0 || Grantpt(fd) != 0 || Unlockpt(fd) != 0)
        {
            throw new IOException($"No pseudo-terminal: errno {Marshal.GetLastPInvokeError()}.");
        }

        var name = new byte[128];
        if (PtsnameR(fd, name, name.Length) != 0)
        {
            throw new IOException($"No pseudo-terminal name: errno {Marshal.GetLastPInvokeError()}.");
        }

        var phone = new FileStream(new SafeFileHandle(fd, ownsHandle: true), FileAccess.ReadWrite, bufferSize: 0);
        return new PseudoTerminal(phone, Encoding.ASCII.GetString(name, 0, Array.IndexOf(name, (byte)0)));
    }

    public void Dispose() => Phone.Dispose();

    private static int NoControllingTerminal() => OperatingSystem.IsMacOS() ? 0x20000 : 0x100;

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "posix_openpt", SetLastError = true)]
    private static partial int PosixOpenpt(int flags);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "grantpt", SetLastError = true)]
    private static partial int Grantpt(int fd);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "unlockpt", SetLastError = true)]
    private static partial int Unlockpt(int fd);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "ptsname_r", SetLastError = true)]
    private static partial int PtsnameR(int fd, [Out] byte[] buffer, nint length);
}
