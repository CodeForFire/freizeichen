using System.Text.Json;
using Freizeichen.Domain;

namespace Freizeichen.AppLogic;

/// <summary>The settings as a small JSON file in the user's profile.</summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    /// <summary>A settings file is well under a kilobyte; anything this size was not written by Freizeichen.</summary>
    public const int MaxFileBytes = 16 * 1024;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _path;

    public JsonSettingsStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    /// <summary>
    /// <c>%APPDATA%\Freizeichen</c> on Windows, <c>~/Library/Application Support/Freizeichen</c> on
    /// macOS, <c>$XDG_CONFIG_HOME/Freizeichen</c> on Linux.
    /// </summary>
    public static string DefaultPath()
    {
        var folder = OperatingSystem.IsMacOS()
            ? Environment.SpecialFolder.LocalApplicationData
            : Environment.SpecialFolder.ApplicationData;
        return Path.Join(Environment.GetFolderPath(folder), "Freizeichen", "settings.json");
    }

    public DialSettings Load()
    {
        try
        {
            var file = new FileInfo(_path);
            if (!file.Exists || file.Length > MaxFileBytes)
            {
                return DialSettings.Default;
            }

            var stored = JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(_path), Options);
            if (stored is null)
            {
                return DialSettings.Default;
            }

            // A hand-edited value gets no further than the settings window would let it.
            var defaults = DialSettings.Default;
            return new DialSettings(
                stored.PortName ?? defaults.PortName,
                DialSettings.IsValidHomeCountryCode(stored.HomeCountryCode) ? stored.HomeCountryCode : defaults.HomeCountryCode,
                DialSettings.IsValidOutsideLinePrefix(stored.OutsideLinePrefix) ? stored.OutsideLinePrefix : defaults.OutsideLinePrefix);
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return DialSettings.Default;
        }
    }

    public void Save(DialSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write beside, then replace, so a crash mid-write never leaves half a file.
        var temporary = _path + ".tmp";
        var stored = new StoredSettings(settings.PortName, settings.HomeCountryCode, settings.OutsideLinePrefix);
        File.WriteAllText(temporary, JsonSerializer.Serialize(stored, Options));
        File.Move(temporary, _path, overwrite: true);
    }

    private sealed record StoredSettings(string? PortName, string? HomeCountryCode, string? OutsideLinePrefix);
}
