# Smart WiFi Timer 3.0 analysis

[Home](../../README.en.md) · **English** · [Português (Brasil)](../ANALISE-SMART-WIFI-TIMER.md)

Static analysis performed on **2026-10-19** using the APK the user reported working with their controller. JADX was used for decompilation; the APK was **not installed or executed** during analysis. The APK and decompiled code are excluded from the repository. Lume implements the protocol fields and sequence documented here.

## Analyzed material

| Field | Value |
|---|---|
| Supplied file | `Smart+WifiTimer_3.0_APKPure.apk` |
| Package | `com.leyview.timercontroller` |
| Manifest name | `SmartTimeControl` |
| Version | 3.0, versionCode 3 |
| Minimum / target SDK | 15 / 26 |
| SHA-256 | `c2462bcd50cce0abb7f76d29cc616642a91a881aa64a0026ffe6bf21228dce7a` |

Relevant classes under `com.leyview.item1`: `operation.Communication`, `operation.MyProtocol`, `operation.Common`, and `timercontroller.DeviceDetailActivity`. These names identify analysis locations, not redistributed source.

## Confirmed discovery rejection cause

| Direction | Header | Length |
|---|---|---|
| App → controller | `55 AA` | 64 bytes |
| Controller → app | `88 00` | 64 bytes |

The APK's acknowledgment parser requires `88 00`. Physical diagnostics showed the same header with 64-byte discovery replies, command 23 decimal. Lume through Android 1.0.2 incorrectly required `55 AA` in responses and rejected these packets before reading identity.

Earlier source-port and identity changes could not fix this header mismatch. Non-hex identity was not established on the hardware; opaque identity support remains to avoid lossy conversions, but the direct evidence here is the response header.

## Discovery

1. `Communication` opens a local UDP socket on **5000** with a receive thread.
2. `MyProtocol.SearchDevice` creates a zeroed 64-byte frame with prefix `55 AA 17 00 00 03 DD EE FF`.
3. Byte **61** is the sum of bytes **0-48** modulo 256: `E3` for discovery.
4. It queries **255.255.255.255:5000**, repeating after 300 ms. These are discovery queries, not schedule writes.
5. Replies with command **0x17** (23 decimal) are treated as devices during discovery.
6. `Common.GetMacFromSearch` extracts twelve identity bytes at **4-15**.
7. IP comes from the datagram source; identities deduplicate the list.

The decompiled preliminary `Communication.CheckReceive` condition is permissive. `MyProtocol.ReceiveData` clearly requires both `88 00` bytes. Lume uses explicit confirmed framing and retains rejection of wrong networks, query echoes, empty identities, and wrong lengths.

Lume also queries the gateway and subnet broadcast, and explicitly binds reception to the phone's Wi-Fi network even without internet.

## Command acknowledgments

`MyProtocol.ReceiveData` checks 64 bytes, `88 00`, status byte **4**, and device identity at **49-60** matching the destination. Unlike discovery, identity is at the end of the frame. Smart ACK acceptance does not require a command echo at byte 2. Lume also filters source IP and port.

The vendor app waits about one second and can try commands up to three times. Lume uses a 2.5-second timeout. Its current bounded retry policy is deliberately narrower: up to three identical attempts for the same indexed point or the initial profile header before any points, only on timeout without a valid device reply. Erasure, credentials, clock, and finalizers are not automatically repeated. Partial profiles are not restarted.

## Outgoing frame and checksum

All offsets are zero-based; all frames are 64 bytes.

| Offset | Meaning |
|---|---|
| 0-1 | `55 AA` |
| 2 | Command |
| 3 | Subcommand where applicable |
| 4 | Reserved, normally zero |
| 5 | Content length where applicable |
| 6 onward | Command data |
| 49-60 | Device identity |
| 61 | Sum of bytes 0-48, modulo 256 |
| 62-63 | Zero in the analyzed APK |

Lume's common-frame calculation `0xFF + sum(payload)` is equivalent because `0x55 + 0xAA = 0xFF` and the remaining area is zeroed. Identity is excluded from the checksum.

## Router provisioning

`MyProtocol.SetWifi` sends command **0x18** in two frames for a protected network:

| Field | SSID | Password |
|---|---|---|
| Byte 5 | SSID byte length | Password byte length |
| Byte 6 | `01` | `02` |
| Byte 7 onward | SSID text | Password text |
| Byte 39 | `01` WPA or `02` WPA2 | Zero |
| Byte 40 | `01` AES or `02` TKIP | Zero |
| Expected status at byte 4 | `70` | `72` |

The vendor UI caps text at 32 bytes and may truncate. Lume uses explicit limits and rejects unsupported input without truncation. The APK's open-network variant uses byte 6 `03`, SSID text, and ACK `74`; open-network setup was not added to Lume's UI.

Unlike the vendor database, Lume does not save Wi-Fi passwords. Credential buffers are cleared after an attempt. If final acknowledgment is absent after switching networks, the UI advises searching from the router before repeating setup.

## Clock, erase, and finalizers

- Clock: command **02**, length **07**, seven bytes starting at 6: year high/low, month, day, hour, minute, second; ACK **85**.
- Finish a profile / exit preview: **03 08**, ACK **83**.
- Finish the batch: **04 0A**, ACK **91**.
- Erase profiles: **05 FE**, ACK **82**.

Function names alone do not establish every firmware effect. Check physical activation/resume behavior on the device.

## Schedule transfer

The APK's `StartDownload` differs from the fixed desktop Pled header:

- Bytes 2/3: `01 01`; byte 5: actual name byte count; byte 6: zero-based profile slot.
- Name starts at byte 7, without an appended `48` count. ACK **80**.

`DownloadProcess` sends 48 indexed points:

- Bytes 2/3: `01 02`; byte 5: `08`; byte 6: index 0-47.
- Bytes 7-8: hour/minute, with minutes 00 or 30; bytes 9-13: five intensities. ACK **81**.

The vendor UI waits **50 ms** after each point ACK, then uses **03 08 / 83** after 48 points and **04 0A / 91** after the batch. Lume detects Smart format from discovery framing and uses these finalizers. It retains seven-character names, a compatible subset of the vendor UI's 12-byte limit. Full replacement erases once after explicit confirmation; individual uploads do not erase all memory.

## Implementation history and verification

Version 1.0.3 fixed discovery/ACK parsers, identity offsets, actual name length, and both finalizers, while retaining whole-plan validation, local backup, and fail-fast behavior. Its initial local suite had 55 general, 22 Smart Timer, and 16 editor checks. UDP integration used real loopback sockets with fictitious credentials and a complete 48-point profile, including negative identity/status/header/length, cancellation, mid-transfer failure, and missing-final-ACK cases.

After user-confirmed discovery and a timeout at point six, **1.0.4** increased pacing to 150 ms and added bounded indexed-point recovery and communication diagnostics. A loopback test drops that point's first ACK and verifies recovery.

After confirmed erase followed by silence at the first header, **1.0.5** added a cancelable 2.1-second post-erase wait and bounded initial-header retries. The vendor UI performs erasure and transfer as separate user actions; the wait is a conservative margin, not a measured firmware requirement. The simulation verifies one erase, identical header retry, and successful completion. The user subsequently reported good results, without proving the original cause of silence or providing memory readback.

**1.0.6** retains this upload path and adds foreground automatic discovery. Current local checks: 55 general, 34 Smart Timer, 16 editor, and 29 automatic discovery. New discovery behavior still needs a phone retest. Windows 0.5.0 has not received the Smart Timer transport adaptation. See [current validation evidence](COMPATIBILITY.md).
