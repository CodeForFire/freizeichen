namespace Waehlhilfe.Domain.Tests;

public sealed class DialNumberTests
{
    private static readonly DialSettings Germany = DialSettings.Default;

    private static DialNumber Dial(string link, DialSettings settings)
    {
        Assert.True(TelUri.TryParse(link, out var uri));
        return DialNumber.From(uri, settings);
    }

    [Fact]
    public void Dials_a_national_number_as_written()
    {
        Assert.Equal("0891234567", Dial("tel:0891234567", Germany).Digits);
    }

    [Fact]
    public void Turns_plus_49_into_a_leading_zero()
    {
        Assert.Equal("0891234567", Dial("tel:+49891234567", Germany).Digits);
    }

    [Fact]
    public void Turns_a_foreign_plus_into_double_zero()
    {
        Assert.Equal("0043121234", Dial("tel:+43121234", Germany).Digits);
    }

    [Fact]
    public void Uses_the_configured_home_country()
    {
        var austria = Germany with { HomeCountryCode = "43" };

        Assert.Equal("0121234", Dial("tel:+43121234", austria).Digits);
        Assert.Equal("0049891234567", Dial("tel:+49891234567", austria).Digits);
    }

    [Fact]
    public void Prepends_the_outside_line_prefix()
    {
        var behindPbx = Germany with { OutsideLinePrefix = "0" };

        Assert.Equal("00891234567", Dial("tel:0891234567", behindPbx).Digits);
        Assert.Equal("00891234567", Dial("tel:+49891234567", behindPbx).Digits);
    }

    [Fact]
    public void Keeps_a_service_code_as_written()
    {
        Assert.Equal("*31#0891234567", Dial("tel:*31#0891234567", Germany).Digits);
    }

    [Theory]
    [InlineData("0;ATH")]
    [InlineData("0\r")]
    [InlineData("12345")]
    [InlineData("a")]
    public void Refuses_an_outside_line_prefix_that_is_not_a_short_digit_string(string prefix)
    {
        Assert.False(DialSettings.IsValidOutsideLinePrefix(prefix));
        Assert.Throws<ArgumentException>(() => Dial("tel:0891234567", Germany with { OutsideLinePrefix = prefix }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("04")]
    [InlineData("1234")]
    [InlineData("+49")]
    [InlineData("4;")]
    public void Refuses_a_home_country_code_that_is_not_one_to_three_digits(string code)
    {
        Assert.False(DialSettings.IsValidHomeCountryCode(code));
        Assert.Throws<ArgumentException>(() => Dial("tel:+49891234567", Germany with { HomeCountryCode = code }));
    }

    [Fact]
    public void Default_settings_are_for_germany_without_an_outside_line()
    {
        Assert.Equal("49", DialSettings.Default.HomeCountryCode);
        Assert.Equal(string.Empty, DialSettings.Default.OutsideLinePrefix);
        Assert.Equal(string.Empty, DialSettings.Default.PortName);
    }
}
