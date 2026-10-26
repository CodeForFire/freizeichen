namespace Waehlhilfe.Domain;

/// <summary>How numbers are dialled and through which port.</summary>
/// <param name="PortName">Serial port of the phone's Bluetooth SPP service; empty until chosen.</param>
/// <param name="HomeCountryCode">Country code that a global number is dialled nationally for.</param>
/// <param name="OutsideLinePrefix">Amtsholung, dialled before every number behind a PBX; usually empty.</param>
public sealed record DialSettings(string PortName, string HomeCountryCode, string OutsideLinePrefix)
{
    public static DialSettings Default { get; } = new(string.Empty, "49", string.Empty);

    public static bool IsValidHomeCountryCode(string? code) =>
        code is { Length: >= 1 and <= 3 } && code[0] != '0' && code.All(char.IsAsciiDigit);

    public static bool IsValidOutsideLinePrefix(string? prefix) =>
        prefix is { Length: <= 4 } && prefix.All(char.IsAsciiDigit);
}
