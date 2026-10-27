#!/usr/bin/env bash
# Build an unsigned Freizeichen.app and wrap it in a drag-to-Applications .dmg. macOS-only
# (hdiutil, codesign). Runs on the macos-latest CI runner.
#
#   build-dmg.sh <version> <publish-dir> <arch> <out-dir>
set -euo pipefail

VERSION="$1"
PUBLISH_DIR="$2"
ARCH="$3"
OUT_DIR="$4"

APP="Freizeichen.app"
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

APP_DIR="$STAGE/$APP"
install -d "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"

cp -R "$PUBLISH_DIR"/. "$APP_DIR/Contents/MacOS/"
chmod +x "$APP_DIR/Contents/MacOS/Freizeichen"

# CFBundleURLTypes is what lets Launch Services hand tel: links to the app; it delivers them as an
# Apple event, not an argument. The bundle id must match MacTelHandlerRegistration.BundleId.
cat > "$APP_DIR/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>              <string>Freizeichen</string>
  <key>CFBundleDisplayName</key>       <string>Freizeichen</string>
  <key>CFBundleIdentifier</key>        <string>de.codeforfire.freizeichen</string>
  <key>CFBundleVersion</key>           <string>$VERSION</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleExecutable</key>        <string>Freizeichen</string>
  <key>CFBundlePackageType</key>       <string>APPL</string>
  <key>LSMinimumSystemVersion</key>    <string>12.0</string>
  <key>NSHighResolutionCapable</key>   <true/>
  <key>NSBluetoothAlwaysUsageDescription</key>
  <string>Freizeichen wählt über die serielle Bluetooth-Schnittstelle des Telefons.</string>
  <key>CFBundleURLTypes</key>
  <array>
    <dict>
      <key>CFBundleURLName</key>       <string>Telefonnummer</string>
      <key>CFBundleURLSchemes</key>    <array><string>tel</string></array>
    </dict>
  </array>
</dict>
</plist>
PLIST

# No Developer ID, so sign with the ad-hoc identity "-": without any signature Gatekeeper reports
# the app as damaged instead of offering to open it.
codesign --force --deep --sign - "$APP_DIR"

ln -s /Applications "$STAGE/Applications"
mkdir -p "$OUT_DIR"
DMG="$OUT_DIR/freizeichen-${VERSION}-macos-${ARCH}.dmg"
hdiutil create -volname "Freizeichen" -srcfolder "$STAGE" -ov -format UDZO "$DMG" 1>&2
echo "$DMG"
