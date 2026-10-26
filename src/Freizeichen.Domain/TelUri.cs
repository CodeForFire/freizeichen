using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Freizeichen.Domain;

/// <summary>
/// A <c>tel:</c> link (RFC 3966) reduced to the number it names. <see cref="Number"/> holds only
/// digits, with a leading <c>+</c> for a global number: the link comes from whatever page or
/// document the user clicked, and whatever reaches the phone after <c>ATD</c> is executed by it.
/// That rules out <c>*</c> and <c>#</c> too — on a landline <c>*21*&lt;number&gt;#</c> sets up call
/// forwarding at the exchange.
/// </summary>
public sealed record TelUri
{
    public const int MaxDigits = 32;

    // Far above any real link; checked before decoding, so a huge argument costs nothing.
    private const int MaxLinkLength = 256;

    private const string Scheme = "tel:";

    private TelUri(string number) => Number = number;

    public string Number { get; }

    public bool IsGlobal => Number.StartsWith('+');

    public static bool TryParse(string? link, [MaybeNullWhen(false)] out TelUri uri)
    {
        uri = null;
        if (link is null || link.Length > MaxLinkLength)
        {
            return false;
        }

        var text = link.Trim();
        if (!text.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        text = text[Scheme.Length..];
        if (text.StartsWith("//", StringComparison.Ordinal))
        {
            text = text[2..];
        }

        var separator = text.IndexOf(';', StringComparison.Ordinal);
        var numberPart = separator < 0 ? text : text[..separator];
        if (separator >= 0 && !AreParametersClean(text[(separator + 1)..]))
        {
            return false;
        }

        if (!TryClean(Uri.UnescapeDataString(numberPart), out var number))
        {
            return false;
        }

        uri = new TelUri(number);
        return true;
    }

    // Parameters (ext, isub, phone-context, …) never reach the phone, but a link carrying a
    // control character anywhere is not one a person wrote, so it is refused as a whole.
    private static bool AreParametersClean(string parameters) =>
        !Uri.UnescapeDataString(parameters).Any(char.IsControl);

    private static bool TryClean(string raw, [MaybeNullWhen(false)] out string number)
    {
        number = null;
        var global = raw.StartsWith('+');
        if (global)
        {
            // "+49 (0)89 …" is how German numbers are often printed; the (0) is not dialled.
            raw = raw[1..].Replace("(0)", string.Empty, StringComparison.Ordinal);
        }

        var digits = new StringBuilder(raw.Length + 1);
        if (global)
        {
            digits.Append('+');
        }

        foreach (var c in raw)
        {
            if (char.IsAsciiDigit(c))
            {
                digits.Append(c);
            }
            else if (!IsVisualSeparator(c))
            {
                return false;
            }
        }

        var count = digits.Length - (global ? 1 : 0);
        if (count is 0 or > MaxDigits)
        {
            return false;
        }

        number = digits.ToString();
        return true;
    }

    // RFC 3966's visual separators, plus the space, no-break space and slash that German
    // numbers are printed with ("089 / 1234567"). Never \r, \n or any other control character.
    private static bool IsVisualSeparator(char c) => c is '-' or '.' or '(' or ')' or ' ' or ' ' or '/';
}
