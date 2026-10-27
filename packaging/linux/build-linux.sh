#!/usr/bin/env bash
# Package a linux-x64 publish output as a .tar.gz for any distribution and a .deb for
# Debian/Ubuntu. Needs dpkg-deb. Runs on the ubuntu-latest CI runner.
#
#   build-linux.sh <version> <publish-dir> <out-dir>
set -euo pipefail

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

cat > "$ROOT/usr/share/applications/freizeichen.desktop" <<'DESKTOP'
[Desktop Entry]
Type=Application
Name=Freizeichen
Comment=Wählt tel:-Links über ein Gigaset DL500A per Bluetooth
Exec=/usr/bin/freizeichen %u
Terminal=false
Categories=Network;Telephony;
MimeType=x-scheme-handler/tel;
DESKTOP

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
Depends: libc6, libstdc++6, libgcc-s1, zlib1g, libfontconfig1, libx11-6, libice6, libsm6, xdg-utils, libicu72 | libicu74 | libicu76 | libicu77 | libicu78
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
