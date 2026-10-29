# Bluetooth einrichten

Freizeichen braucht vom Betriebssystem nur eines: eine serielle Schnittstelle zum Telefon. Dafür
wird das Telefon **gekoppelt**, aber **nicht als Audiogerät verbunden**. Solange der PC die
Audioverbindung (Freisprechen/Headset) hält, antwortet zumindest das DL500A auf der seriellen
Schnittstelle nicht.

Jeder Abschnitt sagt, wie gut er belegt ist:

- **getestet** – so eingerichtet hat Freizeichen mit einem echten Telefon gewählt.
- **theoretisch** – so sollte es gehen, mit einem echten Telefon geprüft hat es noch niemand.
  Klappt es (oder nicht), bitte [Gerät melden](https://github.com/CodeForFire/freizeichen/issues/new?template=device_report.yml).

Welche Telefone getestet sind: [Getestete Geräte](geraete.md).

## Windows

**Status: getestet** mit dem Gigaset DL500A.

1. Am Telefon Bluetooth einschalten und sichtbar machen.
2. **Einstellungen → Bluetooth und Geräte → Gerät hinzufügen → Bluetooth**, das Telefon wählen und
   die PIN bestätigen.
3. Die Audioverbindung abschalten: **Systemsteuerung → Geräte und Drucker**, Rechtsklick auf das
   Telefon → **Eigenschaften → Dienste**, die Haken bei den Audio-Diensten
   (*Freisprechtelefonie*, *Headset*) entfernen. Gekoppelt bleibt das Telefon.
4. Den COM-Port ablesen: **Einstellungen → Bluetooth und Geräte → Geräte → Weitere
   Bluetooth-Einstellungen → COM-Anschlüsse**. Gebraucht wird der Port mit der Richtung
   **Ausgehend** und dem Namen des Telefons. Fehlt er: **Hinzufügen… → Ausgehend**, das Telefon
   wählen.
5. In Freizeichen unter **Bluetooth-Port** diesen COM-Port wählen.

## Linux

**Status: theoretisch.** Freizeichen ist unter Linux bisher nur gegen das simulierte Telefon
`tools/fake-dl500a.py` gelaufen.

Gebraucht werden BlueZ (Paket `bluez`) und die Werkzeuge `rfcomm` und `sdptool`. Neuere
Distributionen liefern die beiden nicht mehr überall mit; unter Debian und Ubuntu sind sie im
Paket `bluez` enthalten.

1. Koppeln, aber **nicht** verbinden – `connect` würde die Audioverbindung aufbauen:

   ```sh
   bluetoothctl
   [bluetooth]# scan on          # warten, bis das Telefon mit seiner Adresse erscheint
   [bluetooth]# pair AA:BB:CC:DD:EE:FF
   [bluetooth]# trust AA:BB:CC:DD:EE:FF
   [bluetooth]# quit
   ```

2. Den RFCOMM-Kanal der seriellen Schnittstelle finden (beim DL500A: Kanal 1):

   ```sh
   sdptool browse AA:BB:CC:DD:EE:FF | grep -A8 'Serial Port\|Dial-up'
   ```

3. Den Port anlegen. Er verbindet sich erst, wenn Freizeichen ihn öffnet:

   ```sh
   sudo rfcomm bind 0 AA:BB:CC:DD:EE:FF 1
   ```

4. Den eigenen Benutzer in die Gruppe `dialout` aufnehmen, damit Freizeichen `/dev/rfcomm0` öffnen
   darf, dann ab- und wieder anmelden:

   ```sh
   sudo usermod -aG dialout "$USER"
   ```

5. In Freizeichen unter **Bluetooth-Port** `/dev/rfcomm0` wählen.

`rfcomm bind` gilt nur bis zum Neustart. Damit der Port bei jedem Start da ist, kann ihn eine
systemd-Unit anlegen, z. B. `/etc/systemd/system/rfcomm0.service`:

```ini
[Unit]
Description=Serielle Bluetooth-Schnittstelle zum Telefon
After=bluetooth.service
Requires=bluetooth.service

[Service]
Type=oneshot
RemainAfterExit=yes
ExecStart=/usr/bin/rfcomm bind 0 AA:BB:CC:DD:EE:FF 1
ExecStop=/usr/bin/rfcomm release 0

[Install]
WantedBy=multi-user.target
```

Danach `sudo systemctl enable --now rfcomm0.service`.

## macOS

**Status: theoretisch.** Mit einem echten Telefon unter macOS ist Freizeichen noch nicht gelaufen.

1. Am Telefon Bluetooth einschalten und sichtbar machen.
2. **Systemeinstellungen → Bluetooth**, beim Telefon **Verbinden** und die PIN bestätigen.
3. Bietet das Telefon eine serielle Schnittstelle an, legt macOS dafür einen Port
   `/dev/cu.<Gerätename>` an. Prüfen im Terminal:

   ```sh
   ls /dev/cu.*
   ```

4. In Freizeichen unter **Bluetooth-Port** diesen Port wählen. Freizeichen zeigt nur die
   `cu.`-Ports und blendet `Bluetooth-Incoming-Port` und `debug-console` aus.

Ob neuere macOS-Versionen für jedes Telefon einen solchen Port anlegen, ist nicht geprüft. Fehlt
er, kann Freizeichen mit diesem Telefon unter macOS nicht wählen – dann bitte melden.

Hat macOS das Telefon zusätzlich als Audiogerät verbunden und Freizeichen meldet „Telefon
antwortet nicht.“, die Audioverbindung in den Ton-Einstellungen bzw. unter **Bluetooth**
trennen.

## Prüfen

In den Einstellungen von Freizeichen:

1. **Verbindung testen** – „Telefon antwortet. Alles bereit.“
2. **Testanruf** auf das eigene Handy – das Telefon wählt.

Kommt eine Fehlermeldung, hilft die [Fehlerbehebung](../README.md#fehlerbehebung) weiter.
