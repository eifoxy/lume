# Lume for iPhone and iPad

[Português (Brasil)](../INSTALACAO-IOS.md) · **English** · [Español](../es/INSTALACION-IOS.md)

[← Lume](../../README.en.md)

**0.1.1 is an unsigned development preview for iOS/iPadOS 15.4+.** The offline editor and native Wi-Fi implementation are included. GitHub Actions uses a Mac to compile and run automated tests. Real iPhone/controller validation is still pending. There is no App Store or TestFlight release.

The `UNSIGNED.ipa` **cannot be installed as downloaded**. It needs Apple signing. The Simulator ZIP is for Xcode on a Mac, not a phone. [Builds and artifacts](https://github.com/eifoxy/lume/actions/workflows/ios.yml). Without access to a Mac or Apple signing, use Windows/Android for now. The project does not create accounts, enroll you in a paid program, or buy services.

## Older iPhones

Version 0.1.1 lowers the deployment target to **iOS 15.4**, covering updated iPhone 7/7 Plus, 6s and first-generation SE devices. iOS 15.0 through 15.3 and devices limited to iOS 12 are unsupported. iOS 15.4 provides the editor's native dialog support; translation no longer requires the RegExp lookbehind introduced in iOS 16.4. Wi-Fi functions and all three languages remain implemented. [Apple Safari 15.4 notes](https://developer.apple.com/documentation/safari-release-notes/safari-15_4-release-notes).

Compilation checks the 15.4 deployment target, with regression tests for older JavaScript capabilities and small-screen layout checks. The available GitHub simulator runs a newer OS. **No iPhone 7, A10 hardware or iOS 15 runtime test has been performed.** Reef performance and controller communication on an older phone still need physical validation. Apple signing is still required.

## Implemented features

Five-channel curves, 48 daily points, cycle generation, profile management, JSON/LMF import, JSON export through Files, local atomic saves and backups. The reef animation and day simulation share the Android editor. Portuguese, English and Spanish follow device preferences, with a manual selector. Native networking includes automatic discovery/reconnection in the foreground, known-IP search, router provisioning, single or replacement uploads of up to 16 profiles, clock synchronization, program resume and Smart Timer live preview.

Implementation does not establish physical compatibility. Standard TC420 units without Wi-Fi remain unsupported wirelessly. Smart Timer and legacy Pled paths still need iOS hardware validation. There is no USB transport, full controller memory readback, or remote/cloud control.

## Development installation

1. Get the source and open `ios/Lume.xcodeproj` in a compatible Xcode on a Mac.
2. Choose the **Lume** scheme and your device. In **Signing & Capabilities**, select your team and a unique bundle identifier.
3. Sign into Xcode with your Apple Account. A free **Personal Team** supports limited personal testing; provisioning expires after seven days and requires rebuilding/reinstalling. Paid membership is not required for this personal test. [Apple account rules](https://developer.apple.com/help/account/basics/about-your-developer-account).
4. Enable Developer Mode when prompted and run. Export your library before removing the app or changing its identifier.

TestFlight/App Store distribution requires the responsible team to enroll in the appropriate Apple program and configure signing and App Store Connect. That distribution has not been set up. [Apple programs](https://developer.apple.com/help/account/membership/programs-overview/).

## Connect to the controller

1. Open **Settings > Wi-Fi**, join the controller network, and remain connected without internet.
2. Open Lume and allow **Local Network**. Enable it in app permissions if previously denied. Lume's Wi-Fi helper explains these steps and can open app permissions; it does not open a private Wi-Fi settings URL.
3. Wait for discovery, search manually, or try the known controller IP. On the `192.168.4.x` device network, try `192.168.4.1`.
4. Select the controller and submit your home Wi-Fi credentials. Legacy modules usually require 2.4 GHz, WPA2/AES and ASCII SSID/password. The protocol accepts 8 to 32 password characters. Lume does not save the password.
5. Join your home Wi-Fi on the phone, return to Lume, discover the controller, synchronize the clock and upload your schedule. Replacing all profiles requires confirmation.

### Discovery limits

This version uses **bounded unicast queries**, avoiding Apple's special broadcast/multicast entitlement. It binds IPv4 UDP 5000 to the Wi-Fi interface, with no cellular fallback. It probes a cached address, the usual access-point address and up to 254 nearby addresses, filtered by the current subnet, within 14 seconds. On networks larger than `/24`, use a known IP for a controller outside that group. Client isolation can prevent communication.

Firmware support for unicast has not been confirmed on a physical iPhone. Broadcast-only devices may need a future version with Apple's entitlement. Entering an IP still requires an actual discovery response and controller identity. [Apple local network guidance](https://developer.apple.com/documentation/technotes/tn3179-understanding-local-network-privacy).

## Live light preview

Link a Smart Timer from **Reef** and confirm. Scrubbing or playing the day changes the actual five-channel light, including accelerated playback, at up to five updates per second. Individual intensities have no acknowledgment or physical light readback. Simulation does not predict coral biology.

Stopping or leaving the foreground attempts to resume the schedule. The recovery marker clears only after acknowledgment from the same controller. A short background task is used for cleanup, not continuous background operation. Network loss or force-quitting may leave the last manual light state. Reconnect the same device and use **Resume program**. Preview does not erase or rewrite profiles.

## Privacy and validation

No Lume account, telemetry, ads or control server. The editor works offline; commands stay on the local network. Libraries, preferences and recovery state remain in app storage, subject to system backup policies. Diagnostics contain local IPs and counters, without SSID/password, and are shared only by user action. Export JSON before uninstalling.

On a Mac, run `node scripts/sync-ios-assets.cjs --check`, `node scripts/generate-ios-project.cjs --check`, `node ios/Tests/bridge-test.cjs`, and `swift test --package-path ios`. Open the project to build, or use the [workflow](../../.github/workflows/ios.yml), which also builds an unsigned device app and packages SHA-256 checksums. Network mocks and loopback tests do not replace real permission and controller testing.
