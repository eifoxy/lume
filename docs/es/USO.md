# Curvas, perfiles y envíos

[Inicio](../../README.es.md) · [Português](../USO.md) · [English](../en/USAGE.md) · **Español**

Cada perfil tiene cinco canales y 48 puntos, uno cada 30 minutos. Las intensidades son enteros de 0 a 100%. La vista previa interpola los intervalos y el paso de las 23:30 a medianoche. Los nombres y colores no cambian el orden físico CH1 a CH5.

## Editar y simular

En **Curvas**, elige perfil y canal, toca el gráfico y ajusta el punto con controles, números o tabla. En Windows, activa **Dibujar curva** para editar con el ratón; sin activarlo, los clics solo seleccionan el horario de la vista previa. Amplía o aísla un canal si lo necesitas.

**Crear ciclo** sustituye los 48 puntos por un amanecer y anochecer con rampas. El intervalo debe terminar el mismo día e incluir ambas rampas. **Deshacer** recupera la edición anterior. En **Arrecife**, recorre o reproduce el día. La animación no modifica la luz real ni predice la respuesta biológica de los corales.

## Biblioteca y copias

Se admiten hasta 100 perfiles locales. JSON permite transferir bibliotecas entre Windows y Android. Ambos importan perfiles LMF; Windows también exporta LMF e importa proyectos LCM cuyos archivos referenciados existan. Android exporta JSON.

Las copias son de la biblioteca local. Lume no lee todos los programas de la controladora y no puede recuperar perfiles que existían solo en su memoria. En Android, exporta una copia fuera de la aplicación antes de desinstalar o borrar datos.

Cambiar de idioma traduce los controles, no los nombres que hayas guardado. Se conservan los horarios, intensidades y datos del protocolo.

## Enviar

Selecciona la controladora y pulsa **Enviar**. Revisa el perfil, su posición y el nombre abreviado de hasta siete caracteres ASCII.

- **Un perfil:** sustituye solo la posición elegida, del 1 al 16.
- **Todos los perfiles:** borra una vez todos los anteriores y envía la biblioteca en orden desde la posición 1. Requiere confirmación explícita.

Se valida el conjunto y se crea una copia local antes del primer comando. Mantén el app abierto y la conexión activa. Los comandos requieren confirmación.

En Smart Timer Android, un punto indexado o el encabezado inicial antes de enviar puntos puede tener hasta tres intentos idénticos, solo si no llega una respuesta válida. No se repite el borrado, no se reinicia un perfil parcial y no se reintentan automáticamente credenciales, reloj o finalizadores. Tras confirmar el borrado, hay una espera cancelable de 2,1 segundos, como margen conservador.

Un fallo puede dejar la memoria vacía o parcial. Copia el diagnóstico, vuelve a conectar y reenvía el conjunto revisado. No hay rollback ni lectura completa de retorno. Comprueba en el aparato qué perfil queda activo.

## Otros comandos

**Sincronizar reloj** envía la fecha y hora local del dispositivo. **Reanudar programa** sale del modo manual; no elige un perfil. **Aplicar este instante a los LED**, solo en Windows por USB, modifica las intensidades físicas tras confirmación.
