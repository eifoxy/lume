# Instalación e idioma

[Inicio](../../README.es.md) · [Português](../INSTALACAO-ANDROID.md) · [English](../en/INSTALL-ANDROID.md) · **Español**

## Elegir idioma

Las versiones Android 1.1.0 y Windows 0.6.0 incluyen portugués brasileño, inglés y español. La opción inicial es **Automático (dispositivo)**: usa la preferencia de idioma del teléfono o Windows, con inglés como alternativa para idiomas no soportados. Las variantes de español, como es-MX y es-ES, usan español; las variantes de portugués usan PT-BR.

El selector **Idioma** aparece al entrar, sobre el contenido en Android y en la barra lateral en Windows. Puedes cambiarlo sin reiniciar. Se guarda de forma separada de los perfiles. Durante búsquedas, envíos o diálogos, espera a que terminen antes de cambiarlo. Los nombres personales de perfiles y canales permanecen iguales.

## Android

Requiere Android 8 o superior, WebView actualizado y controladora con Wi-Fi compatible. No requiere cable, cuenta, nube ni depuración USB.

1. Abre la [versión Android](https://github.com/eifoxy/lume/releases/tag/android-v1.1.0).
2. Descarga `Lume-Android-1.1.0.apk` en **Assets**.
3. Abre el archivo. Si Android lo solicita, permite instalar aplicaciones a ese navegador o gestor de archivos.
4. Instala y abre Lume. Si ya lo usabas, instala encima, **sin desinstalar**, para conservar los perfiles.
5. Elige **Español** en el selector si tu dispositivo usa otro idioma.

El APK se distribuye por GitHub, no por Google Play, y usa la misma clave de firma de las versiones anteriores. Las compilaciones de CI pueden tener otra clave y no sustituir la instalación de lanzamiento.

En **Conexión**, abre las redes Wi-Fi, conéctate a la red del aparato y vuelve a Lume. Se inicia la búsqueda automática. Cierra o fuerza la detención del programa original si ocupa el puerto UDP 5000. Mantente en la red aunque Android indique que no tiene internet. [Configuración del router](WIFI.md).

Importa JSON/LMF en **Perfiles** y exporta JSON mediante el selector de documentos. Desinstalar o borrar datos elimina las bibliotecas internas: exporta antes una copia fuera de la aplicación. Las contraseñas del router no se guardan.

## Windows

Requiere Windows 10/11 x64 para el paquete completo. Descarga la [versión Windows](https://github.com/eifoxy/lume/releases/tag/windows-v0.6.0):

| Archivo | Requisito |
|---|---|
| `Lume-Windows-0.6.0-win-x64.zip` | Incluye .NET. |
| `Lume-Windows-0.6.0-portable.zip` | Requiere .NET Desktop Runtime 8. |

Extrae todo en una carpeta donde puedas guardar archivos y abre `Lume.exe`. No lo ejecutes dentro del ZIP. No requiere administrador ni cambiar el controlador HID de Windows. En el paquete ligero, conserva DLL y JSON junto al ejecutable. El ejecutable no tiene certificado comercial.

Antes de actualizar, exporta la biblioteca, cierra el app y extrae la nueva versión en otra carpeta. Importa allí la copia y verifica tus perfiles antes de borrar la carpeta anterior. Si conservas la carpeta `data` completa, conservas también `language.json`, la preferencia de idioma.

Para USB, cierra Pled, conecta un cable de datos y pulsa **Buscar USB**. Para Wi-Fi, sigue la [guía de red](WIFI.md). La compatibilidad del protocolo no se amplía por añadir idiomas: Smart Timer `88 00` continúa adaptado solo en Android.

## Verificación

Las versiones incluyen archivos `SHA256SUMS.txt`. Los tests locales y la firma del APK se verifican antes de publicar. La traducción se prueba con eventos, temporizadores, navegador y renderizado WPF. Esas pruebas no sustituyen la ejecución en un teléfono real o la validación física de cada comando.
