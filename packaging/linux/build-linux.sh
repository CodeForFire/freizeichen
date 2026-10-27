#!/usr/bin/env bash
# Package a linux-x64 publish output as a .tar.gz for any distribution and a .deb for
# Debian/Ubuntu. Needs dpkg-deb and rsvg-convert (librsvg2-bin). Runs on the ubuntu-latest CI
# runner.
#
#   build-linux.sh <version> <publish-dir> <out-dir>
set -euo pipefail

command -v rsvg-convert >/dev/null || { echo "error: rsvg-convert not found (apt install librsvg2-bin)" >&2; exit 1; }

VERSION="$1"
PUBLISH_DIR="$2"
OUT_DIR="$3"

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
mkdir -p "$OUT_DIR"

# The tarball: unpack, start Freizeichen, press "Als Standard festlegen". The button writes the
# user's own desktop entry pointing at wherever the binary was unpacked.
TAR_ROOT="freizeichen-${VERSION}"
install -d "$STAGE/tar/$TAR_ROOT"
cp -R "$PUBLISH_DIR"/. "$STAGE/tar/$TAR_ROOT/"
# The runner's umask leaks into the archives otherwise.
chmod -R u=rwX,go=rX "$STAGE/tar"
chmod 755 "$STAGE/tar/$TAR_ROOT/Freizeichen"
TARBALL="$OUT_DIR/freizeichen-${VERSION}-linux-x64.tar.gz"
tar -C "$STAGE/tar" --owner=0 --group=0 --numeric-owner --sort=name -czf "$TARBALL" "$TAR_ROOT"

# The .deb: the program under /usr/lib, a /usr/bin link, and a system desktop entry so tel: links
# reach Freizeichen without the button. The entry's file name matches
# LinuxTelHandlerRegistration.EntryName: the user entry the button writes then shadows this one
# under the same id. desktop-file-utils' dpkg trigger refreshes the MIME cache.
ROOT="$STAGE/deb"
install -d "$ROOT/DEBIAN" "$ROOT/usr/lib/freizeichen" "$ROOT/usr/bin" \
  "$ROOT/usr/share/applications" "$ROOT/usr/share/doc/freizeichen"
cp -R "$PUBLISH_DIR"/. "$ROOT/usr/lib/freizeichen/"
ln -s ../lib/freizeichen/Freizeichen "$ROOT/usr/bin/freizeichen"

# The icon under the name Icon= looks up, one file per size hicolor's index.theme declares (it
# stops at 512), each rendered from the vector so the handset stays sharp at panel sizes. The
# scalable copy serves every size the theme does not declare. The publish output carries the
# master as freizeichen.svg (Freizeichen.App.csproj).
ICON_SVG="$PUBLISH_DIR/freizeichen.svg"
for size in 16 22 24 32 48 64 96 128 256 512; do
  install -d "$ROOT/usr/share/icons/hicolor/${size}x${size}/apps"
  rsvg-convert -w "$size" -h "$size" "$ICON_SVG" -o "$ROOT/usr/share/icons/hicolor/${size}x${size}/apps/freizeichen.png"
done
install -Dm644 "$ICON_SVG" "$ROOT/usr/share/icons/hicolor/scalable/apps/freizeichen.svg"

# StartupWMClass is the window's WM_CLASS, which Avalonia takes from the assembly name: it is what
# ties the running window to this entry, and so to its icon, in the dock and the task switcher.
cat > "$ROOT/usr/share/applications/freizeichen.desktop" <<'DESKTOP'
[Desktop Entry]
Type=Application
Name=Freizeichen
Comment=Wählt tel:-Links über ein Gigaset DL500A per Bluetooth
Exec=/usr/bin/freizeichen %u
Icon=freizeichen
Terminal=false
StartupWMClass=Freizeichen
Categories=Network;Telephony;
MimeType=x-scheme-handler/tel;
DESKTOP

# AppStream metadata: GNOME Software and KDE Discover read the licence, summary and age rating of
# an installed app from here; without it they report an unknown, possibly proprietary licence.
# The id is the reverse-DNS name macOS uses as well; the launchable ties it to the entry above.
# (A .deb not yet installed still shows an unknown licence: PackageKit's apt backend reports
# "unknown" for every package, whatever the package carries.)
# The screenshots are the README's (ReadmeScreenshotTests), fetched by the software centre from
# GitHub: a release points at its own tag, a dev build, which has none, at main.
case "$VERSION" in
  0.0.0-dev.*) SCREENSHOT_REF=main ;;
  *) SCREENSHOT_REF="v${VERSION}" ;;
esac
SCREENSHOTS="https://raw.githubusercontent.com/CodeForFire/freizeichen/${SCREENSHOT_REF}/docs/screenshots"
install -d "$ROOT/usr/share/metainfo"
cat > "$ROOT/usr/share/metainfo/de.codeforfire.freizeichen.metainfo.xml" <<METAINFO
<?xml version="1.0" encoding="UTF-8"?>
<component type="desktop-application">
  <id>de.codeforfire.freizeichen</id>
  <name>Freizeichen</name>
  <summary>Wählt tel:-Links über ein Gigaset DL500A per Bluetooth</summary>
  <metadata_license>CC0-1.0</metadata_license>
  <project_license>MIT</project_license>
  <developer id="de.codeforfire">
    <name>CodeForFire</name>
  </developer>
  <description>
    <p>
      Freizeichen wird zum Standardprogramm für tel:-Links und wählt die Nummer über die
      serielle Bluetooth-Schnittstelle eines Gigaset DL500A. Vor jedem Anruf zeigt es die Nummer
      zur Bestätigung.
    </p>
  </description>
  <launchable type="desktop-id">freizeichen.desktop</launchable>
  <url type="homepage">https://github.com/CodeForFire/freizeichen</url>
  <url type="bugtracker">https://github.com/CodeForFire/freizeichen/issues</url>
  <provides>
    <mediatype>x-scheme-handler/tel</mediatype>
  </provides>
  <screenshots>
    <screenshot type="default">
      <caption>Vor dem Wählen bestätigen</caption>
      <image>${SCREENSHOTS}/dial.png</image>
    </screenshot>
    <screenshot>
      <caption>Telefon einrichten und testen</caption>
      <image>${SCREENSHOTS}/settings.png</image>
    </screenshot>
  </screenshots>
  <content_rating type="oars-1.1"/>
  <releases>
    <release version="${VERSION}" date="$(date -u +%Y-%m-%d)"/>
  </releases>
</component>
METAINFO

cat > "$ROOT/usr/share/doc/freizeichen/copyright" <<'COPYRIGHT'
Format: https://www.debian.org/doc/packaging-manuals/copyright-format/1.0/
Upstream-Name: Freizeichen
Source: https://github.com/CodeForFire/freizeichen

Files: *
Copyright: 2026 Thomas Müller
License: MIT
 See /usr/lib/freizeichen/LICENSE.txt. The bundled fonts are under the SIL Open Font
 License 1.1 (/usr/lib/freizeichen/OFL.txt); further third-party notices are in
 /usr/lib/freizeichen/THIRD-PARTY-NOTICES.md.
COPYRIGHT

# Debian sorts "~" before everything, so a prerelease 0.2.0-rc.1 becomes 0.2.0~rc.1 and installs
# below the final 0.2.0. A self-contained .NET app still needs the system's ICU, whose package
# name carries its version; the alternatives cover Debian 12/13 and Ubuntu 24.04 onwards.
# hicolor-icon-theme owns hicolor's index.theme, without which Icon=freizeichen finds nothing; a
# GTK app gets it transitively, Avalonia links no GTK. Its dpkg trigger refreshes the icon cache.
DEB_VERSION="${VERSION//-/\~}"
INSTALLED_KB=$(du -sk "$ROOT/usr" | cut -f1)
cat > "$ROOT/DEBIAN/control" <<CONTROL
Package: freizeichen
Version: ${DEB_VERSION}
Architecture: amd64
Section: comm
Priority: optional
Maintainer: Thomas Müller <1005065+DeepDiver1975@users.noreply.github.com>
Homepage: https://github.com/CodeForFire/freizeichen
Installed-Size: ${INSTALLED_KB}
Depends: libc6, libstdc++6, libgcc-s1, zlib1g, libfontconfig1, libx11-6, libice6, libsm6, xdg-utils, hicolor-icon-theme, libicu72 | libicu74 | libicu76 | libicu77 | libicu78
Recommends: bluez
Description: Dial tel: links on a Gigaset DL500A over Bluetooth
 Freizeichen registers as the tel: handler and dials the number on a Gigaset
 DL500A through its Bluetooth serial port, after a confirm window.
CONTROL

chmod -R u=rwX,go=rX "$ROOT"
chmod 755 "$ROOT/usr/lib/freizeichen/Freizeichen"
DEB="$OUT_DIR/freizeichen_${DEB_VERSION}_amd64.deb"
dpkg-deb --root-owner-group -Zxz --build "$ROOT" "$DEB" 1>&2

echo "$TARBALL"
echo "$DEB"
