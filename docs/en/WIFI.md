# Wi-Fi setup and discovery

[Home](../../README.en.md) · **English** · [Português (Brasil)](../WIFI.md) · [Compatibility](COMPATIBILITY.md)

Android uses Wi-Fi exclusively; Windows also offers USB. Wireless operation requires a compatible module. Software cannot add Wi-Fi hardware to a standard TC420. The released app buttons use Brazilian Portuguese.

## 1. Join the controller's network

Power on the controller and follow its manufacturer's initial access instructions. Join the network created by the device; some modules use a name beginning with `KITELIGHT-WIFI`. Its name and any password depend on the hardware; Lume does not assume a universal password.

Android: **Conexão → Abrir redes Wi-Fi** opens the system panel. Windows: use the Windows network menu. Stay connected if the network has no internet. This is not yet the home-router step.

## 2. Discover and select

- Windows: **Conectar por Wi-Fi → Buscar na rede**.
- Android 1.0.6: discovery starts automatically on launch/return. **Conexão → Buscar aparelhos** remains available for manual search and diagnostics.

Searches take about five seconds on Windows, eight seconds for manual Android searches, and three seconds for automatic searches. They send local UDP queries and list responding identities and IPs. Android queries the Wi-Fi gateway, subnet broadcast, and limited broadcast, without scanning host addresses or ports. Discovery does not write schedules or alter lighting configuration.

Android selects a sole controller on first discovery. If several respond, choose one. It stores only that identifier and recovers the same device on future searches, updating its IP. If that device disappears, Lume does not silently select another; you can choose another in **Conexão**. It retries 20 seconds after a search without a selected device and checks again after 60 seconds with one selected. Intervals start when each search ends.

Automatic searches do not open pages or overwrite an upload failure's diagnostic. Editing/navigation remain available; device commands wait for the short search to finish. Network operations are exclusive. Searches wait for uploads and dialogs, and stop outside the foreground. There is no background service, automatic clock synchronization, or automatic schedule resend.

On Windows, choose **Usar esta controladora** to operate the selected controller; use the router fields in the dialog for initial setup. Direct IP search is available when you know the address. It must be on the same local IPv4 network, not an internet destination.

## 3. Configure the home network

While still joined to the controller's network, enter:

| Field | Implemented limit |
|---|---|
| Network name / SSID | 1-32 ASCII characters, case-sensitive, without accents. |
| Password | 8-32 ASCII characters. Overlong input is rejected, not silently truncated. |
| Authentication | WPA2-PSK or WPA-PSK. |
| Encryption | AES or TKIP. |

The initial UI choice is WPA2-PSK/AES. Use settings and a radio band supported by both module and router, often 2.4 GHz. WPA3, enterprise networks, and captive portals are not implemented.

Review the destination, then confirm **Enviar rede à controladora** on Windows or **Configurar rede de casa** on Android. Passwords do not appear in the review, are not stored, and are excluded from backups. Commands use the module's legacy local protocol; the module provides no TLS tunnel.

SSID and password are separate packets with separate acknowledgments. **If the final acknowledgment is missing, the device may already have switched networks.** Join the home router and search there before trying again. Credentials are not automatically retried.

## 4. Join the home network

1. Connect your PC/phone to the router you configured.
2. Wait for the controller to join it.
3. Return to Lume on Android, or search again on Windows. The device IP may change.
4. Synchronize the clock, review your profile, and upload it.

On Android, returning from another app invalidates the old destination and starts a fresh search. The previous selection is recovered by identifier only when the controller responds.

## Troubleshooting

| Symptom | Check |
|---|---|
| Device's initial network is absent | Confirm Wi-Fi hardware and how its access point is enabled. Do not factory-reset without knowing the procedure and preserving schedules. |
| No results during initial setup | PC/phone must remain on the device's Wi-Fi even without internet. |
| No results at home | Both must share a local network; guest isolation, VLANs, or client isolation can block UDP. |
| Known IP but broadcast blocked | Try direct IP search. Firmware and network still need to accept communication. |
| UDP 5000 busy | Close Pled, another Lume instance, and other controller apps. |
| Windows blocks replies | Review app firewall permissions. Lume does not disable or change the firewall automatically. |
| Credentials sent without final confirmation | Search from the home router before resending. |
| Upload error | Memory may be incomplete. Reconnect and resend the reviewed library; no physical rollback exists. See [upload recovery](USAGE.md#upload-profiles). |

No internet port forwarding is needed. Intended use is local-network-only.

## Device beeps but does not appear

A beep alone does not prove its response reached the phone. Use the [current Android release](https://github.com/eifoxy/lume/releases/tag/android-v1.0.6), updating without uninstalling.

The diagnostic **cabeçalho diferente: 88 00** identified a confirmed parser issue: Smart Timer replies use `88 00`, while releases through 1.0.2 incorrectly expected `55 AA`. Version 1.0.3 corrected discovery, acknowledgment identity checks, and upload finalization. [APK analysis →](SMART-WIFI-TIMER-ANALYSIS.md)

Close the vendor app; force-stop it if its receiver keeps port 5000 open. Stay on the controller's no-internet Wi-Fi. Temporarily testing without VPN/mobile data can help isolate routing; it is not proof of a cause.

Run a manual search, then open **Diagnóstico da última comunicação → Copiar diagnóstico**. A received count of zero differs from received-but-rejected packets. The report contains local IPs, gateway, destinations, phone/Android version, and counts, without SSID, password, or raw packets. Review it before sharing; Lume does not transmit it automatically.

Discovery is user-confirmed on Android, with positive feedback after 1.0.5. Automatic discovery in 1.0.6 still needs a phone retest. Windows compatibility and individual hardware functions have separate evidence. [Current validation status →](COMPATIBILITY.md)
