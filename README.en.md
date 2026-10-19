# ◒ Lume

[Português (Brasil)](README.md) · **English**

> **Built with vibe coding to solve a real problem, with no profit motive.** Lume is a free, independent, open-source personal project created to make an old aquarium LED controller easier to use. It was developed with substantial AI assistance from OpenAI Codex, guided by a real user's requirements and feedback from their controller.

**A day of light, from sunrise to sunset.**

![Lume's reef interface, with corals and clownfish](docs/images/recife-dia.png)

An aquarium lighting editor for compatible TC420 controllers and Pled V2.2 Wi-Fi modules. **Windows** and **Android** apps with five-channel curves, lighting profiles, and an animated reef that follows your schedule. The released app interfaces currently use Brazilian Portuguese; this repository opens in Brazilian Portuguese, with this English version and English guides also available.

[![Checks](https://github.com/eifoxy/lume/actions/workflows/checks.yml/badge.svg)](https://github.com/eifoxy/lume/actions/workflows/checks.yml)
[![MIT license](https://img.shields.io/badge/license-MIT-91e3c7)](LICENSE)

> **Development preview:** the user confirmed Android discovery and reported good results after 1.0.5. Version 1.0.6 adds automatic discovery and reconnection, verified in local tests; these new functions still need a phone retest. See the [compatibility matrix](docs/en/COMPATIBILITY.md) for evidence and limits.

## Download and install

| App | Download | Installation |
|---|---|---|
| Windows x64 · 0.5.0 | [Full ZIP, .NET included](https://github.com/eifoxy/lume/releases/download/preview-2026.10.18/Lume-Windows-0.5.0-win-x64.zip) | Extract everything and open `Lume.exe`. |
| Windows · 0.5.0 | [Small ZIP, requires .NET Desktop Runtime 8](https://github.com/eifoxy/lume/releases/download/preview-2026.10.18/Lume-Windows-0.5.0-portable.zip) | Install the runtime, extract everything, and open `Lume.exe`. |
| Android 8+ · 1.0.6 | [APK](https://github.com/eifoxy/lume/releases/download/android-v1.0.6/Lume-Android-1.0.6.apk) | Download on your phone and open it in the file manager. |

[All releases and checksums](https://github.com/eifoxy/lume/releases) · [Windows guide](docs/en/INSTALL-WINDOWS.md) · [Android guide](docs/en/INSTALL-ANDROID.md) · [Wi-Fi setup](docs/en/WIFI.md)

On Android, install updates **over the existing app, without uninstalling**, to keep your profiles.

## Features

- **Five channels, 48 points per day:** integer intensities from 0 to 100%, with preview interpolation between points and across midnight.
- **Readable curves:** select channels and times, then edit with sliders or numbers. Windows also supports drawing, enlarging, and isolating curves.
- **Smooth daily cycles:** create sunrise, peak light, and sunset using adjustable times, ramps, and channel peaks.
- **Local profiles:** create, rename, duplicate, delete, undo, and back up. Import Lume JSON libraries and Pled V2.2 `.lmf` files.
- **Individual or batch uploads:** write one slot or replace all memory with up to 16 profiles, with review and explicit confirmation before erasing.
- **Wi-Fi:** local discovery, direct IP lookup, initial router setup, profile uploads, clock synchronization, and resume commands.
- **Automatic connection on Android:** discover on launch/return, select a sole controller, and recover the previous choice by identifier even if its IP changes. Retry when it stops responding.
- **Animated reef:** corals open and close with simulated light, while three clownfish swim through the aquarium.
- **Recoverable local saves:** atomic library writes and a local copy before uploads. This backup does not read controller memory.
- **No account or cloud:** the editor works offline; device communication uses the local network, or USB on Windows.

### Platform comparison

| Feature | Windows | Android |
|---|:---:|:---:|
| Curves, table, cycles, five channels, profiles | ✓ | ✓ |
| Animated reef and daily preview | ✓ | ✓ |
| Wi-Fi discovery and setup | ✓ | ✓ |
| Profile uploads and clock sync over Wi-Fi | ✓ | ✓ |
| Automatic discovery and reconnection | - | ✓ |
| USB HID | ✓ | - |
| Instant physical LED adjustment | USB, experimental | - |
| `.lmf` / JSON import | ✓ | ✓ |
| `.lcm` project import | ✓ | - |
| `.lmf` / JSON export | ✓ | JSON |

## Reef preview

<img src="docs/images/recife-celular.png" alt="Android interface preview at phone width, with corals open at noon" width="320" />

*Browser capture of the Android interface at phone width, not a capture from a physical Android device. The reef is illustrative: it does not measure PAR, spectrum, power, or animal response, and does not change the physical LEDs.*

## Start with Wi-Fi

1. Join the controller's Wi-Fi network on your PC or phone. Stay connected even if it has no internet.
2. On Android, wait for automatic discovery and choose a controller if there are several. On Windows, open the Wi-Fi connection dialog and search.
3. Send your home network name and password using security settings supported by the router and module.
4. Join that home network. Return to Lume on Android for automatic discovery; search again on Windows.
5. Synchronize the clock and upload your reviewed profiles.

This requires **compatible Wi-Fi hardware**. A standard TC420 without a Wi-Fi module cannot gain wireless connectivity through software. Android uses Wi-Fi exclusively. [Setup and troubleshooting →](docs/en/WIFI.md)

## Documentation

- [Install and use on Windows](docs/en/INSTALL-WINDOWS.md)
- [Install and use on Android](docs/en/INSTALL-ANDROID.md)
- [Wi-Fi setup and discovery](docs/en/WIFI.md)
- [Curves, profiles, and uploads](docs/en/USAGE.md)
- [Compatibility, limits, and test evidence](docs/en/COMPATIBILITY.md)
- [Build and test](docs/en/DEVELOPMENT.md)
- [Architecture and protocol](docs/en/PROTOCOL.md)
- [Analysis of the working Smart WiFi Timer APK](docs/en/SMART-WIFI-TIMER-ANALYSIS.md)
- [References](docs/en/REFERENCES.md)
- [Changelog](CHANGELOG.md)
- [Contributing and reporting problems](CONTRIBUTING.md)
- [All documentation in Brazilian Portuguese](README.pt-BR.md)

## Source and license

```text
windows/src/          WPF / C# / .NET 8 app
android/app/          Native Java + local WebView / SVG interface
android/tests/        Protocol, UDP, editor, and reconnection tests
docs/en/             English guides
docs/                Brazilian Portuguese guides and shared images
scripts/             Build and verification scripts
.github/workflows/   Automated checks
```

Lume's code and artwork use the [MIT license](LICENSE). The project is free to download and maintained without a profit motive; that describes its purpose and does not add restrictions to the MIT license. Proprietary Pled binaries, third-party decompiled code, signing keys, and personal profile libraries are excluded. The public Android library contains only an illustrative example.

This is an independent project with no affiliation to controller manufacturers. Platform documentation and protocol references are listed in [References](docs/en/REFERENCES.md).

## Author and AI collaboration

Created and maintained by [eifoxy](https://github.com/eifoxy), who supplied the idea, requirements, and hardware feedback. **Codex, OpenAI's AI assistant**, contributed Windows and Android implementation, protocol analysis, automated tests, and documentation. This credits AI assistance; it does not represent a personal GitHub account or official OpenAI endorsement.
