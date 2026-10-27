# Third-party notices

Freizeichen itself is released under the [MIT licence](LICENSE). The installers (MSI and .dmg)
also contain the third-party components listed below, each under its own licence. This file and
the font licence ship next to the program.

## Fonts

Bundled as application resources, unmodified except that the Oswald files are static weight
instances of the upstream variable font (see [`Assets/Fonts/README.md`](src/Freizeichen.App/Assets/Fonts/README.md)).
All three families are under the **SIL Open Font License 1.1**; the full text, with every
copyright line, is in [`OFL.txt`](src/Freizeichen.App/Assets/Fonts/OFL.txt).

| Family | Copyright | Source |
|---|---|---|
| Oswald | Copyright 2016 The Oswald Project Authors | <https://github.com/googlefonts/OswaldFont> |
| Barlow | Copyright 2017 The Barlow Project Authors | <https://github.com/jpt/barlow> |
| JetBrains Mono | Copyright 2020 The JetBrains Mono Project Authors | <https://github.com/JetBrains/JetBrainsMono> |

## Libraries

Pulled in as NuGet packages; exact versions are pinned in `Directory.Packages.props` and the
`packages.lock.json` files.

| Component | Licence | Source |
|---|---|---|
| .NET runtime (self-contained) | MIT | <https://github.com/dotnet/runtime> |
| System.IO.Ports | MIT | <https://github.com/dotnet/runtime> |
| CommunityToolkit.Mvvm | MIT | <https://github.com/CommunityToolkit/dotnet> |
| Avalonia (incl. Desktop, Skia, Fluent theme, platform backends) | MIT | <https://github.com/AvaloniaUI/Avalonia> |
| Avalonia.Angle.Windows.Natives (ANGLE) | BSD-3-Clause | <https://github.com/google/angle> |
| SkiaSharp | MIT; native Skia: BSD-3-Clause | <https://github.com/mono/SkiaSharp> |
| HarfBuzzSharp | MIT; native HarfBuzz: MIT ("Old MIT") | <https://github.com/mono/SkiaSharp> |
| MicroCom.Runtime | MIT | <https://github.com/kekekeks/MicroCom> |
| Tmds.DBus.Protocol | MIT | <https://github.com/tmds/Tmds.DBus> |

The .NET runtime carries its own third-party notices for the code it incorporates:
<https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT>.

Build- and test-only tools (StyleCop.Analyzers, xUnit, coverlet, WiX) are not shipped.
