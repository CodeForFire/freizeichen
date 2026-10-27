#!/usr/bin/env bash
# Regenerate the icon derivatives from the master, src/Freizeichen.App/Assets/icon.svg. Run it
# after changing the master and commit the outputs; never hand-edit them.
#
#   packaging/icon/build-icon-assets.sh        (or: make icon)
#
# Needs rsvg-convert (librsvg2-bin) and ImageMagick 7 (magick). ImageMagick's own SVG renderer
# drops the gradients, so it only assembles the .ico from rsvg-convert's renders.
#
# The outputs are committed rather than built in CI because the macOS and Windows runners have no
# SVG renderer: build-dmg.sh makes the .icns from icon-1024.png with sips, and the exe, the MSI
# and both windows take icon.ico. build-linux.sh renders the hicolor sizes from icon-1024.png.
set -euo pipefail

ASSETS="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/src/Freizeichen.App/Assets"
SVG="$ASSETS/icon.svg"

command -v rsvg-convert >/dev/null || { echo "error: rsvg-convert not found (apt install librsvg2-bin)" >&2; exit 1; }
command -v magick >/dev/null || { echo "error: ImageMagick 7 (magick) not found" >&2; exit 1; }

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

rsvg-convert -w 1024 -h 1024 "$SVG" -o "$ASSETS/icon-1024.png"

# Each .ico frame is rendered from the vector at its own size, not downscaled from the 1024
# render: at 16-32 px, the sizes Explorer and the title bar show, a downscale blurs the handset.
frames=()
for size in 16 20 24 32 40 48 64 128 256; do
    rsvg-convert -w "$size" -h "$size" "$SVG" -o "$STAGE/$size.png"
    frames+=("$STAGE/$size.png")
done
magick "${frames[@]}" "$ASSETS/icon.ico"
