# Changelog

**English** · [Português (Brasil)](CHANGELOG.pt-BR.md) · [Home](README.en.md)

## Android 1.2.0 · 2026-10-26

- Adds opt-in **Link light fixture** in Reef for Smart Timer Wi-Fi. Timeline scrubbing, current-profile edits, and accelerated playback send five interpolated intensities using vendor preview commands `06 01`/`06 02`, without erasing or rewriting profiles or changing the device clock.
- Explicit physical-light confirmation; uploading only opens Reef and offers the feature. A floating stop/resume button stays accessible. Uses a latest-value mailbox, at most five intensity frames per second, and a three-second UI heartbeat deadline.
- Leaving the foreground stops streaming and attempts `03 08` schedule resume, requiring Smart `83` from the selected source and identity. Individual intensities have no acknowledgment/readback, so the UI reports sent values rather than measured light.
- Recovery identity is persisted before the first preview command. Unconfirmed restoration survives app reopening, blocks new writes/preview, and requires reconnecting the same controller. Process death or network loss can leave the light in manual mode; no hardware watchdog is assumed.
- Excludes discovery, uploads, Wi-Fi setup, clock changes, and profile switching during preview. Supports PT-BR, English, and Spanish. Adds native/JavaScript/UI tests and a copyable preview diagnostic without credentials or raw packets.
- Local simulator, real loopback UDP, and browser checks passed; physical controller retest pending. Windows transport/features are unchanged. [Usage and limitations](docs/en/LIVE-PREVIEW.md).

## Android 1.1.0 / Windows 0.6.0 · 2026-10-21

- Adds Brazilian Portuguese, English, and Spanish to both apps. Automatic device language is the default, with English fallback for unsupported languages; a visible selector saves manual preference and allows returning to automatic mode.
- Live switching translates controls, dialogs, confirmations, progress, validation, and communication messages. Existing profile/channel names, schedules, and protocol fields are preserved. Changes wait for network operations/dialogs.
- Shared offline catalog of 496 messages, with synchronization checks, catalog tests, wire-data preservation tests, browser language/persistence checks, and offscreen Windows rendering in all three languages.
- Adds a Spanish README and installation, Wi-Fi, usage, and development guides. Portuguese remains the repository landing page, with English and Spanish links.
- Android retains its signing key and Wi-Fi-only transport. Windows retains its existing USB/legacy Wi-Fi transport; this release does not add the Smart Timer framing adaptation to Windows.
- New builds are locally verified; phone and physical controller testing of this release remain pending. No device commands were sent during automated checks.

## Documentation · 2026-10-19

- Explicit opening disclosure: built with vibe coding and substantial AI assistance to solve a real problem, without a profit motive.
- English becomes the default README, contribution guide, changelog, and documentation path. Brazilian Portuguese remains available through reciprocal language links.
- Existing Android/Windows releases and Portuguese app interfaces are unchanged.

## Android 1.0.6 · 2026-10-19

- Three-second automatic discovery on launch/return; select a sole controller and remember a choice by ID, even when its IP changes.
- Retry 20 seconds after searches without a selected device; recheck after 60 seconds with one selected. Never silently switch to another controller.
- Foreground-only searches wait for operations/dialogs and share exclusive UDP access with commands/uploads. No automatic schedule writes or clock sync.
- Editing/navigation remain available; physical commands wait for the short search. Manual discovery and upload-failure diagnostics remain available.
- 55 general, 34 Smart Timer, 16 editor, and 29 automatic discovery checks; build/signature verified. New behavior needs a phone retest.
- Project capture and human/AI collaboration credits added to README.

## Android 1.0.5 · 2026-10-19

- Addresses confirmed erasure followed by no datagrams for the first profile header.
- Adds a cancelable 2.1-second post-erase wait, a conservative margin rather than a measured firmware timing requirement.
- Allows up to three identical Smart initial-header attempts before any points; never repeats erasure or restarts a partial profile. Unexpected valid statuses still stop immediately.
- Distinguishes confirmed erasure from an erase command merely attempted.
- 55 general, 34 Smart Timer, and 16 editor checks. Loopback tests cover lost first-header ACK after one erase, recovery, and cancellation.
- Hardware cause was unknown at release; subsequent user feedback was positive, without memory readback.

## Android 1.0.4 · 2026-10-19

- After user-confirmed discovery and timeout at point six, adds up to three identical indexed-point attempts only on timeout without a valid reply.
- Raises pacing from 50 to 150 ms and discards queued replies before each request to reduce stale ACK acceptance.
- Adds datagram/status/header/identity diagnostics without raw packets or credentials, available in the last-communication report.
- 55 general, 28 Smart Timer, and 16 editor checks; real loopback UDP tests sixth-point ACK loss, recovery, bounded attempts, and fail-fast behavior.
- Did not automatically retry headers, erase, credentials, clock, or finalizers in this release.

## Android 1.0.3 · 2026-10-19

- Static analysis of the working Smart WiFi Timer 3.0 APK confirms 64-byte requests `55 AA` and replies `88 00`, fixing the confirmed discovery parser mismatch.
- Validates ACK identity at bytes 49-60, versus 4-15 for discovery. Previous non-hex identity speculation was not established physically.
- Detects Smart framing, uses actual profile-name length, and removes the desktop reserved count from the Smart header.
- Uses 50 ms pacing, per-profile `03 08 / 83`, and whole-batch `04 0A / 91`; success requires final ACK.
- Validates/copies the full upload plan before mutation; retains fail-fast behavior without copying vendor password storage or broad mutation retries.
- 55 general, 22 Smart Timer, and 16 editor checks, including full loopback transfers.
- [APK analysis](docs/en/SMART-WIFI-TIMER-ANALYSIS.md); proprietary APK/decompiled code not redistributed.

## Android 1.0.2 · 2026-10-18

- Preserves identity as twelve opaque bytes instead of requiring ASCII hexadecimal; retains existing IDs and wire bytes in commands/provisioning.
- Rejects query-identity echoes, including alternate padding, and empty identities.
- Diagnostics distinguish header, command, size, echo, and empty identity without dumping packets/credentials.
- 55 general Java and 16 editor checks; retained signing key supports updates without uninstalling. At this stage the exact rejection cause still required physical diagnostics.

## Android 1.0.1 · 2026-10-18

- Adds gateway queries alongside subnet/limited broadcasts and an eight-second listening window.
- Accepts valid same-subnet discovery replies from source ports other than 5000, matching the vendor receiver, while retaining frame/identity validation.
- Excludes VPN interfaces and adds a copyable discovery report with local addresses, counts, and rejection summaries, excluding SSID/password/raw packets.
- Distinguishes silence from unrecognized responses; 44 general Java and 16 editor checks. Signing key preserved.

## Initial public preview · 2026-10-18

Windows/Android source, downloads, MIT license, installation/network/usage guides, tests, and CI published together. Public downloads contain generic examples; personal libraries and keys are excluded.

### Windows 0.5.0

Five-channel editor, curves, precision table, ramps, profiles, undo/backups; classic TC420 and Pled V2.2 USB HID paths; single/batch uploads up to 16 slots with confirmation; clock sync; vector reef with corals/clownfish; IPv4/UDP discovery, router setup, and Wi-Fi uploads; simulated transport/UDP/UI tests.

### Android 1.0.0

Android 8+, Wi-Fi only; touch editor, intensity controls, table/cycle wizard; JSON/LMF import and JSON export through the system picker; SVG reef and interpolated daily preview; discovery, router setup, clock, resume, single/batch uploads; Wi-Fi-bound sockets without internet; interruption handling and no automatic write retries in this initial version; Java/JavaScript tests and signed APK.

Hardware installation and physical functions were unvalidated at initial publication. For subsequent evidence, consult the [current compatibility matrix](docs/en/COMPATIBILITY.md).
