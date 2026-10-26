# Compatibility and validation

[Home](../../README.en.md) · **English** · [Português (Brasil)](../COMPATIBILIDADE.md)

## Platforms

| Platform | Requirements | Evidence |
|---|---|---|
| Full Windows package | Windows 10/11 x64, runtime included | Built and tested locally. |
| Small Windows package | Windows with .NET Desktop Runtime 8 | Built and tested locally. |
| Android | Android 8+, minimum API 26, target API 36 | Build/signature verified. User tested Samsung SM-S948B/API 37, confirmed discovery, and reported good results after 1.0.5. Automatic discovery in 1.0.6 still needs a phone retest. |

## Controllers and transports

| Device/protocol | Windows | Android | Evidence and limits |
|---|---|---|---|
| Classic TC420, HID `0888:4000` | USB implemented | Unsupported | Reference protocol and simulated transport tests; physical commands pending. |
| Pled V2.2 variant, HID `0483:5750` | USB implemented | Unsupported | HID detected on development PC, supplied executable analyzed; physical writing pending. |
| Desktop Pled V2.2 Wi-Fi protocol | Experimental path | Legacy path retained | Simulator only; physical compatibility unconfirmed. |
| Smart WiFi Timer, replies `88 00` | Not adapted in Windows 0.5.0 | Supported since 1.0.3 | User-confirmed discovery, positive feedback after upload fixes in 1.0.5; no memory readback. |
| TC420 without Wi-Fi hardware | Compatible USB only | Unsupported | Software cannot add a radio. |
| Other modules/firmware | No guarantee | No guarantee | A product name alone does not establish protocol compatibility. |

## Physical functions

| Function | Implementation | Physical verification |
|---|---|---|
| Windows USB discovery | HID enumeration | Controller detected. |
| Wi-Fi discovery | UDP broadcast/direct IP | User-confirmed Android discovery; Windows unconfirmed. Automatic discovery tested with simulated events, phone retest pending. |
| SSID/password setup | Two packets with separate ACKs | Separate physical confirmation pending. |
| Individual profile upload | Header + 48 points, command ACKs | Positive general feedback after 1.0.5; no independent memory readback or detailed per-function validation. |
| Erase and upload all | One erase, sequential profiles | Same limitation: general feedback does not establish every function separately. |
| Clock sync / resume | Local date/time / manual-mode exit | Separate physical confirmation pending. |
| Instant physical intensities | Windows USB only | Pending; not implemented over Wi-Fi. |
| Read memory / device backup | Not implemented | Local backups contain the app library, not device memory. |

## Local checks

- **Languages, Android 1.1.0 and Windows 0.6.0:** 496 shared messages in PT-BR, English, and Spanish. Catalog tests cover every entry in each language; interface checks cover automatic/manual selection, persistence, unsupported system languages, and preservation of library data and profile bytes. Android was also reviewed in a browser; Windows was rendered offscreen in all three languages. These builds still need a phone/controller retest. Windows 0.6.0 inherits 0.5.0 transports and still lacks Android's Smart Timer adaptation.
- **Windows:** 77 functional checks for models, files, packets, transfers, errors, and UDP; 24 offscreen UI checks.
- **Android 1.0.6:** 55 general Java checks, 34 Smart Timer checks, 16 editor checks, and 29 automatic discovery checks. Real loopback UDP covers clock, fictitious credentials, complete profiles, and lost acknowledgments at the sixth point and first header after erasure. Simulated events/timers cover startup/return, changed IP, lost/recovered devices, multiple controllers, persistent choice, and operation/dialog exclusion.
- **APK:** javac/D8/aapt2 build, alignment, and v2/v3 signatures verified.
- **Android UI:** browser review at phone width, including reload persistence, cycles, curves, reef, and connection.

These results do not constitute real Android radio testing, device memory readback, or firmware validation. Loopback tests use fictitious data and do not modify the physical controller.

## Hardware feedback history

1. Initial Android search made the device beep without listing it. A beep alone did not confirm discovery.
2. In 1.0.1, diagnostics showed received 64-byte discovery packets, rejected by the parser. The network was receiving; the differing field was still unknown.
3. In 1.0.2, diagnostics revealed `88 00`. Static analysis of the working vendor APK confirmed this reply header. Earlier non-hex identity speculation was not established on the hardware.
4. In 1.0.3, the user confirmed discovery but reported an upload timeout at point six after five confirmed points. This established partial communication, not complete memory readback.
5. Version 1.0.4 added bounded point retries, slower pacing, and diagnostics. The next report showed confirmed erasure followed by silence at the first profile header.
6. Version 1.0.5 added a post-erase wait and bounded initial-header retries. The user then reported good results. This does not establish the original cause of silence or replace readback.
7. Version 1.0.6 retains that upload flow and adds automatic discovery; physical retesting of the new behavior is pending.

See the [protocol analysis](SMART-WIFI-TIMER-ANALYSIS.md) for packet-level evidence.

## Limits

Five channels, 48 half-hour points, integer intensities 0-100; 100 local profiles and 16 upload slots. Device names use at most seven ASCII characters. Local IPv4/UDP 5000 only, with no remote internet control. SSID 1-32 and password 8-32 ASCII characters; WPA/WPA2 and AES/TKIP; no WPA3.

No full memory readback, physical rollback, firmware updates, Bluetooth discovery, Android USB, or cloud telemetry. A failure after erasure can leave device memory empty or partial. Automatic upload recovery is bounded to the same indexed Smart point or initial header before points begin, only for timeout without a valid reply, up to three total attempts. Erasure is never automatically repeated.

## Wi-Fi preview, Android 1.2.0

Implemented only for Smart Timer, using vendor APK `06 01`/`06 02` commands. Intensities have no individual ACK; `03 08` resume requires Smart `83` and matching identity. Fixtures, latest-value mailbox, cancellation, heartbeat expiry, restoration failures, and real loopback UDP passed. UI was tested in all three languages with a simulated native bridge. Physical light behavior remains pending a hardware test. Windows does not receive this feature. [Usage and limits](LIVE-PREVIEW.md).
