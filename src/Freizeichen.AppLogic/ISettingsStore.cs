using Freizeichen.Domain;

namespace Freizeichen.AppLogic;

public interface ISettingsStore
{
    /// <summary>The saved settings, or <see cref="DialSettings.Default"/> when there are none or they are unreadable.</summary>
    DialSettings Load();

    /// <exception cref="IOException">The settings could not be written.</exception>
    void Save(DialSettings settings);
}
