namespace Waehlhilfe.Domain.Tests;

public sealed class TelUriTests
{
    [Theory]
    [InlineData("tel:0891234567", "0891234567")]
    [InlineData("TEL:0891234567", "0891234567")]
    [InlineData("  tel:0891234567  ", "0891234567")]
    [InlineData("tel://0891234567", "0891234567")]
    public void Parses_a_plain_tel_link(string link, string expected)
    {
        Assert.True(TelUri.TryParse(link, out var uri));
        Assert.Equal(expected, uri.Number);
        Assert.False(uri.IsGlobal);
    }

    [Theory]
    [InlineData("tel:089-123-4567")]
    [InlineData("tel:089.123.4567")]
    [InlineData("tel:(089) 1234567")]
    [InlineData("tel:089%201234567")]
    [InlineData("tel:089 / 1234567")]
    [InlineData("tel:089 1234567")]
    public void Drops_visual_separators(string link)
    {
        Assert.True(TelUri.TryParse(link, out var uri));
        Assert.Equal("0891234567", uri.Number);
    }

    [Theory]
    [InlineData("tel:+49891234567", "+49891234567")]
    [InlineData("tel:+49-89-1234567", "+49891234567")]
    [InlineData("tel:%2B49891234567", "+49891234567")]
    public void Parses_a_global_number(string link, string expected)
    {
        Assert.True(TelUri.TryParse(link, out var uri));
        Assert.Equal(expected, uri.Number);
        Assert.True(uri.IsGlobal);
    }

    [Fact]
    public void Drops_the_bracketed_zero_of_a_global_number()
    {
        Assert.True(TelUri.TryParse("tel:+49 (0)89 1234567", out var uri));
        Assert.Equal("+49891234567", uri.Number);
    }

    [Theory]
    [InlineData("tel:0891234567;ext=12")]
    [InlineData("tel:0891234567;isub=12")]
    [InlineData("tel:0891234567;phone-context=+49")]
    public void Keeps_parameters_off_the_number(string link)
    {
        Assert.True(TelUri.TryParse(link, out var uri));
        Assert.Equal("0891234567", uri.Number);
    }

    [Theory]
    [InlineData("tel:*31#0891234567", "*31#0891234567")]
    [InlineData("tel:%2A31%230891234567", "*31#0891234567")]
    public void Keeps_star_and_hash_service_codes(string link, string expected)
    {
        Assert.True(TelUri.TryParse(link, out var uri));
        Assert.Equal(expected, uri.Number);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tel:")]
    [InlineData("tel:+")]
    [InlineData("0891234567")]
    [InlineData("mailto:0891234567")]
    [InlineData("https://example.org/?tel:0891234567")]
    [InlineData("tel:089abc")]
    [InlineData("tel:+49*31")]
    [InlineData("tel:08+91234567")]
    public void Rejects_what_is_not_a_dialable_tel_link(string? link)
    {
        Assert.False(TelUri.TryParse(link, out var uri));
        Assert.Null(uri);
    }

    // A tel: link is produced by whatever web page or document the user clicked. Anything that
    // reaches the phone after `ATD` is executed by it, so a smuggled line break or semicolon
    // would let a link run AT commands of its own.
    [Theory]
    [InlineData("tel:0891234567%0D%0AATH")]
    [InlineData("tel:0891234567%0AATZ")]
    [InlineData("tel:0891234567\r\nATH")]
    [InlineData("tel:0891234567%0D")]
    [InlineData("tel:0891234567%00")]
    public void Rejects_a_line_break_smuggled_in_by_percent_encoding(string link)
    {
        Assert.False(TelUri.TryParse(link, out _));
    }

    [Theory]
    [InlineData("tel:0891234567%3BATH")]
    [InlineData("tel:0891234567%3B")]
    public void Rejects_a_semicolon_command(string link)
    {
        Assert.False(TelUri.TryParse(link, out _));
    }

    [Theory]
    [InlineData("tel:0891234567;ext=1%0D%0AATH")]
    [InlineData("tel:0891234567;foo\nbar")]
    public void Rejects_a_line_break_in_a_parameter(string link)
    {
        Assert.False(TelUri.TryParse(link, out _));
    }

    [Fact]
    public void Accepts_thirty_two_digits()
    {
        Assert.True(TelUri.TryParse("tel:+" + new string('1', 32), out _));
    }

    [Fact]
    public void Rejects_more_than_thirty_two_digits()
    {
        Assert.False(TelUri.TryParse("tel:" + new string('1', 33), out _));
    }

    [Fact]
    public void Rejects_an_oversized_link_before_decoding_it()
    {
        Assert.False(TelUri.TryParse("tel:1" + new string('-', 10_000), out _));
    }
}
