# Transfer PC profiles to your phone with QR codes

[Português (Brasil)](../QR.md) · **English** · [Español](../es/QR.md)

Requires **Windows 0.7.0** and **Android 1.3.0** or **iOS 0.2.0**. No account, internet or shared Wi-Fi network is needed. QR codes embed profile/channel names and all 48 five-channel schedule points from the PC library. Router passwords, controller addresses and hardware commands are excluded.

1. On the PC, select a profile and click **Send to phone via QR**. Fix invalid table cells first.
2. The selected profile is the default. Check **All profiles** to transfer the entire library.
3. On your phone, open **Profiles > Scan PC QR**, allow camera access and point at the computer screen.
4. For multipart transfers, use **Next** on the PC or **Cycle QR codes automatically**. Codes repeat at two-second intervals. The phone counts received parts and accepts repeats or out-of-order scans.
5. After all parts pass verification, the camera closes and Lume asks before adding profiles. **Cancel preserves your library.** Confirmation appends profiles, keeping existing ones, within the phone's 100-profile limit.

If scanning is difficult, move closer, avoid screen reflections and enlarge the PC window. Keep the phone pointed at the screen until the counter completes. Scanning expires after fifteen minutes. Cancel before switching between the selected profile and all profiles, because different transfers cannot be mixed.

Camera images are processed locally without saving photos or uploading images. Leaving the app closes the camera. If permission is denied, enable it in app settings or use JSON/LMF import instead.

**Import does not send anything to the light.** Review the profiles on the phone and upload them separately when ready. The source is the local PC library, not a readback of controller memory. JSON backups remain available.

iOS still requires Apple signing and iOS 15.4+. [iOS guide](INSTALL-IOS.md). Physical phone camera validation is pending. Automated tests cover actual Windows-generated QR images decoded by the Android engine, data reassembly, confirmation/cancellation and three languages. They do not validate real camera focus or lighting.
