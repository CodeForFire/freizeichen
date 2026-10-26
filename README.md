# Wählhilfe

Wählt `tel:`-Links über ein Gigaset DL500A – per Bluetooth, ohne Gigaset QuickSync, TAPI oder CSTA.

Ein Klick auf eine Telefonnummer in Lagebuch (oder in jedem anderen Programm, das `tel:`-Links
öffnet) zeigt ein kleines Fenster „0891234567 wählen?“. Nach *Wählen* baut das DL500A den Anruf
auf; das Gespräch wird am Telefon geführt.

## So funktioniert es

Das DL500A stellt über Bluetooth eine serielle Schnittstelle (SPP) bereit und versteht dort
AT-Befehle. Wählhilfe öffnet diese Schnittstelle, sendet `ATD <Nummer>` und schließt sie wieder.

## Status

In Entwicklung. Hintergrund: CodeForFire/lagebuch#617.
