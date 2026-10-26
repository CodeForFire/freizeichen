namespace Freizeichen.App.Tests;

public sealed class LaunchArgumentsTests
{
    [Fact]
    public void Finds_the_tel_link_windows_passes_as_the_first_argument()
    {
        Assert.Equal("tel:0891234567", LaunchArguments.TelLink(["tel:0891234567"]));
    }

    [Fact]
    public void Matches_the_scheme_case_insensitively()
    {
        Assert.Equal("TEL:0891234567", LaunchArguments.TelLink(["--flag", "TEL:0891234567"]));
    }

    [Theory]
    [InlineData]
    [InlineData("--settings")]
    [InlineData("0891234567")]
    public void Opens_the_settings_without_a_tel_link(params string[] args)
    {
        Assert.Null(LaunchArguments.TelLink(args));
    }
}
