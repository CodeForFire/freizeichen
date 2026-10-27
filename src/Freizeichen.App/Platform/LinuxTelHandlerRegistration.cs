using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Freizeichen.AppLogic;

namespace Freizeichen.App.Platform;

/// <summary>
/// Linux has no installer to declare the handler, so Freizeichen writes its own desktop entry to
/// the user's applications directory and asks <c>xdg-mime</c> (xdg-utils, present on every
/// desktop) to make it the default for <c>x-scheme-handler/tel</c>. The desktop opens the link
/// by running the entry's <c>Exec</c> line with the link as the argument, which is how
/// <see cref="LaunchArguments"/> receives it.
/// </summary>
internal sealed class LinuxTelHandlerRegistration : ITelHandlerRegistration
{
    internal const string EntryName = "freizeichen.desktop";

    private const string MimeType = "x-scheme-handler/tel";
    private static readonly TimeSpan XdgMimeTimeout = TimeSpan.FromSeconds(5);

    private readonly string _applicationsDirectory;
    private readonly string? _executablePath;
    private readonly Func<IReadOnlyList<string>, string?> _xdgMime;

    public LinuxTelHandlerRegistration()
        : this(DefaultApplicationsDirectory(), Environment.ProcessPath, RunXdgMime)
    {
    }

    /// <param name="applicationsDirectory">Where the desktop entry goes, normally <c>~/.local/share/applications</c>.</param>
    /// <param name="executablePath">The program the entry runs; <c>null</c> when the process cannot tell.</param>
    /// <param name="xdgMime">Runs <c>xdg-mime</c> with the arguments and returns its output, or <c>null</c> when it failed.</param>
    internal LinuxTelHandlerRegistration(string applicationsDirectory, string? executablePath, Func<IReadOnlyList<string>, string?> xdgMime)
    {
        _applicationsDirectory = applicationsDirectory;
        _executablePath = executablePath;
        _xdgMime = xdgMime;
    }

    public bool IsDefaultHandler() =>
        string.Equals(_xdgMime(["query", "default", MimeType])?.Trim(), EntryName, StringComparison.Ordinal);

    public async Task MakeDefaultAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_executablePath) || _executablePath.AsSpan().IndexOfAny('\r', '\n') >= 0)
        {
            throw new InvalidOperationException("The executable path cannot be written to a desktop entry.");
        }

        Directory.CreateDirectory(_applicationsDirectory);
        await File.WriteAllTextAsync(Path.Join(_applicationsDirectory, EntryName), DesktopEntry(_executablePath), cancellationToken);

        if (_xdgMime(["default", EntryName, MimeType]) is null)
        {
            throw new InvalidOperationException("xdg-mime could not make Freizeichen the tel: handler.");
        }
    }

    private static string DesktopEntry(string executablePath) =>
        new StringBuilder()
            .Append("[Desktop Entry]\n")
            .Append("Type=Application\n")
            .Append("Name=Freizeichen\n")
            .Append("Comment=Wählt tel:-Links über ein Gigaset DL500A per Bluetooth\n")
            .Append("Exec=").Append(QuoteExecArgument(executablePath)).Append(" %u\n")
            .Append("Terminal=false\n")
            .Append("Categories=Network;Telephony;\n")
            .Append("MimeType=").Append(MimeType).Append(";\n")
            .ToString();

    // The Desktop Entry spec quotes an Exec argument in double quotes and backslash-escapes ", `, $
    // and \ inside them; the value as a whole is then a string, whose own escaping doubles every
    // backslash. A literal % is written %% so it is not read as a field code.
    private static string QuoteExecArgument(string argument)
    {
        var quoted = new StringBuilder("\"");
        foreach (var c in argument)
        {
            switch (c)
            {
                case '"' or '`' or '$' or '\\':
                    quoted.Append(@"\\").Append(c == '\\' ? @"\\" : c.ToString());
                    break;
                case '%':
                    quoted.Append("%%");
                    break;
                default:
                    quoted.Append(c);
                    break;
            }
        }

        return quoted.Append('"').ToString();
    }

    private static string DefaultApplicationsDirectory()
    {
        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(dataHome) || !Path.IsPathRooted(dataHome))
        {
            dataHome = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return Path.Join(dataHome, "applications");
    }

    // Arguments go through ArgumentList, never a command line, and none of them come from input.
    private static string? RunXdgMime(IReadOnlyList<string> arguments)
    {
        var start = new ProcessStartInfo("xdg-mime")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(start);
            if (process is null)
            {
                return null;
            }

            // The answer is one line, far below the pipe buffer, so waiting before reading cannot
            // block the process on a full pipe.
            if (!process.WaitForExit(XdgMimeTimeout))
            {
                process.Kill();
                return null;
            }

            return process.ExitCode == 0 ? process.StandardOutput.ReadToEnd() : null;
        }
        catch (Win32Exception)
        {
            // xdg-utils is not installed: there is no desktop to register with.
            return null;
        }
    }
}
