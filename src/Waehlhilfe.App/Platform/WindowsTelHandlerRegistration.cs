using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Waehlhilfe.AppLogic;

namespace Waehlhilfe.App.Platform;

/// <summary>
/// The MSI registers Wählhilfe as *capable* of handling <c>tel:</c> (ProgId, Capabilities,
/// RegisteredApplications). Windows lets no program make itself the default, so this opens the
/// Settings page where the user picks it once.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsTelHandlerRegistration : ITelHandlerRegistration
{
    /// <summary>Must match the ProgId and the RegisteredApplications name in packaging/windows/Waehlhilfe.wxs.</summary>
    internal const string ProgId = "Waehlhilfe.tel";

    internal const string RegisteredApplicationName = "Waehlhilfe";

    private const string UserChoiceKey = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\tel\UserChoice";

    public bool IsDefaultHandler()
    {
        using var key = Registry.CurrentUser.OpenSubKey(UserChoiceKey);
        return string.Equals(key?.GetValue("ProgId") as string, ProgId, StringComparison.Ordinal);
    }

    public Task MakeDefaultAsync(CancellationToken cancellationToken = default)
    {
        // A fixed URI, nothing from input: the Settings page that lists Wählhilfe's link types.
        using var _ = Process.Start(new ProcessStartInfo($"ms-settings:defaultapps?registeredAppMachine={RegisteredApplicationName}")
        {
            UseShellExecute = true,
        });
        return Task.CompletedTask;
    }
}
