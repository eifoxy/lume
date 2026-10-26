# Live light preview over Wi-Fi

[Home](../../README.en.md) · [Português (Brasil)](../PREVIA-AO-VIVO.md) · **English** · [Español](../es/VISTA-EN-VIVO.md)

Available in **Android 1.2.0** for controllers identified as **Smart Timer**, with `88 00` replies. Commands were derived from the user-supplied working APK and tested locally with simulators. Physical light behavior still needs a hardware retest.

## Use

1. Join the controller's Wi-Fi or the same home network and wait for discovery.
2. Open your desired profile. Upload it normally if you want to save it on the controller. After an acknowledged upload, the app opens Reef.
3. In **Reef**, choose **Link light fixture** and confirm the physical light change. Uploading does not automatically activate live preview.
4. Scrub the timeline or choose **Play day**. The device receives the same five interpolated channel intensities shown in the Reef. Pausing playback holds the current moment while live preview remains active.
5. Choose **End preview and resume schedule**, wait for the resume acknowledgment, and check the active profile on the device.

You can also edit the current profile's points while testing. Edits remain local saves; preview does not rewrite the controller schedule. Changing profiles, uploading, configuring Wi-Fi, synchronizing the clock, and discovery are blocked during the session.

## Sent values are not readback

Vendor preview commands do not await individual acknowledgments. **Last sent** reports a datagram sent by the phone, not a measurement or proof of reception. UDP can drop packets and the light can lag behind the screen.

A latest-value mailbox coalesces changes and sends at most five intensity frames per second. Unchanged values are not continually resent. The UI supplies a local heartbeat every 200 ms; after more than three seconds without updates, the session attempts to stop and resume the schedule.

## Backgrounding and recovery

Leaving the foreground stops streaming and attempts `03 08` restoration, requiring a Smart `83` status from the correct source and identity within 2.5 seconds. There is no background preview service or automatic preview restart.

If the network fails, the OS kills the process, or the acknowledgment is missing, **the fixture can remain in manual mode at its last received intensity**. No confirmed hardware watchdog allows a guarantee of automatic recovery without communication.

Before the first command, the app persists the session identity. A pending recovery warning survives reopening. Reconnect the **same controller**, rediscover it, and choose **Resume schedule on device**. New previews, uploads, network setup, and clock changes wait for recovery. This record contains no password or profile library.

The last communication diagnostic records the local destination, frames sent, and resume acknowledgment, without profile names, SSID, passwords, or raw packets.

## Protocol

- Start: `06 01`, name length at byte 5 and name at byte 6. Two identical sends, 300 ms apart, matching the vendor APK.
- Intensities: `06 02`, byte 5 `05`, five values 0 to 100 at bytes 6 to 10.
- 64-byte requests with `55 AA`, identity at 49 to 60, checksum at 61.
- Exit: `03 08`, Smart `88 00` reply with status `83` and matching identity.
- No erasure, profile writes, provisioning, or clock commands during preview.

This does not add Wi-Fi preview to Windows or legacy Pled devices. Windows retains USB instant adjustment. [APK analysis](SMART-WIFI-TIMER-ANALYSIS.md) · [Compatibility](COMPATIBILITY.md).
