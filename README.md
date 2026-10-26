# Freizeichen

*Ein Klick auf die Nummer – das Telefon wählt.*

Wählt `tel:`-Links über ein Gigaset DL500A – per Bluetooth, ohne Gigaset QuickSync, TAPI oder CSTA.

Ein Klick auf eine Telefonnummer in Lagebuch (oder in jedem anderen Programm, das `tel:`-Links
öffnet) zeigt ein kleines Fenster „0891234567 wählen?“. Nach *Wählen* baut das DL500A den Anruf
auf; das Gespräch wird am Telefon geführt.

## So funktioniert es

Das DL500A stellt über Bluetooth eine serielle Schnittstelle (SPP) bereit und versteht dort
AT-Befehle. Freizeichen öffnet diese Schnittstelle, sendet `ATD <Nummer>` und schließt sie wieder.

## Ohne Telefon testen

Unter Linux spielt `tools/fake-dl500a.py` ein DL500A auf einem Pseudo-Terminal und zeigt jede
Zeile, die Freizeichen sendet:

```sh
make fake-phone                     # legt /dev/rfcomm9 an (fragt per sudo), SCENARIO=ok
make run-fake                       # Einstellungen: /dev/rfcomm9 wählen, „Testen“
make run-fake ARGS=tel:0891234567   # Wählfenster
```

`SCENARIO` steuert die Antwort auf `ATD`: `ok`, `error`, `busy`, `nocarrier`, `nodialtone` oder
`silent` – Letzteres antwortet gar nicht, wie das DL500A bei bestehender Audioverbindung.
`make run-fake` startet Freizeichen mit `tools/pty-modem-lines.c`, weil ein Pseudo-Terminal keine
DTR/RTS-Leitungen hat und der Port sonst nicht aufgeht.

## Status

In Entwicklung. Hintergrund: CodeForFire/lagebuch#617.
