using Waehlhilfe.Domain;

namespace Waehlhilfe.AppLogic.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Join(Path.GetTempPath(), "waehlhilfe-tests-" + Guid.NewGuid().ToString("N"));

    private string File => Path.Join(_directory, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Starts_from_the_defaults_when_nothing_was_saved()
    {
        Assert.Equal(DialSettings.Default, new JsonSettingsStore(File).Load());
    }

    [Fact]
    public void Round_trips_saved_settings()
    {
        var settings = new DialSettings("/dev/cu.DL500A", "43", "0");

        new JsonSettingsStore(File).Save(settings);

        Assert.Equal(settings, new JsonSettingsStore(File).Load());
    }

    [Fact]
    public void Falls_back_to_the_defaults_for_a_damaged_file()
    {
        Directory.CreateDirectory(_directory);
        System.IO.File.WriteAllText(File, "{ not json");

        Assert.Equal(DialSettings.Default, new JsonSettingsStore(File).Load());
    }

    // The file is plain JSON in the user's profile; a value edited by hand must not get past
    // the rules the settings window enforces.
    [Fact]
    public void Drops_hand_edited_values_the_settings_window_would_refuse()
    {
        Directory.CreateDirectory(_directory);
        System.IO.File.WriteAllText(File, """{ "portName": "COM7", "homeCountryCode": "4;", "outsideLinePrefix": "*21*" }""");

        Assert.Equal(DialSettings.Default with { PortName = "COM7" }, new JsonSettingsStore(File).Load());
    }

    [Fact]
    public void Ignores_an_oversized_file()
    {
        Directory.CreateDirectory(_directory);
        System.IO.File.WriteAllText(File, """{ "portName": "COM7" }""" + new string(' ', JsonSettingsStore.MaxFileBytes));

        Assert.Equal(DialSettings.Default, new JsonSettingsStore(File).Load());
    }
}
