namespace Waehlhilfe.Domain;

/// <summary>The exact string sent after <c>ATD</c>: digits only.</summary>
public sealed record DialNumber
{
    private DialNumber(string digits) => Digits = digits;

    public string Digits { get; }

    /// <exception cref="ArgumentException">The settings carry an invalid country code or prefix.</exception>
    public static DialNumber From(TelUri uri, DialSettings settings)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(settings);
        if (!DialSettings.IsValidHomeCountryCode(settings.HomeCountryCode))
        {
            throw new ArgumentException("The home country code must be one to three digits.", nameof(settings));
        }

        if (!DialSettings.IsValidOutsideLinePrefix(settings.OutsideLinePrefix))
        {
            throw new ArgumentException("The outside line prefix must be at most four digits.", nameof(settings));
        }

        var number = uri.Number;
        if (uri.IsGlobal)
        {
            var national = number[1..];
            number = national.StartsWith(settings.HomeCountryCode, StringComparison.Ordinal)
                ? "0" + national[settings.HomeCountryCode.Length..]
                : "00" + national;
        }

        return new DialNumber(settings.OutsideLinePrefix + number);
    }

    public override string ToString() => Digits;
}
