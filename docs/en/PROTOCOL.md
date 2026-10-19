# Architecture and protocol

[Home](../../README.md) · **English** · [Português (Brasil)](../PROTOCOLO.md) · [Validation](COMPATIBILITY.md)

## Smart WiFi Timer

Since Android 1.0.3, Lume detects 64-byte `88 00` replies and selects the Smart Timer format. Requests remain `55 AA`. Discovery identity is at bytes 4–15, acknowledgment identity at 49–60. Profile headers use actual name length; uploads finalize each profile with `03 08` and the batch with `04 0A`. See the [full APK analysis](SMART-WIFI-TIMER-ANALYSIS.md) for offsets, checksums, credentials, statuses, and sequencing.

The response header was observed in physical diagnostics. Through 1.0.2, Lume incorrectly expected `55 AA` in replies. The desktop format remains a separate legacy path; Windows 0.5.0 has not received the Smart Timer adaptation.

## Implementation

Windows uses C#/WPF/.NET 8, native HID, and IPv4/UDP sockets. `Transfer.cs` creates a validated library copy and separates transport from execution. `Wifi.cs` handles discovery, credentials, and sessions; `LedSimulation.cs` draws the vector reef.

Android uses a Java Activity, `AtomicFile` persistence, the system document picker, and bundled HTML/CSS/JS/SVG in a WebView. The JavaScript bridge is limited to the local UI; external navigation/loading is blocked. `Protocol.java` encodes frames and `PacketIO.java` handles exchanges. `Network.bindSocket` binds sockets to Wi-Fi, including networks without internet; discovery holds a `MulticastLock`. Network operations run outside the UI thread.

Both apps save JSON with `Version`, `Profiles`, and `Selected`. Each profile has `Name`, five `Channels`, and 48 `Values` rows of five integers. Saves are atomic and invalid files are preserved. Editing and simulation issue no hardware commands.

Android 1.0.6 uses foreground lifecycle events and UI timers for automatic discovery. Preferences store the chosen identifier, not a persistent IP or router credentials. Automatic and manual discovery share the native operation gate with uploads/commands. They never run concurrently with a transfer. No background service is used.

## Legacy desktop Pled V2.2 Wi-Fi path

This description comes from the supplied executable and simulation, not an official specification or proof of physical compatibility.

- Local and remote IPv4/UDP port **5000**. One socket stays open through each exchange.
- Frame length 64 bytes: `55 AA`, payload at 2, 12-byte identity at 49–60, checksum at 61.
- Common checksum: `0xFF + sum(payload)` modulo 256, excluding identity.
- Discovery prefix: `55 AA 17 00 00 03 DD EE FF`, checksum byte `E3`; reply identity at 4–15, IP from datagram source.
- Legacy ACK: `55 AA`, command echo at 2, status at 4. Transport checks source, port, length, and status; command timeout is 2.5 seconds.
- Android discovery accepts valid same-subnet replies from any source port, as the vendor receiver does. `Discovery.java` builds gateway/subnet/limited-broadcast destinations and rejects self/network/broadcast addresses and outside origins. Write ACK filtering remains strict.
- Android preserves identity as 12 opaque bytes. `raw:` plus 24 hex digits serializes these in JSON, decoded back to the original bytes on the wire. Existing ASCII-hex IDs remain supported. Windows 0.5.0 still assumes ASCII hexadecimal identity.

| Operation | Legacy payload | ACK |
|---|---|---|
| Profile header | `01 01 00 0A`, zero-based slot, seven ASCII name bytes padded with spaces, `00 30` | `80` |
| Point | `01 02 00 08`, index 0–47, hour, minute, five intensities | `81` |
| Erase profiles | `05 FE` | `82` |
| Resume | `03 08` | `83` |
| Clock | `02 00 00 07`, year high/low, month, day, hour, minute, second | `85` |

Credentials use special `18` frames, index 1/2, text at 7, authentication at 39, and cipher at 40 for SSID. Checksum is the sum of bytes 0–48 modulo 256. Legacy SSID accepts `70`/`74`; password accepts `72`. Credential buffers are cleared after use and passwords are not persisted.

A full replacement validates the entire library, saves a local backup, erases once, and sends profiles sequentially. It stops on failure and reports completion count. No readback or automatic restoration exists. Current Android Smart retry/finalization policy is described separately in [Usage](USAGE.md#upload-profiles).

## USB and file formats

HID identity selects two separate USB protocols: Pled V2.2 `0483:5750` or classic TC420 `0888:4000`. The classic device uses different framing/commands; see `Usb.cs`, `Transfer.cs`, and [references](REFERENCES.md). Android has no USB transport.

Supported LMF: 960 bytes = 48 × 5 × four little-endian bytes. The low byte stores intensity with swapped nibbles; three high bytes must be zero. LCM contains NUL-separated local paths with nibble-swapped bytes. Only Windows imports LCM because it relies on those referenced files.
