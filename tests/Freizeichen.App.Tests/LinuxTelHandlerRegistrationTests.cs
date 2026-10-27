using Freizeichen.App.Platform;

namespace Freizeichen.App.Tests;

public sealed class LinuxTelHandlerRegistrationTests : IDisposable
{
    private readonly string _applications = Path.Join(Path.GetTempPath(), "freizeichen-tests-" + Guid.NewGuid().ToString("N"));
    private readonly List<string[]> _calls = [];
    private string? _xdgMimeOutput = string.Empty;

    public void Dispose()
    {
        if (Directory.Exists(_applications))
        {
            Directory.Delete(_applications, recursive: true);
        }
    }

    [Fact]
    public async Task Writes_a_desktop_entry_that_opens_tel_links_with_this_executable()
    {
        await Registration("/opt/freizeichen/Freizeichen").MakeDefaultAsync(TestContext.Current.CancellationToken);

        var entry = await File.ReadAllLinesAsync(Path.Join(_applications, "freizeichen.desktop"), TestContext.Current.CancellationToken);
        Assert.Equal("[Desktop Entry]", entry[0]);
        Assert.Contains("Exec=/opt/freizeichen/Freizeichen %u", entry);
        Assert.Contains("MimeType=x-scheme-handler/tel;", entry);
    }

    // xdg-mime (xdg-utils 1.2) takes the first word of Exec literally, quotes included, and drops a
    // default whose program it cannot find: a quoted plain path made "Prüfen" report another
    // handler although the default was set.
    [Theory]
    [InlineData("/home/user/Freizeichen-1.0/bin/Freizeichen")]
    [InlineData("/usr/lib/freizeichen/Freizeichen")]
    public async Task Leaves_a_plain_executable_path_unquoted_so_xdg_mime_can_resolve_it(string executable)
    {
        await Registration(executable).MakeDefaultAsync(TestContext.Current.CancellationToken);

        var entry = await File.ReadAllLinesAsync(Path.Join(_applications, "freizeichen.desktop"), TestContext.Current.CancellationToken);
        Assert.Contains($"Exec={executable} %u", entry);
    }

    [Fact]
    public async Task Quotes_a_path_with_a_space()
    {
        await Registration("/home/a b/Freizeichen").MakeDefaultAsync(TestContext.Current.CancellationToken);

        var entry = await File.ReadAllLinesAsync(Path.Join(_applications, "freizeichen.desktop"), TestContext.Current.CancellationToken);
        Assert.Contains("Exec=\"/home/a b/Freizeichen\" %u", entry);
    }

    [Fact]
    public async Task Quotes_an_executable_path_the_way_the_desktop_entry_spec_requires()
    {
        await Registration("/home/a b/$x`y\"z\\Free%zeichen").MakeDefaultAsync(TestContext.Current.CancellationToken);

        var entry = await File.ReadAllLinesAsync(Path.Join(_applications, "freizeichen.desktop"), TestContext.Current.CancellationToken);
        Assert.Contains("Exec=\"/home/a b/\\\\$x\\\\`y\\\\\"z\\\\\\\\Free%%zeichen\" %u", entry);
    }

    [Fact]
    public async Task Refuses_an_executable_path_with_a_line_break()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Registration("/opt/x\nExec=evil").MakeDefaultAsync(TestContext.Current.CancellationToken));

        Assert.False(File.Exists(Path.Join(_applications, "freizeichen.desktop")));
    }

    [Fact]
    public async Task Makes_the_entry_the_default_for_tel_links_through_xdg_mime()
    {
        await Registration().MakeDefaultAsync(TestContext.Current.CancellationToken);

        Assert.Equal([["default", "freizeichen.desktop", "x-scheme-handler/tel"]], _calls);
    }

    [Fact]
    public async Task Fails_when_xdg_mime_cannot_set_the_default()
    {
        _xdgMimeOutput = null;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Registration().MakeDefaultAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Is_the_default_when_xdg_mime_names_the_entry()
    {
        _xdgMimeOutput = "freizeichen.desktop\n";

        Assert.True(Registration().IsDefaultHandler());
        Assert.Equal([["query", "default", "x-scheme-handler/tel"]], _calls);
    }

    [Theory]
    [InlineData("org.gnome.Calls.desktop\n")]
    [InlineData("")]
    [InlineData(null)]
    public void Is_not_the_default_when_xdg_mime_names_another_program_or_fails(string? output)
    {
        _xdgMimeOutput = output;

        Assert.False(Registration().IsDefaultHandler());
    }

    private LinuxTelHandlerRegistration Registration(string executable = "/opt/freizeichen/Freizeichen") =>
        new(_applications, executable, args =>
        {
            _calls.Add([.. args]);
            return _xdgMimeOutput;
        });
}
