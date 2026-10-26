# Install on Android

[Home](../../README.en.md) · **English** · [Português (Brasil)](../INSTALACAO-ANDROID.md) · [Wi-Fi setup](WIFI.md) · [Usage](USAGE.md)

## Requirements

- Android **8.0 or newer** (API 26), with an up-to-date Android System WebView.
- Phone Wi-Fi and a controller with a compatible Pled V2.2/Smart Timer Wi-Fi module.

No cable, computer, account, or cloud service is required. Android has no USB transport. A standard TC420 without a Wi-Fi module cannot connect through this app. The app supports Portuguese, English, and Spanish, automatically or through the Language selector. Examples below retain Portuguese button names; select Português (Brasil) to follow them exactly.

## Install the APK

1. Open the [Android release](https://github.com/eifoxy/lume/releases/tag/android-v1.2.0) on your phone.
2. Download **`Lume-Android-1.2.0.apk`** under **Assets**. For updates, install over the previous version **without uninstalling** to keep your profiles.
3. Open the downloaded file in your file manager or download notification.
4. If prompted, allow **that browser or file manager** to install apps. The setting name varies by manufacturer.
5. Tap **Install** and open **Lume**.

The APK is distributed on GitHub, not Google Play, and signed with the project's retained signing key. USB debugging is unnecessary. Compare its SHA-256 with the release's `SHA256SUMS.txt` if you want to verify the download.

## First connection

Open **Conexão** (Connection). **Abrir redes Wi-Fi** opens Android's Wi-Fi panel, where you choose the controller's network. Stay connected if Android reports no internet. Return to Lume: automatic discovery selects the sole controller, or lets you choose if several respond. Your choice is remembered by identifier.

Configure your home router while connected to the controller's network, then join the home network and return to Lume for another automatic search. Manual search remains available. See the [full Wi-Fi guide](WIFI.md).

Close Smart WiFi Timer before using Lume; if its background receiver holds UDP port 5000, force-stop it in Android settings. Lume explicitly binds UDP communication to Wi-Fi, including networks without internet, even if mobile internet is active.

## Edit and upload

1. In **Curvas** (Curves), choose a profile and channel.
2. Tap the curve to select a point; use arrows to move in 30-minute steps.
3. Edit with sliders, numeric inputs, or the table. **Criar ciclo** creates a day with smooth ramps.
4. In **Recife** (Reef), scrub or play the day. Without the live link, this preview does not change physical LEDs.
5. Once a controller is selected, tap **Enviar** (Upload). Review the profile, slot, and short device name before confirming.
6. In **Conexão**, **Sincronizar horário** sends your phone's local date and time.

To upload every profile, choose full-memory replacement and explicitly confirm erasing existing profiles. Keep the app open and Wi-Fi connected. [Upload details →](USAGE.md#upload-profiles)

## Files and updates

**Perfis → Importar** imports a Lume JSON library or Pled V2.2 `.lmf`, adding profiles to the current library. Android does not import `.lcm` projects. **Exportar biblioteca** exports JSON through Android's document picker; the app only accesses files you select.

Internal profiles and backups belong to the app's data. **Uninstalling or clearing data deletes them.** Export before uninstalling or changing phones. Updates signed with the same key preserve the installation; CI/development APKs may use a different key and cannot necessarily replace a release APK.

## Permissions and validation

Declared permissions: `INTERNET`, `ACCESS_NETWORK_STATE`, `ACCESS_WIFI_STATE`, and `CHANGE_WIFI_MULTICAST_STATE`. No USB, Bluetooth, camera, contacts, or location permissions. Router passwords are not saved in libraries or backups.

The target SDK is 36. A migration to target 37 must account for Android 17's local-network permission; see the [platform reference](https://developer.android.com/privacy-and-security/local-network-permission).

Build, signature verification, and local tests passed. The user confirmed discovery and reported good results after 1.0.5. Automatic discovery/reconnection in 1.0.6 still needs a phone retest. See the [compatibility matrix](COMPATIBILITY.md).

## Live light preview

Android 1.2.0 adds opt-in Wi-Fi preview for Smart Timer. In Reef, choose Link light fixture and confirm. Moving the timeline or playing the day sends real intensities without rewriting profiles. End preview to resume the schedule; reconnect the same device if restoration is unconfirmed. [Instructions and limits](LIVE-PREVIEW.md).
