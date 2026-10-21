# Compilar, probar y traducir

[Inicio](../../README.es.md) · [Português](../DESENVOLVIMENTO.md) · [English](../en/DEVELOPMENT.md) · **Español**

Clona `https://github.com/eifoxy/lume.git`. Windows usa WPF, C# y .NET 8; Android usa Java 17, SDK 36 y HTML/CSS/JavaScript/SVG locales. Los comandos de ejemplo requieren PowerShell 7.

## Windows

```powershell
dotnet build windows/src/Lume.csproj -c Release
pwsh ./scripts/build-windows.ps1
pwsh ./scripts/test-windows.ps1 -AppDirectory ./artifacts/windows-x64
```

El paquete completo incluye el runtime fijado por el script; el ligero necesita .NET Desktop Runtime 8. Las pruebas funcionales usan memoria y UDP loopback, y las de interfaz renderizan fuera de la pantalla. No envían comandos a una controladora real.

## Android

```powershell
pwsh ./android/build.ps1 `
  -Jdk 'C:\tools\jdk-17' `
  -BuildTools 'C:\Android\Sdk\build-tools\36.0.0' `
  -Platform 'C:\Android\Sdk\platforms\android-36' `
  -SigningKey 'C:\private\lume-local.p12' `
  -BuildDirectory './artifacts/android-build'
pwsh ./scripts/test-android.ps1 -Jdk 'C:\tools\jdk-17'
```

El resultado es `android/Lume-Android.apk`. No publiques la clave de firma. Las actualizaciones necesitan la misma clave; los APK de CI usan claves temporales. El proyecto Gradle de `android/` requiere AGP 8.10.1, Gradle 8.11.1 y JDK 17; no incluye wrapper. [Detalles de compilación, en inglés](../en/DEVELOPMENT.md).

## Traducciones

El catálogo compartido está en `localization/strings.json`. Cada entrada tiene `pt`, `en` y `es`. Windows lo incorpora como recurso; Android usa el archivo generado `strings.js`.

```powershell
node ./scripts/sync-localization.cjs
node ./scripts/sync-localization.cjs --check
node ./android/tests/i18n-test.cjs
```

Al añadir textos, usa `I18n.T` o la extensión XAML `Tr` en Windows y `tr` en Android. Traduce fragmentos estáticos, sin pasar nombres personales de perfiles o canales por el traductor. Nunca traduzcas claves JSON, comandos, direcciones ni bytes del protocolo. Conserva las confirmaciones de borrado, validaciones, copias y límites de reintentos.

La preferencia de idioma se guarda separada de la biblioteca. Las pruebas verifican catálogo, selección automática/manual, persistencia y conservación de datos y paquetes. Para reportar pruebas físicas, indica versión, modelo y función comprobada, sin contraseñas ni datos personales.
