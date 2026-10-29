# Pasar perfiles del PC al móvil mediante QR

[Português (Brasil)](../QR.md) · [English](../en/QR.md) · **Español**

Requiere **Windows 0.7.0** y **Android 1.3.0** o **iOS 0.2.0**. No necesita cuenta, internet ni la misma red Wi-Fi. Los QR contienen nombres de perfiles/canales y los 48 horarios de cinco canales de la biblioteca del PC. No incluyen contraseñas, direcciones de la controladora ni comandos para la luminaria.

1. En el PC, selecciona un perfil y pulsa **Enviar al móvil por QR**. Corrige celdas no válidas antes.
2. Se comparte el perfil seleccionado por defecto. Marca **Todos los perfiles** para transferir la biblioteca completa.
3. En el móvil, abre **Perfiles > Leer QR del PC**, permite la cámara y apunta a la pantalla del ordenador.
4. Si hay varias partes, pulsa **Siguiente** en el PC o activa **Alternar códigos QR automáticamente**. Se repiten cada dos segundos. El móvil cuenta las partes y admite lecturas repetidas o fuera de orden.
5. Al completar y verificar la transferencia, la cámara se cierra y Lume pide confirmación. **Cancelar conserva tu biblioteca.** Confirmar añade perfiles sin sustituir los existentes, hasta el límite de 100 perfiles del móvil.

Si cuesta leer, acerca el móvil, evita reflejos y amplía la ventana del PC. Mantén la cámara apuntando hasta completar el contador. La lectura caduca a los diez minutos. Cancela antes de cambiar entre el perfil actual y toda la biblioteca: no se mezclan envíos distintos.

Las imágenes se procesan localmente, sin guardar fotos ni subirlas a un servidor. Salir del app cierra la cámara. Si denegaste el permiso, actívalo en los ajustes o utiliza la importación JSON/LMF.

**Importar no envía nada a la luminaria.** Revisa los perfiles y envía la programación por separado cuando quieras. Se comparte la biblioteca local del PC, sin leer la memoria de la controladora. La copia JSON sigue disponible.

iOS continúa requiriendo firma Apple e iOS 15.4+. [Guía iOS](INSTALACION-IOS.md). Falta validar la cámara en teléfonos reales. Las pruebas automáticas cubren QR generados por Windows y leídos por el motor Android, reconstrucción, confirmación/cancelación y tres idiomas; no sustituyen pruebas físicas de enfoque e iluminación.
