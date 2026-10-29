# Install on Windows

[Home](../../README.en.md) · **English** · [Português (Brasil)](../INSTALACAO-WINDOWS.md) · [Wi-Fi setup](WIFI.md) · [Usage](USAGE.md)

## Requirements and download

- Windows 10/11 x64 for the `win-x64` package, and a folder your user can write to.
- A compatible USB HID controller or a compatible Wi-Fi module for wireless use.

Open the [Windows release](https://github.com/eifoxy/lume/releases/tag/windows-v0.7.0):

| File | Choose this when |
|---|---|
| `Lume-Windows-0.7.0-win-x64.zip` | You want .NET included; easiest for Windows x64. |
| `Lume-Windows-0.7.0-portable.zip` | You prefer a smaller download and have [.NET Desktop Runtime 8 for Windows](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). The base or ASP.NET runtime alone is insufficient. |

1. Download your chosen ZIP and select **Extract All**.
2. Keep the extracted folder in a writable location such as Documents. Do not run inside the ZIP.
3. Open `Lume.exe`. In the small package, keep the `.dll` and `.json` files beside it.
4. Review the example cycle before uploading anything.

The app is portable and needs no installer or administrator privileges. The current executable has no commercial signing certificate. Download from this repository's releases; compare with `SHA256SUMS.txt` to verify:

```powershell
Get-FileHash .\Lume-Windows-0.7.0-win-x64.zip -Algorithm SHA256
```

The app supports Portuguese, English, and Spanish. Select a language or Automatic (device) in the sidebar; the choice is saved separately from profiles. Examples below retain Portuguese button names. The transport is inherited from 0.5.0; adding languages does not adapt Smart Timer framing.

## Connect over USB

1. Close Pled and other apps using the controller.
2. Connect a USB data cable and select **Buscar TC420** (Find TC420).
3. Check the detected identity: classic TC420 and Pled V2.2 use separate protocol paths.
4. Edit a profile. **Enviar à TC420** opens the profile/slot review before writing.

Lume uses native Windows HID; no external driver replacement is required. Detection does not prove every firmware command works. See [compatibility](COMPATIBILITY.md).

**Aplicar este instante aos LEDs** sends actual USB intensities after confirmation. The preview and reef do not. **Retomar programa** exits manual adjustment; **Sincronizar horário** sends Windows local date/time.

## Connect over Wi-Fi

Open **Conectar por Wi-Fi** and follow the [Wi-Fi guide](WIFI.md). After selecting a controller, uploads, clock sync, and resume use that connection. Instant physical intensity adjustment remains USB-only. Windows 0.7.0 does not include Android's Smart Timer `88 00` adaptation.

![Wi-Fi connection window](../images/previa-wifi.png)

## Files and updates

Data lives in `data/` next to the executable:

| File | Contents |
|---|---|
| `biblioteca.json` | Current profile library. |
| `biblioteca.json.bak` | State before the last atomic save. |
| `diagnostico.log` | Local failure diagnostics. |

Use **Salvar backup** to export. Before updating, close the app and export a backup; extract the new version into a separate folder and import it. Keep the old folder until you verify your profiles.

## Troubleshooting

- **Will not open:** install .NET **Desktop** Runtime 8 for the small package and extract the entire ZIP.
- **USB not detected:** check the data cable, connector, model identity, and other apps holding the controller.
- **Wi-Fi not found:** verify your current network and follow [network troubleshooting](WIFI.md#troubleshooting).
- **Cannot save:** use a writable folder, check free space, and preserve existing files before recovery.
- **Upload interrupted:** device memory may be incomplete. Reconnect and resend the reviewed set; physical memory has no rollback.
