using Freizeichen.Modem;

namespace Freizeichen.AppLogic;

/// <summary>What the Lagebuchführer reads after a dial or a connection test.</summary>
public static class DialMessages
{
    public static string For(DialResult result, string portName) => result switch
    {
        DialResult.Dialled => "Gewählt. Gespräch am Telefon führen.",
        DialResult.Answered => "Telefon antwortet. Alles bereit.",
        DialResult.PortNotSet => "Kein Telefon eingerichtet – in den Freizeichen-Einstellungen den Bluetooth-Port wählen.",
        DialResult.PortMissing => $"Port {portName} nicht gefunden. Ist das Telefon mit diesem PC gekoppelt?",
        DialResult.PortBusy => $"Port {portName} ist belegt oder gesperrt – läuft noch ein Anruf, oder nutzt ein anderes Programm den Port?",
        DialResult.NoResponse => "Telefon antwortet nicht. Bluetooth am Telefon eingeschaltet? Ist es mit diesem PC als Audiogerät verbunden, diese Verbindung trennen.",
        DialResult.Rejected => "Das Telefon hat die Wahl abgelehnt.",
        DialResult.LineBusy => "Besetzt.",
        DialResult.NoCarrier => "Keine Verbindung zustande gekommen.",
        DialResult.NoDialTone => "Kein Wählton – ist die Telefonleitung angeschlossen?",
        _ => "Unbekannte Antwort vom Telefon.",
    };

    public static bool IsError(DialResult result) => result is not (DialResult.Dialled or DialResult.Answered);
}
