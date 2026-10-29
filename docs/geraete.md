# Getestete Geräte

Freizeichen spricht mit dem Telefon nur über eine serielle Bluetooth-Schnittstelle und sendet dort
`AT` und `ATD <Nummer>`. Es weiß nichts über ein bestimmtes Modell. Getestet ist es bisher mit
einem Gerät; jedes andere Telefon, das die [Voraussetzungen](#was-ein-telefon-braucht) erfüllt,
sollte ebenso funktionieren.

| Gerät | System | Status | Hinweise |
|---|---|---|---|
| Gigaset DL500A | Windows | **getestet** | SPP auf RFCOMM-Kanal 1. Antwortet nicht, solange es als Audiogerät verbunden ist. |
| Gigaset DL500A | Linux, macOS | theoretisch | Nur gegen `tools/fake-dl500a.py` geprüft, siehe [Bluetooth einrichten](bluetooth-einrichten.md). |

**getestet** heißt: Mit diesem Gerät auf diesem System hat Freizeichen echte Anrufe aufgebaut.
**theoretisch** heißt: Es sollte funktionieren, ausprobiert hat es aber noch niemand.

## Was ein Telefon braucht

- **Bluetooth mit serieller Schnittstelle:** das Serial Port Profile (SPP) oder das Dial-up
  Networking Profile (DUN). Das Betriebssystem macht daraus einen COM-Port, `/dev/cu.…` oder
  `/dev/rfcomm0`.
- **AT-Befehle:** Auf `AT` antwortet das Telefon mit `OK`, auf `ATD <Nummer>` baut es den Anruf
  auf und antwortet mit `OK`.
- **Der Anruf bleibt bestehen,** wenn Freizeichen die Schnittstelle danach wieder schließt.

In Frage kommen Tischtelefone und DECT-Basen mit Bluetooth sowie ältere Handys mit DUN – welche
es wirklich können, zeigt erst ein Test. Aktuelle Smartphones bieten meist keine serielle
Schnittstelle mit AT-Befehlen mehr an.

Ob ein Telefon antwortet, zeigt in den Einstellungen **Verbindung testen**; ob es auch wählt, ein
**Testanruf**.

## Gerät melden

Freizeichen mit einem anderen Telefon ausprobiert – ob es geklappt hat oder nicht? Bitte
[Gerät melden](https://github.com/CodeForFire/freizeichen/issues/new?template=device_report.yml).
Die Meldung kommt in diese Liste.
