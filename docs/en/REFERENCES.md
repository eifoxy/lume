# References

[Home](../../README.en.md) · **English** · [Português (Brasil)](../REFERENCIAS.md)

- Classic TC420 protocol: [wallneradam/tc420](https://github.com/wallneradam/tc420). A reference for the classic transport, not proof of compatibility with every firmware.
- Pled V2.2 USB/Wi-Fi: analysis of the supplied executable; the proprietary binary is not redistributed.
- Smart WiFi Timer 3.0, package `com.leyview.timercontroller`: [static analysis of the user-supplied APK](SMART-WIFI-TIMER-ANALYSIS.md). Confirms `88 00` replies, acknowledgment identity, and upload finalization. APK/decompiled code excluded from this repository.
- [.NET Desktop Runtime 8 - Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
- [.NET single-file publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).
- [Android Network.bindSocket](https://developer.android.com/reference/android/net/Network#bindSocket(java.net.DatagramSocket)).
- [Android local-network permissions](https://developer.android.com/privacy-and-security/local-network-permission).
- [Android tools](https://developer.android.com/tools).
- [Android Gradle Plugin 8.10 compatibility](https://developer.android.com/build/releases/agp-8-10-0-release-notes).
- [Temurin JDK](https://adoptium.net/).

Android/.NET sources document their platforms, not the controller's proprietary protocol. See [Compatibility](COMPATIBILITY.md) to distinguish software tests from physical validation.
