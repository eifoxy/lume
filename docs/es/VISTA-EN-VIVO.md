# Vista previa en la luminaria por Wi-Fi

[Inicio](../../README.es.md) · [Português (Brasil)](../PREVIA-AO-VIVO.md) · [English](../en/LIVE-PREVIEW.md) · **Español**

Disponible en **Android 1.2.0** para controladoras **Smart Timer** con respuestas `88 00`. Los comandos se identificaron en el APK original y se probaron con simuladores. Falta comprobar el resultado en la luminaria real.

## Uso

1. Conecta el teléfono al Wi-Fi del aparato o al mismo router y espera la búsqueda.
2. Abre el perfil que deseas probar. Envíalo normalmente si quieres guardarlo en la controladora. Tras confirmar el envío, se abre el Arrecife.
3. Pulsa **Vincular luminaria** y confirma que quieres modificar la luz real. El envío de perfiles no activa esta función automáticamente.
4. Mueve el horario o pulsa **Reproducir día**. La luminaria recibe las cinco intensidades interpoladas del mismo perfil. Pausar el día mantiene el instante actual y deja activa la vista previa en vivo.
5. Pulsa **Finalizar vista previa y reanudar programa**, espera la confirmación y comprueba el perfil activo en el aparato.

También puedes editar los puntos del perfil actual. Los cambios se guardan localmente; esta función no regraba los horarios del aparato. Cambiar de perfil, enviar programación, configurar Wi-Fi, sincronizar el reloj y buscar otros aparatos queda bloqueado mientras la sesión está activa.

## Límites de la sincronización

El protocolo no confirma cada intensidad. **Último envío** muestra un datagrama enviado por el teléfono, no una medición ni prueba de recepción. UDP puede perder paquetes y puede existir retraso entre la pantalla y la luz.

La sesión conserva solo los valores más recientes y envía como máximo cinco tramas de intensidad por segundo, sin reenviar continuamente valores iguales. La interfaz aporta una señal local de actividad cada 200 ms; si falta durante más de tres segundos, se intenta finalizar y reanudar el programa.

## Salir o perder conexión

Al salir del primer plano, el app detiene el envío e intenta reanudar con `03 08`, exigiendo respuesta Smart `83` de la misma controladora en un máximo de 2,5 segundos. No existe servicio de vista previa en segundo plano ni reinicio automático.

Si se pierde la red, Android cierra el proceso o no llega la confirmación, **la luminaria puede permanecer en modo manual con la última intensidad recibida**. No hay un watchdog del hardware confirmado que permita garantizar la recuperación sin comunicación.

El app guarda la identificación de recuperación antes del primer comando. Si queda pendiente, el aviso aparece al volver a abrir. Reconecta la **misma controladora**, espera la búsqueda y pulsa **Reanudar programación en el aparato**. Nuevas vistas previas, grabaciones, configuración de red y reloj esperan esta recuperación. No se guarda contraseña ni biblioteca en ese registro.

El diagnóstico registra destino local, contadores y confirmación de reanudación, sin nombre del perfil, SSID, contraseña ni paquetes completos.

## Comandos

Inicio `06 01`, dos veces con 300 ms de separación; nombre desde el byte 6. Intensidades `06 02`, cinco valores de 0 a 100 desde el byte 6. Tramas de 64 bytes con `55 AA`, identificación en 49 a 60 y checksum en 61. Finalización `03 08`, respuesta `88 00` con estado `83` e identificación coincidente. No envía borrado, grabación de perfiles, configuración de red ni reloj durante la sesión.

La función se limita a Smart Timer en Android. Windows conserva su ajuste por USB; no se añade vista previa Wi-Fi al protocolo Pled antiguo.
