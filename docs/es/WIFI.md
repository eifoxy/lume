# Wi-Fi y búsqueda de la controladora

[Inicio](../../README.es.md) · [Português](../WIFI.md) · [English](../en/WIFI.md) · **Español**

## Primera conexión

1. Enciende el aparato y activa su punto de acceso siguiendo el procedimiento del fabricante. Algunos módulos usan redes con prefijo `KITELIGHT-WIFI`; no hay una contraseña universal asumida por Lume.
2. Conecta el PC o teléfono a esa red. Si no tiene internet, permanece conectado.
3. En Android, vuelve a Lume para iniciar la búsqueda automática. En Windows, pulsa **Conectar por Wi-Fi → Buscar en la red**.
4. Si solo hay una controladora, Android la selecciona. Si hay varias, elige la correcta. La elección se recuerda por identificador, incluso si cambia la IP.
5. Envía los datos de tu router, conecta el PC o teléfono a la red de casa y vuelve a buscar.

La red inicial puede desaparecer después de recibir las credenciales. Si no llega la confirmación final, busca desde el router antes de reenviar la configuración.

## Datos de la red

| Campo | Compatibilidad implementada |
|---|---|
| Nombre de red / SSID | De 1 a 32 caracteres ASCII, sin acentos. |
| Contraseña | De 8 a 32 caracteres ASCII, sin acentos; no se recorta. |
| Seguridad | WPA2-PSK o WPA-PSK. |
| Cifrado | AES o TKIP. |

Usa la banda soportada por el módulo, normalmente 2,4 GHz. No se implementan WPA3, redes empresariales o portales de acceso. La contraseña no se guarda en la biblioteca ni en la copia. El módulo usa su protocolo local antiguo, sin túnel TLS.

## Búsqueda automática en Android

Al abrir o regresar, la búsqueda dura unos tres segundos. Sin una controladora seleccionada, vuelve a intentar 20 segundos después de finalizar; con una seleccionada, comprueba de nuevo tras 60 segundos. No cambia silenciosamente a otra si la anterior desaparece.

La búsqueda espera a que terminen operaciones y diálogos. Se detiene fuera del primer plano y no usa un servicio en segundo plano. No reenvía perfiles, configura redes ni sincroniza el reloj por sí sola. La búsqueda manual dura unos ocho segundos y permite generar un diagnóstico. Windows usa búsqueda manual, de unos cinco segundos.

## Si no aparece

- Comprueba que existe hardware Wi-Fi compatible. Una TC420 sin radio no puede conectarse por software.
- Usa la misma red local; redes de invitados, VLAN o aislamiento entre clientes pueden bloquear UDP.
- Cierra el programa original y otras instancias que ocupen el puerto UDP 5000.
- Prueba la búsqueda por IP si conoces la dirección. Debe ser de la misma subred IPv4.
- No abras puertos hacia internet. Lume se comunica solo por la red local.
- Si el aparato pita pero no aparece, el sonido no prueba que su respuesta haya llegado al teléfono. Usa la versión actual y copia el diagnóstico de una búsqueda manual.

Las respuestas Smart Timer usan cabecera `88 00`, distinta de las solicitudes `55 AA`. La corrección está en Android desde 1.0.3; Windows 0.6.0 conserva su protocolo anterior. [Análisis técnico, en inglés](../en/SMART-WIFI-TIMER-ANALYSIS.md).

El diagnóstico muestra direcciones locales y contadores, sin SSID, contraseña ni paquetes completos. No se envía automáticamente. Revísalo antes de compartirlo. Un fallo después de borrar perfiles puede dejar la memoria incompleta: vuelve a conectar y reenvía la biblioteca revisada. No existe restauración automática de la memoria física.
