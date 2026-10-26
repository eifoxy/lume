# Lume para iPhone y iPad

[Português (Brasil)](../INSTALACAO-IOS.md) · [English](../en/INSTALL-IOS.md) · **Español**

[← Lume](../../README.es.md)

**0.1.0 es una versión de desarrollo sin firma para iOS/iPadOS 17+.** Incluye el editor y la comunicación Wi-Fi nativa. GitHub Actions compila y ejecuta pruebas en un Mac. Falta validar con un iPhone y una controladora reales. No hay publicación en App Store o TestFlight.

El archivo `UNSIGNED.ipa` **no se instala al descargarlo**: requiere firma Apple. El ZIP Simulator es para Xcode en un Mac, no para el teléfono. [Compilaciones y archivos](https://github.com/eifoxy/lume/actions/workflows/ios.yml). Sin acceso a un Mac o a firma Apple, utiliza Windows/Android por ahora. El proyecto no compra servicios ni crea cuentas o suscripciones.

## Funciones implementadas

Curvas de cinco canales, 48 puntos diarios, ciclos, biblioteca de perfiles, importación JSON/LMF, exportación JSON mediante Archivos y guardado local con copias de seguridad. Arrecife animado, simulación del día y vista previa en la luminaria Smart Timer. Portugués, inglés y español según el dispositivo o el selector manual. Búsqueda y reconexión automáticas en primer plano, IP conocida, configuración del router, envío individual o sustitución de hasta 16 perfiles, reloj y reanudación del programa.

Implementado no significa validado físicamente. TC420 sin radio Wi-Fi sigue sin conexión inalámbrica. Los protocolos Smart Timer y Pled necesitan pruebas en iOS. No hay USB, lectura completa de la memoria ni control por internet.

## Instalación de desarrollo

1. Obtén el código y abre `ios/Lume.xcodeproj` en Xcode compatible, en un Mac.
2. Elige el esquema **Lume** y tu dispositivo. En **Signing & Capabilities**, selecciona tu equipo y un identificador único.
3. Inicia sesión en Xcode con tu Apple Account. Una **Personal Team gratuita** permite pruebas personales limitadas. La firma caduca a los siete días y debes recompilar/reinstalar. Para esta prueba personal no necesitas una membresía de pago. [Normas de Apple](https://developer.apple.com/help/account/basics/about-your-developer-account).
4. Activa el modo de desarrollador cuando Xcode lo solicite y ejecuta. Exporta la biblioteca antes de borrar el app o cambiar el identificador.

TestFlight/App Store requieren un equipo inscrito en el programa Apple correspondiente, firma y configuración de App Store Connect. Esta distribución no está configurada. [Programas Apple](https://developer.apple.com/help/account/membership/programs-overview/).

## Configuración Wi-Fi

1. Abre **Ajustes > Wi-Fi**, conecta a la red de la controladora y permanece conectado aunque no tenga internet.
2. Abre Lume y permite **Red local**. Si denegaste el permiso, actívalo en los ajustes del app. El botón de ayuda explica cómo conectar y permite abrir los permisos del app.
3. Espera la búsqueda automática, busca manualmente o usa una IP conocida. En `192.168.4.x`, prueba `192.168.4.1`.
4. Selecciona la controladora y envía el Wi-Fi de casa. Los módulos antiguos suelen requerir 2,4 GHz, WPA2/AES y SSID/contraseña ASCII. El protocolo acepta contraseñas de 8 a 32 caracteres. Lume no guarda la contraseña.
5. Conecta el iPhone al Wi-Fi de casa, vuelve a Lume, encuentra el aparato, sincroniza el reloj y envía los perfiles. Sustituir todos exige confirmación.

### Límites de búsqueda

Consultas **unicast limitadas**, sin depender del permiso especial de broadcast/multicast de Apple. IPv4 UDP 5000 se vincula al Wi-Fi y no cambia a datos móviles. Se consulta la IP guardada, la habitual del punto de acceso y hasta 254 direcciones próximas dentro de la subred actual, con un plazo máximo de 14 segundos. En redes mayores que `/24`, una controladora fuera de ese grupo puede necesitar IP conocida. El aislamiento entre clientes impide la comunicación.

No se ha confirmado en un iPhone real que todos los firmwares respondan a unicast. Aparatos que solo respondan a broadcast pueden necesitar una versión futura con autorización Apple. Escribir una IP no sustituye la respuesta ni la identidad del aparato. [Guía de Apple](https://developer.apple.com/documentation/technotes/tn3179-understanding-local-network-privacy).

## Vista previa en la luminaria

En **Arrecife**, vincula un Smart Timer y confirma. Mover el horario o reproducir el día cambia la luz física, incluso acelerada, con hasta cinco actualizaciones por segundo. No hay confirmación individual de intensidades ni lectura de la luz real. La escena no predice efectos biológicos.

Al detener o salir del primer plano, Lume intenta reanudar el programa. El aviso de recuperación solo se elimina tras confirmación del mismo aparato. Hay un plazo corto de limpieza en segundo plano, no funcionamiento continuo. Si pierdes la red o fuerzas el cierre, puede quedar la última intensidad manual. Reconecta el mismo aparato y pulsa **Reanudar programa**. La vista previa no borra ni regraba los perfiles.

## Privacidad y pruebas

Sin cuenta Lume, anuncios, telemetría o servidor. Editor sin conexión; comandos solo en la red local. Biblioteca, preferencias y recuperación en el almacenamiento del app, sujeto a la política de copias del sistema. Diagnósticos con IP locales y contadores, sin SSID/contraseña, compartidos solo por acción del usuario. Exporta JSON antes de desinstalar.

En un Mac: `node scripts/sync-ios-assets.cjs --check`, `node scripts/generate-ios-project.cjs --check`, `node ios/Tests/bridge-test.cjs` y `swift test --package-path ios`. Abre Xcode o usa el [flujo automático](../../.github/workflows/ios.yml), que también genera archivos sin firma con SHA-256. Las pruebas locales de UDP no sustituyen la validación de permisos y del aparato real.
