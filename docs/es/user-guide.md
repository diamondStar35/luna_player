# Guía del usuario de Luna Player

Luna Player es un reproductor de audio y video para Windows pensado para usarse con el teclado. Reproduce medios locales, transmisiones de red, YouTube e IPTV; lee subtítulos en voz alta; descarga y convierte medios; graba audio; y ofrece administración de archivos, marcadores, anuncios por voz y atajos locales y de todo el sistema configurables.

Esta guía describe la versión actual de Luna Player. Los nombres de menús y los atajos que aparecen a continuación usan la interfaz en español y la configuración de atajos predeterminada.

::: toc
- [1. Antes de comenzar](#1-before-you-begin)
- [2. Interfaz y accesibilidad](#2-interface-and-accessibility)
- [3. Inicio rápido](#3-quick-start)
- [4. Apertura de medios](#4-opening-media)
- [5. Controles de reproducción](#5-playback-controls)
- [6. Trabajo con varios archivos](#6-working-with-several-files)
- [7. Bucles A-B](#7-a-b-loops)
- [8. Eliminación de silencios y procesamiento de audio](#8-silence-removal-and-audio-processing)
- [9. Administración de archivos locales](#9-local-file-management)
- [10. Archivos marcados y operaciones por lotes](#10-marked-files-and-batch-operations)
- [11. Marcadores](#11-bookmarks)
- [12. YouTube](#12-youtube)
- [13. Grabación de audio](#13-recording-audio)
- [14. IPTV](#14-iptv)
- [15. Convertidor de medios](#15-media-converter)
- [16. Temporizador de sueño](#16-sleep-timer)
- [17. Referencia de Preferencias](#17-preferences-reference)
- [18. Actualizaciones de la aplicación](#18-application-updates)
- [19. Ayuda e información de versiones](#19-help-and-release-information)
- [20. Configuración y datos del usuario](#20-settings-and-user-data)
- [21. Referencia completa de atajos predeterminados](#21-complete-default-shortcut-reference)
- [22. Formatos compatibles](#22-supported-formats)
- [23. Solución de problemas](#23-troubleshooting)
- [24. Editor y licencias](#24-publisher-and-licensing)
- [25. Contacto y soporte](#25-contact-and-support)
:::

## 1. Antes de comenzar {#1-before-you-begin}

### Requisitos del sistema

Luna Player requiere Windows 10 de 64 bits versión 1809 o posterior. Capturar el sonido de un programa específico requiere Windows 10 versión 2004 o posterior. Las demás funciones de reproducción y grabación siguen disponibles en versiones compatibles más antiguas.

### Ediciones instalada y portátil

Las versiones de Luna Player están disponibles como instalador y como archivo ZIP portátil.

- Para instalar Luna Player, ejecute el instalador de la versión y siga sus páginas. El instalador puede crear accesos directos en el menú Inicio y en el escritorio, y puede registrar los tipos de medios compatibles.
- Para usar la edición portátil, extraiga el archivo ZIP completo en una carpeta y ejecute `LunaPlayer.exe`. Mantenga juntos todos los archivos y carpetas extraídos.

Las dos ediciones usan la misma carpeta de configuración del usuario. El actualizador integrado reconoce qué edición se está ejecutando y descarga el instalador o el archivo portátil correspondiente.

### Inicio de Luna Player

Puede iniciar Luna Player directamente, abrir con él un archivo compatible desde Windows o pasarle archivos y carpetas desde la línea de comandos. De forma predeterminada, al abrir más medios mientras Luna ya está en ejecución, esos medios se envían a la ventana existente. Desmarque **Abrir los archivos en la ventana existente cuando Luna ya está en ejecución** en la página General de Preferencias si cada archivo abierto desde Windows debe usar una ventana de Luna independiente.

Si **Recordar la posición del último archivo** está habilitado, al iniciar Luna sin otro archivo se restaura el último archivo local y su última posición de reproducción, siempre que ese archivo siga disponible.

## 2. Interfaz y accesibilidad {#2-interface-and-accessibility}

### Ventana principal

La ventana principal contiene una barra de menús y cinco botones:

- **Anterior** pasa al elemento anterior.
- **Retroceder** retrocede la cantidad de desplazamiento seleccionada.
- **Reproducir** o **Pausar** cambia el estado de reproducción.
- **Avanzar** avanza la cantidad de desplazamiento seleccionada.
- **Siguiente** pasa al elemento siguiente.

Todos los comandos también están disponibles desde un menú o un atajo. Los elementos que no se pueden aplicar a los medios actuales aparecen deshabilitados. Por ejemplo, las propiedades del archivo están disponibles para un archivo local pero no para una transmisión web, y el menú **Opciones de video** solo se habilita para un video de YouTube activo.

### Navegación con el teclado

Use la navegación estándar de Windows en toda la aplicación:

- Presione [Alt]{.kbd} para acceder a la barra de menús y luego use las teclas de flecha y [Intro]{.kbd}.
- Presione [Tab]{.kbd} y [Mayús]{.kbd}+[Tab]{.kbd} para moverse entre los controles de los cuadros de diálogo.
- Use las teclas de flecha para desplazarse por listas y opciones.
- Presione [Intro]{.kbd} para activar el elemento seleccionado donde un cuadro de diálogo lo admita.
- Presione [Esc]{.kbd} para cerrar la mayoría de los cuadros de diálogo sin aplicar ningún cambio.
- Presione [F1]{.kbd} en la ventana principal para abrir esta guía.

En un control de Preferencias, [F1]{.kbd} tiene otra función: Luna lee en voz alta la ayuda contextual de ese control. Si el foco está en el árbol de categorías, explica cómo moverse entre las páginas de configuración.

### Anuncios por voz

Luna informa por medio de un lector de pantalla compatible el estado de la reproducción, el tiempo, el volumen, la navegación, las operaciones con archivos y otros cambios. Hay dos niveles de detalle disponibles:

- **Principiante** usa mensajes completos y explicativos.
- **Avanzado** usa confirmaciones más breves.

Presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[V]{.kbd} para cambiar el nivel de detalle de inmediato, o elíjalo en la página **General** de Preferencias. Habilite **Leer el nombre del archivo al navegar (Anterior/Siguiente)** si desea que Luna anuncie cada elemento al que se llega con Anterior o Siguiente.

### Controles multimedia de Windows

Luna se integra con la superposición multimedia de Windows y con las teclas multimedia de hardware compatibles. La superposición puede mostrar el título actual y la línea de tiempo. Los botones de reproducir, pausar, anterior, siguiente, retroceder y avance rápido invocan los comandos correspondientes de Luna.

Si un teclado, unos auriculares con micrófono u otro programa envían comandos multimedia a la aplicación equivocada o los envían dos veces, habilite **Deshabilitar los controles multimedia del sistema** en la página General de Preferencias. Esto quita a Luna de la superposición multimedia de Windows y evita que gestione las teclas multimedia de hardware.

## 3. Inicio rápido {#3-quick-start}

Para reproducir un archivo local:

1.  Presione [Ctrl]{.kbd}+[O]{.kbd}.
2.  Elija un archivo multimedia o una lista de reproducción M3U/M3U8.
3.  Presione [Espacio]{.kbd} para pausar o reanudar.
4.  Presione [Flecha izquierda]{.kbd} o [Flecha derecha]{.kbd} para avanzar o retroceder.
5.  Presione [Flecha arriba]{.kbd} o [Flecha abajo]{.kbd} para cambiar el volumen.

Para reproducir todos los archivos compatibles de una carpeta, presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[O]{.kbd} y elija la carpeta. Use [Tab]{.kbd} y [Mayús]{.kbd}+[Tab]{.kbd} para moverse entre los elementos cargados.

Para reproducir un video de YouTube, presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[Y]{.kbd} e ingrese su dirección. Para buscar en cambio, presione [Ctrl]{.kbd}+[Y]{.kbd}.

## 4. Apertura de medios {#4-opening-media}

### Apertura de un archivo

Elija **Archivo \> Abrir archivo\...** o presione [Ctrl]{.kbd}+[O]{.kbd}. El cuadro de diálogo acepta archivos de audio, video, M3U y M3U8 compatibles.

La opción **Qué desea abrir con los archivos?** controla lo que sucede cuando se abre un archivo multimedia desde Windows o desde el cuadro de diálogo de archivos:

- **Abrir solo el archivo** carga únicamente el archivo seleccionado.
- **Abrir el archivo y los archivos de la carpeta principal** carga los archivos compatibles de la misma carpeta y selecciona el archivo solicitado.
- **Abrir el archivo con los archivos de la carpeta principal y las subcarpetas** analiza la carpeta y todas las carpetas inferiores, carga todos los archivos multimedia compatibles que encuentre y selecciona el archivo solicitado.

Un análisis recursivo muestra un cuadro de diálogo de progreso cancelable e informa cuántos archivos multimedia ha encontrado. Los archivos se ordenan según el orden natural de Windows.

Cuando se envían varios archivos juntos a Luna y está seleccionada la opción **Abrir solo el archivo**, Luna carga todos esos archivos seleccionados explícitamente en lugar de descartar todos menos uno.

### Apertura de una carpeta

Elija **Archivo \> Abrir carpeta\...** o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[O]{.kbd}.

Con cualquiera de los dos primeros modos de apertura de archivos, Luna carga los medios compatibles que están directamente dentro de la carpeta elegida. Con el modo de subcarpetas, realiza un análisis recursivo. Las carpetas inaccesibles y del sistema se omiten durante un análisis recursivo.

### Apertura de una transmisión de red

Elija **Archivo \> Abrir enlace\...** o presione [Ctrl]{.kbd}+[L]{.kbd}. Ingrese una dirección `http` o `https`.

Use este comando para transmisiones multimedia directas y listas de reproducción M3U o M3U8 remotas. Use **Abrir enlace de YouTube\...** para direcciones de videos y listas de reproducción de YouTube.

### Apertura de listas de reproducción

Luna admite listas de reproducción M3U y M3U8 locales y remotas. Las entradas relativas de una lista de reproducción local se resuelven a partir de la carpeta de la lista; las entradas relativas de una lista de reproducción de red se resuelven a partir de su dirección.

Un manifiesto HLS M3U8 se trata como una sola transmisión y se pasa al motor de reproducción. No se expande en una lista de segmentos de medios. Las demás listas de reproducción se convierten en una lista de archivos abiertos que contiene sus rutas locales reproducibles y sus enlaces web.

### Apertura de medios desde el portapapeles

Copie un archivo o una carpeta en el Explorador de archivos, vuelva a Luna y presione [Ctrl]{.kbd}+[V]{.kbd}. Luna abre la primera ruta existente del portapapeles según el modo de apertura de archivos configurado.

### Reapertura de medios recientes

El submenú **Archivo \> Recientes** conserva listas separadas de archivos, carpetas y listas de reproducción abiertos recientemente. Elija una entrada para abrirla de nuevo. Cada lista tiene su propio comando **Borrar**, y una lista vacía muestra un mensaje deshabilitado en lugar de un comando.

El historial se almacena en la carpeta de configuración del usuario de Luna. Borrar una lista reciente elimina solo su historial; no elimina ningún medio.

### Asociaciones de archivos

El instalador puede registrar Luna para los tipos de archivo compatibles. También puede usar **Preferencias \> General \> Registrar extensiones de archivo** o **Anular el registro de extensiones de archivo**.

El registro agrega Luna a la lista de aplicaciones de Windows para los medios compatibles. Es posible que Windows todavía le exija seleccionar Luna en **Configuración \> Aplicaciones \> Aplicaciones predeterminadas** para que se convierta en el reproductor predeterminado.

## 5. Controles de reproducción {#5-playback-controls}

### Reproducir y pausar

Presione [Espacio]{.kbd} o [Intro]{.kbd}, elija **Reproductor \> Reproducir/Pausar**, o use el botón Reproducir/Pausar de la ventana principal.

Si un elemento terminado o descargado se puede volver a abrir, Reproducir/Pausar lo vuelve a cargar. En el nivel Principiante, Luna anuncia Reproducir o Pausar; el nivel Avanzado evita estos mensajes de estado más largos.

### Avanzar y retroceder

Presione [Flecha izquierda]{.kbd} o [Flecha derecha]{.kbd} para moverse un paso de desplazamiento. Hay movimientos mayores disponibles sin cambiar el paso seleccionado:

- [Mayús]{.kbd}+[Flecha izquierda]{.kbd} o [Mayús]{.kbd}+[Flecha derecha]{.kbd} se mueve dos pasos.
- [Ctrl]{.kbd}+[Mayús]{.kbd}+[Flecha izquierda]{.kbd} o [Ctrl]{.kbd}+[Mayús]{.kbd}+[Flecha derecha]{.kbd} se mueve cuatro pasos.
- [Inicio]{.kbd} se mueve al principio.
- [Fin]{.kbd} se mueve al final.

La cantidad de desplazamiento predeterminada es de 5 segundos. Elija otro valor en **Reproductor \> Cantidad de desplazamiento**, o presione [Mayús]{.kbd} más un número:

  Atajo                   Cantidad de desplazamiento
  ----------------------- ------------------------------------------------
  [Mayús]{.kbd}+[1]{.kbd} 1 segundo
  [Mayús]{.kbd}+[2]{.kbd} 5 segundos
  [Mayús]{.kbd}+[3]{.kbd} 10 segundos
  [Mayús]{.kbd}+[4]{.kbd} 20 segundos
  [Mayús]{.kbd}+[5]{.kbd} 30 segundos
  [Mayús]{.kbd}+[6]{.kbd} 1 minuto
  [Mayús]{.kbd}+[7]{.kbd} 2 minutos
  [Mayús]{.kbd}+[8]{.kbd} 3 minutos
  [Mayús]{.kbd}+[9]{.kbd} 5 minutos
  [Mayús]{.kbd}+[0]{.kbd} 10 minutos
  [Mayús]{.kbd}+[-]{.kbd} Valor personalizado de las Preferencias de audio

La cantidad seleccionada se guarda de inmediato. Cambiar **Valor de desplazamiento personalizado (segundos)** no selecciona automáticamente el valor personalizado; selecciónelo desde el menú o con [Mayús]{.kbd}+[-]{.kbd} cuando desee usarlo.

### Ir a un tiempo

Elija **Reproductor \> Ir al tiempo\...** o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[G]{.kbd}. Ingrese las horas, los minutos y los segundos que sean pertinentes para la duración actual. Luna impide que la posición seleccionada supere el final del archivo.

### Saltar por porcentaje

Elija **Reproductor \> Saltar al porcentaje** o use los atajos de porcentaje predeterminados:

- [Ctrl]{.kbd}+[1]{.kbd} hasta [Ctrl]{.kbd}+[9]{.kbd} saltan del 10% al 90%.
- [Ctrl]{.kbd}+[Mayús]{.kbd}+[1]{.kbd} hasta [Ctrl]{.kbd}+[Mayús]{.kbd}+[9]{.kbd} saltan del 15% al 95%.
- [Ctrl]{.kbd}+[0]{.kbd} o [Ctrl]{.kbd}+[Mayús]{.kbd}+[0]{.kbd} salta al 100%.

Los saltos por porcentaje requieren una duración conocida y, por lo tanto, no están disponibles en algunas transmisiones en vivo.

### Obtención de información de reproducción

Luna puede leer en voz alta información de reproducción sin mover el foco:

- [E]{.kbd} lee el tiempo transcurrido.
- [R]{.kbd} lee el tiempo restante.
- [T]{.kbd} lee la duración total.
- [P]{.kbd} lee el porcentaje actual.
- [V]{.kbd} lee el volumen actual.
- [S]{.kbd} lee la velocidad actual.
- [Mayús]{.kbd}+[P]{.kbd} lee el ajuste de tono.
- [B]{.kbd} lee el valor del balance izquierdo/derecho.

### Nombre de archivo, ruta y título del medio

Presione [F]{.kbd} repetidamente para obtener información progresivamente más detallada sobre el elemento actual:

1.  La primera pulsación lee el nombre del archivo, el nombre de la transmisión o el título del video de YouTube.
2.  Una segunda pulsación rápida lee la ruta local completa o la dirección de la transmisión.
3.  Una tercera pulsación rápida copia esa ruta o dirección al portapapeles.

Si hace una pausa de más de un breve instante, la secuencia vuelve a comenzar con el nombre mostrado.

Presione [I]{.kbd} para leer el título almacenado en los metadatos del medio. Es independiente del nombre del archivo; un archivo local puede no tener un título incrustado o puede tener un título distinto de su nombre.

### Volumen

Presione [Flecha arriba]{.kbd} o [Flecha abajo]{.kbd} para cambiar el volumen según el paso de volumen configurado. El paso predeterminado es de 5 puntos porcentuales.

- [Ctrl]{.kbd}+[Alt]{.kbd}+[Flecha arriba]{.kbd} establece el volumen en el máximo de Luna.
- [Ctrl]{.kbd}+[Alt]{.kbd}+[Flecha abajo]{.kbd} establece el volumen en 5%.
- [V]{.kbd} lee el volumen actual.

Luna permite la amplificación por encima del 100%, hasta el 2000%. Una amplificación alta puede aumentar el ruido de fondo y la distorsión de la fuente. La normalización dinámica y el limitador pueden ayudar a contener los picos, pero comience con un nivel moderado para proteger su audición y sus equipos.

### Velocidad de reproducción

Presione [Ctrl]{.kbd}+[Flecha arriba]{.kbd} o [Ctrl]{.kbd}+[Flecha abajo]{.kbd} para cambiar la velocidad de reproducción. Presione [Alt]{.kbd}+[Y]{.kbd} para volver a 1x. La velocidad está limitada de 0.5x a 4x, y el paso se puede configurar en la página de Preferencias de audio.

### Tono

Los cambios de tono no modifican la velocidad de reproducción.

- [Mayús]{.kbd}+[Flecha arriba]{.kbd} sube el tono.
- [Mayús]{.kbd}+[Flecha abajo]{.kbd} baja el tono.
- [Alt]{.kbd}+[Mayús]{.kbd}+[P]{.kbd} restablece el tono original.
- [Mayús]{.kbd}+[P]{.kbd} lee el ajuste actual en semitonos.

El tono está limitado a 12 semitonos por encima o por debajo del original. El paso se puede configurar de 0.001 a 12 semitonos.

### Balance estéreo

- [Ctrl]{.kbd}+[Flecha izquierda]{.kbd} mueve el sonido hacia la izquierda.
- [Ctrl]{.kbd}+[Flecha derecha]{.kbd} mueve el sonido hacia la derecha.
- [B]{.kbd} lee el porcentaje de balance actual.

Cero es el centro, los valores negativos son izquierda y los valores positivos son derecha. El paso de balance se puede configurar de 1 a 100 puntos porcentuales.

### Elección de un dispositivo de salida

Elija **Reproductor \> Tarjetas de sonido\...** o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[A]{.kbd}. Seleccione un dispositivo de salida de audio de Windows y presione Aceptar. El dispositivo seleccionado se guarda de inmediato.

### Video en pantalla completa

Elija **Reproductor \> Pantalla completa** o presione [F11]{.kbd} para entrar en el modo de pantalla completa o salir de él. Este comando solo está disponible mientras el elemento actual tiene video. Presionar [Esc]{.kbd} también sale del modo de pantalla completa.

### Elección de una pista de audio

Los archivos pueden contener varias pistas de audio, como diferentes idiomas o comentarios. Use **Reproductor \> Pista de audio**:

- Presione [Mayús]{.kbd}+[Av Pág]{.kbd} para la pista siguiente.
- Presione [Mayús]{.kbd}+[Re Pág]{.kbd} para la pista anterior.
- Elija **Lista de pistas\...** o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[T]{.kbd} para seleccionar de una lista.

Luna describe cada pista usando su título, la disposición de canales y el idioma cuando esa información está disponible. Al pasar de la primera o de la última pista de audio no se vuelve al otro extremo.

### Lectura de subtítulos en voz alta

El submenú **Reproductor \> Subtítulos** enumera las pistas de subtítulos del archivo actual. Elija una pista para que se lea en voz alta cada línea de subtítulo a medida que aparece, o elija **Desactivado** para dejar de leer los subtítulos. Presione [Ctrl]{.kbd}+[U]{.kbd} para activar o desactivar la última pista seleccionada.

También puede cargar subtítulos que no están incrustados en el video:

- Elija **Cargar desde archivo\...** o presione [Ctrl]{.kbd}+[J]{.kbd} para un archivo de subtítulos de este equipo.
- Elija **Cargar desde URL\...** o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[J]{.kbd} para una dirección `http` o `https`.

Habilite **Recordar el subtítulo durante esta sesión**, o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[U]{.kbd}, para mantener el idioma de subtítulo seleccionado en toda la lista de reproducción actual. Esta elección dura solo hasta que Luna se cierre. Si el archivo siguiente no tiene subtítulo en ese idioma, Luna deja sin cambios su selección de subtítulo.

De forma predeterminada, Luna termina de leer una línea de subtítulo antes de leer la siguiente. Habilite **Interrumpir la línea actual cuando aparece un nuevo subtítulo** en la página General de Preferencias si es más importante mantener el ritmo de la imagen que escuchar cada línea completa.

## 6. Trabajo con varios archivos {#6-working-with-several-files}

### Anterior, siguiente, primero y último

- [Mayús]{.kbd}+[Tab]{.kbd} o [Re Pág]{.kbd} reproduce el elemento anterior.
- [Tab]{.kbd} o [Av Pág]{.kbd} reproduce el elemento siguiente.
- [Ctrl]{.kbd}+[Inicio]{.kbd} reproduce el primer elemento.
- [Ctrl]{.kbd}+[Fin]{.kbd} reproduce el último elemento.

Si **Volver al principio con varios archivos** está habilitado, Siguiente en el último elemento vuelve al primero y Anterior en el primero vuelve al último. El avance automático sigue la misma regla.

### Ir a un número de archivo

Elija **Reproductor \> Ir al archivo\...** o presione [Ctrl]{.kbd}+[G]{.kbd}. Ingrese un número desde 1 hasta la cantidad de elementos cargados.

### Cuadro de diálogo Archivos abiertos

Elija **Archivo \> Archivos abiertos\...** o presione [F2]{.kbd}. El elemento actual aparece seleccionado en la lista.

- Elija un elemento y active **Saltar al seleccionado**, o presione [Intro]{.kbd} sobre él, para reproducirlo.
- Active **Información de la lista de reproducción** para calcular la cantidad de archivos, el tamaño total, la duración total, el tiempo transcurrido en la lista de reproducción y el tiempo restante. El análisis es cancelable y el resultado se abre en una ventana de texto de solo lectura.

La lista está diseñada para seguir respondiendo incluso con una cantidad muy grande de archivos cargados.

### Reproducción aleatoria

Elija **Reproductor \> Reproducción aleatoria** o presione [Ctrl]{.kbd}+[Z]{.kbd}. Al habilitar la reproducción aleatoria se genera un orden de reproducción al azar manteniendo seleccionado el elemento actual.

Mientras la reproducción aleatoria está activada, Anterior, Siguiente, Primer archivo, Último archivo, Ir al archivo y el cuadro de diálogo Archivos abiertos operan sobre el orden aleatorio. Al desactivarla se restablece el orden normal de la lista conservando el elemento actual.

### Repetición y comportamiento al final del archivo

Elija **Reproductor \> Repetir archivo** o presione [Ctrl]{.kbd}+[R]{.kbd} para repetir el elemento actual. Esta opción de tiempo de ejecución tiene prioridad mientras está habilitada.

La página de Preferencias de audio también controla la acción normal al final de un archivo:

- **Avanzar al siguiente archivo** inicia el elemento siguiente.
- **Repetir el archivo** inicia de nuevo el mismo elemento.
- **No hacer nada** deja la reproducción al final.

### Recordar posiciones

Dos opciones cumplen propósitos diferentes:

- **Recordar la posición del último archivo** restaura el archivo local activo y el tiempo de reproducción la próxima vez que se inicie Luna.
- **Guardar la posición actual de cada archivo** recuerda una posición independiente para cada elemento local y vuelve a ella cuando se revisita ese elemento.

Si ambas están deshabilitadas, los archivos normalmente comienzan desde el principio.

## 7. Bucles A-B {#7-a-b-loops}

Un bucle A-B repite una parte del elemento actual.

1.  Desplácese hasta el comienzo deseado y presione [\[]{.kbd}. Esto establece el punto A.
2.  Desplácese hasta una posición posterior y presione [\]]{.kbd}. Esto establece el punto B y comienza el bucle de A a B.
3.  Presione [Retroceso]{.kbd} para borrar el bucle y continuar desde el punto B.

Mientras un bucle está activo, el desplazamiento normal queda limitado al rango seleccionado. [Inicio]{.kbd} va al punto A y [Fin]{.kbd} va al punto B. El punto B debe ser posterior al punto A.

La selección se aplica al elemento actual y se borra cuando la reproducción pasa a otro elemento.

## 8. Eliminación de silencios y procesamiento de audio {#8-silence-removal-and-audio-processing}

### Activación o desactivación de la eliminación de silencios

Elija **Reproductor \> Habilitar el filtro de eliminación de silencios** o presione [Ctrl]{.kbd}+[M]{.kbd}. Luna guarda de inmediato el estado de activado/desactivado.

La eliminación de silencios acorta las secciones silenciosas mientras se reproducen los medios. No modifica el archivo original. Los resultados dependen en gran medida de la fuente: un umbral agresivo puede tratar el habla o la música suaves como silencio.

### Configuración básica de silencios

Abra **Preferencias \> Eliminación de silencios**.

- **Duración mínima del silencio (segundos)** controla cuánto debe durar una sección silenciosa para poder acortarse. Aumentarla conserva más pausas cortas.
- **Umbral de silencio** es el nivel, en decibeles, por debajo del cual el audio se considera silencio. Un valor menos negativo, como -20, trata el audio más fuerte como silencio; un valor más negativo, como -50, limita la eliminación al audio más silencioso.

Los valores predeterminados son 0.5 segundos y -30 dB.

### Configuración avanzada de silencios

Habilite **Mostrar configuración avanzada** para configurar con mayor precisión el filtro de eliminación de silencios subyacente:

- **Partes silenciosas iniciales a recortar**: 0 conserva el comienzo; 1 elimina el silencio inicial hasta encontrar un sonido sostenido. Los valores más altos continúan a través de más secciones de sonido.
- **Sonido requerido antes de que termine el recorte inicial**: cuánto tiempo debe permanecer el audio por encima del umbral antes de que Luna lo conserve.
- **Partes silenciosas a recortar después de que comienza el audio**: -1 acorta todas las pausas posteriores que cumplan los requisitos, 0 conserva el silencio posterior y un valor positivo limita la cantidad que se acorta.
- **Duración mínima del silencio interno**: la pausa mínima que cumple los requisitos después de que el sonido ha comenzado.
- **Pausa a conservar después del silencio recortado**: retiene una parte de cada pausa para que las palabras no se junten.
- **Tamaño de la ventana de detección**: el intervalo usado en cada medición de volumen. Las ventanas más grandes son más estables; las más pequeñas reaccionan más rápido.
- **Modo de detección**: Pico reacciona a la muestra más fuerte y a los sonidos breves; RMS usa la energía promedio para una detección más suave.

Haga cambios pequeños y pruébelos con material representativo. El filtro afecta la reproducción de inmediato después de aceptar las Preferencias.

### Normalización, limitación y mono

La página de Preferencias de audio contiene dos filtros adicionales:

- **Habilitar normalización dinámica y limitador** uniforma el volumen cambiante y contiene los picos para reducir la saturación.
- **Reproducir el audio en mono** combina los canales izquierdo y derecho en una única salida mono.

Ninguna de las dos opciones modifica el archivo de origen.

### Ecualizador

El ecualizador realza y atenúa rangos de frecuencia individuales. Elija **Reproductor \> Ecualizador** y seleccione una entrada de la lista. El cambio surte efecto de inmediato y se recuerda entre sesiones.

La lista comienza con **Desactivado**, seguido de seis ajustes preestablecidos para problemas de escucha comunes:

- **Aumento de graves** y **Reducción de graves** suben o bajan los graves. El efecto es más fuerte por debajo de 100 Hz, sigue siendo considerable a 170 Hz y se desvanece a los 600 Hz, por lo que abarca el rango que ocupan un bombo y un bajo eléctrico sin engrosar las voces.
- **Aumento de agudos** sube todo desde aproximadamente 4 kHz hacia arriba.
- **Auriculares** añade peso a los graves, suaviza el rango de 3.5 kHz donde los auriculares suenan estridentes y abre la parte alta.
- **Voz** elimina el retumbo y el sonido encajonado y realza el rango de presencia, que es lo que hace que las consonantes se distingan. Es adecuado para audiolibros, podcasts y conferencias más que para música.
- **Sonoridad** sube ambos extremos y deja intacto el centro, para escuchar a volumen bajo, donde el oído es menos sensible a ellos.

Luego vienen los ajustes preestablecidos por género: Clásica, Club, Dance, En vivo, Pop, Reggae, Rock, Ska, Suave, Rock suave y Techno. **Personalizado** es el último. No cambia nada hasta que usted lo edite, y es el punto de partida para sus propios ajustes preestablecidos.

Un ajuste preestablecido que realza un rango hace que el sonido sea más fuerte y no solo diferente, y ningún rango se atenúa para hacer que el cambio parezca mayor. Por lo tanto, el material que ya se masterizó cerca del límite puede saturarse. **Habilitar normalización dinámica y limitador** en la página de Preferencias de audio evita esto y está activado de forma predeterminada; si lo desactiva, baje el volumen en su lugar.

**Desactivado** aplana todas las bandas en lugar de quitar el ecualizador. El ajuste preestablecido que usó por última vez se recuerda mientras el ecualizador está desactivado, por lo que al seleccionarlo de nuevo se vuelve a él.

### Edición de un ajuste preestablecido

Elija **Reproductor \> Ecualizador \> Editar el ajuste preestablecido actual** para abrir el ajuste al que está configurado el ecualizador. Todos los ajustes preestablecidos se pueden editar, incluidos los que suministra Luna.

El cuadro de diálogo contiene un grupo por banda: un filtro shelf de graves, quince bandas de 25 Hz a 16 kHz y un filtro shelf de agudos. Cada grupo contiene tres campos.

- **Frecuencia** es el centro de la banda, o el punto donde gira un filtro shelf.
- **Ganancia** es cuánto se sube o se baja la banda, en decibeles, de -20 a 20.
- **Q** es el ancho de la banda, de 0.1 a 10. Los valores más grandes son más estrechos.

Mientras el ajuste preestablecido que se edita es el que está en uso, cada cambio se aplica a medida que escribe, de modo que puede juzgarlo de oído antes de conservarlo. **Guardar** conserva el resultado; **Cancelar** restablece el ajuste preestablecido tal como estaba.

**Ajuste preestablecido base** completa todas las bandas a partir de otro ajuste preestablecido. Úselo para empezar desde algo cercano a lo que desea. **Restablecer valores predeterminados** devuelve las bandas a la forma en que se suministró el ajuste preestablecido elegido allí.

Al editar un ajuste preestablecido que suministra Luna, solo se registran los valores que usted cambió, de modo que las bandas que deje intactas siguen a Luna y las mejoras posteriores de ese ajuste preestablecido siguen llegándole. Al restablecer, se descartan esos cambios. El nombre de un ajuste preestablecido suministrado no se puede cambiar.

### Administración de ajustes preestablecidos

Elija **Reproductor \> Ecualizador \> Administrar ajustes preestablecidos** para ver la lista completa.

- **Nuevo** crea un ajuste preestablecido, a partir del ajuste base que elija en el editor.
- **Editar** abre el ajuste preestablecido seleccionado.
- **Eliminar** lo quita después de una confirmación.
- **Cerrar** sale de la lista.

Editar y Eliminar se aplican a sus propios ajustes preestablecidos y están ocultos mientras está seleccionado uno que suministra Luna; use **Editar el ajuste preestablecido actual** para esos. Para copiar un ajuste preestablecido, cree uno nuevo y elija el original como base.

Los ajustes preestablecidos que cree aparecen en el menú Ecualizador junto con los demás y se almacenan en `equalizer.json`, junto al archivo de configuración.

## 9. Administración de archivos locales {#9-local-file-management}

Los comandos de cambio de nombre, eliminación, carpeta contenedora y propiedades de Windows solo funcionan con archivos locales. No están disponibles para transmisiones de red ni para medios de YouTube.

### Mostrar el archivo en Windows

- Presione [Ctrl]{.kbd}+[F]{.kbd} o elija **Archivo \> Abrir carpeta contenedora** para abrir el Explorador de archivos con el archivo actual seleccionado.
- Presione [Alt]{.kbd}+[Intro]{.kbd} o elija **Archivo \> Propiedades del archivo\...** para abrir el cuadro de diálogo de propiedades de Windows.

### Cambio de nombre de un archivo

Elija **Editar \> Cambiar nombre\...** o presione [Mayús]{.kbd}+[F2]{.kbd}. Escriba el nuevo nombre del archivo. Si omite la extensión, Luna conserva la extensión actual. Luna rechaza un nombre vacío o un nombre que ya se use en esa carpeta.

### Eliminación de un archivo

Elija **Editar \> Eliminar** o presione [Mayús]{.kbd}+[Supr]{.kbd}. Confirme el aviso para eliminar de forma permanente el archivo actual del disco y quitarlo de la lista cargada. Luna no mueve el archivo a la Papelera de reciclaje.

Eliminar no es lo mismo que **Cerrar archivo**. Cerrar quita un elemento solo de Luna; eliminar quita el archivo local del disco.

### Copia de un archivo

Presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[C]{.kbd} para colocar el elemento actual en el portapapeles de archivos de Windows. Péguelo en el Explorador de archivos o en otro programa que acepte archivos copiados.

### Cierre de archivos

- Presione [Ctrl]{.kbd}+[W]{.kbd} para quitar el elemento actual de Luna sin eliminarlo.
- Presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[W]{.kbd} para quitar todos los elementos cargados.

## 10. Archivos marcados y operaciones por lotes {#10-marked-files-and-batch-operations}

Las marcas le permiten reunir archivos locales mientras navega y luego operar sobre ellos en conjunto.

- Presione [Ctrl]{.kbd}+[K]{.kbd} para marcar o desmarcar el archivo actual.
- Presione [Ctrl]{.kbd}+[A]{.kbd} para marcar todos los archivos cargados o desmarcarlos todos cuando ya estén todos marcados.
- Presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[K]{.kbd} para borrar todas las marcas.
- Presione [K]{.kbd} para escuchar la cantidad de archivos marcados.

Cuando hay al menos un archivo marcado, el menú **Acciones para archivos marcados** pasa a estar disponible:

- **Copiar a la carpeta\...** copia todos los archivos marcados a una carpeta elegida.
- **Mover a la carpeta\...** los mueve y quita de Luna las entradas movidas correctamente.
- **Copiar al portapapeles** los coloca en el portapapeles de archivos de Windows.
- **Eliminar** solicita confirmación, elimina de forma permanente los archivos del disco sin usar la Papelera de reciclaje y quita de Luna las entradas procesadas correctamente.

Copiar y mover usan un cuadro de diálogo de progreso cancelable. Luna informa éxito total, éxito parcial, cancelación o error. Si algunos archivos fallan, los archivos procesados correctamente permanecen procesados.

## 11. Marcadores {#11-bookmarks}

Los marcadores guardan posiciones de reproducción con nombre para los archivos locales. Se almacenan por archivo y no están disponibles para transmisiones ni videos de YouTube.

### Agregar un marcador

1.  Reproduzca o desplácese hasta la posición deseada.
2.  Elija **Marcadores \> Agregar un marcador nuevo** o presione [Mayús]{.kbd}+[M]{.kbd}.
3.  Acepte el nombre generado basado en el tiempo o escriba un nombre descriptivo.

Los marcadores se ordenan por posición de reproducción. Los primeros diez en ese orden ocupan las ranuras expuestas por los atajos directos.

### Saltar a un marcador numerado

Presione [Alt]{.kbd}+[1]{.kbd} hasta [Alt]{.kbd}+[9]{.kbd} para las ranuras de la 1 a la 9. Presione [Alt]{.kbd}+[0]{.kbd} para la ranura 10. Luna anuncia cuando la ranura solicitada está vacía.

### Administración de marcadores

Elija **Marcadores \> Administrar marcadores** o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[M]{.kbd}. El cuadro de diálogo enumera los nombres y las posiciones de los marcadores del archivo actual.

- **Saltar** mueve la reproducción al marcador seleccionado.
- **Editar** cambia su nombre.
- **Eliminar** lo quita después de una confirmación.
- Al activar un marcador en la lista se salta directamente a él.

Use la página de Preferencias Copia de seguridad y restauración para exportar o importar toda la colección de marcadores.

## 12. YouTube {#12-youtube}

Las funciones de YouTube requieren una conexión a internet. La reproducción, la exploración de canales y las descargas también requieren los componentes de YouTube disponibles en la página de Preferencias de YouTube. La disponibilidad puede cambiar cuando YouTube modifica su servicio.

### Apertura de un enlace de YouTube

Elija **Archivo \> Abrir enlace de YouTube\...** o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[Y]{.kbd}. Ingrese la dirección de un video o de una lista de reproducción. Este comando no acepta direcciones de canales.

Algunas direcciones de YouTube identifican tanto un video como una lista de reproducción. Luna puede preguntar si desea reproducir el video o abrir la lista de reproducción, o seguir el comportamiento fijo seleccionado en las Preferencias de YouTube.

### Búsqueda en YouTube

Elija **Archivo \> Buscar en YouTube\...** o presione [Ctrl]{.kbd}+[Y]{.kbd}. Ingrese el texto de búsqueda y luego desplácese por la lista de resultados. De forma predeterminada, Luna ofrece sugerencias de búsqueda mientras escribe. Puede desactivarlas en las Preferencias de YouTube si no desea solicitudes en segundo plano mientras ingresa una consulta.

- Presione [Intro]{.kbd} sobre un resultado seleccionado o active **Reproducir** para reproducirlo con video.
- Presione [Ctrl]{.kbd}+[Intro]{.kbd} o active **Reproducir como audio** para reproducir solo su sonido.
- Active **Descargar** o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[D]{.kbd} para elegir cómo guardarlo.
- Presione [Ctrl]{.kbd}+[D]{.kbd} para descargarlo de inmediato con los valores predeterminados guardados.
- Presione [Ctrl]{.kbd}+[Q]{.kbd} para agregarlo a la cola de descargas.
- Presione [Ctrl]{.kbd}+[Espacio]{.kbd} para agregarlo a Favoritos o quitarlo de allí.
- Presione [Ctrl]{.kbd}+[C]{.kbd} para copiar el enlace del resultado seleccionado.
- Presione [Ctrl]{.kbd}+[B]{.kbd} para abrirlo en el navegador predeterminado.
- Presione [Ctrl]{.kbd}+[N]{.kbd} para abrir el canal de quien lo subió en el navegador predeterminado.
- Presione [Ctrl]{.kbd}+[G]{.kbd} para explorar ese canal dentro de Luna.
- Abra el menú contextual para las mismas acciones de favoritos, copiar, navegador, canal, descarga y cola.

Cuando la selección llega al final de la página de resultados actual, Luna solicita más resultados automáticamente hasta que YouTube ya no tenga más para devolver.

Las ventanas de resultados de listas de reproducción y canales funcionan de la misma manera. Siguiente avanza por la sesión de resultados de YouTube activa, obteniendo el siguiente video cuando es necesario. Presionar [Esc]{.kbd} en la ventana principal después de que comienza la reproducción puede volver a la lista de resultados relacionada cuando esa sesión sigue disponible.

Los resultados de un canal se organizan en pestañas. Una pestaña se carga cuando la abre por primera vez y luego permanece disponible mientras la ventana del canal sigue abierta.

### Calidad de reproducción

Abra **Preferencias \> YouTube**.

- **Calidad de video** selecciona una altura de imagen preferida para la reproducción iniciada con [Intro]{.kbd}.
- **Calidad de audio** selecciona una tasa de bits preferida para la reproducción solo de audio iniciada con [Ctrl]{.kbd}+[Intro]{.kbd}.

Estas son opciones de reproducción. La calidad y el formato de descarga tienen sus propios valores predeterminados en la misma página de Preferencias.

### Opciones de video

Mientras un video de YouTube está activo, el menú **Opciones de video** ofrece:

- **Descargar\...** o [Ctrl]{.kbd}+[Mayús]{.kbd}+[D]{.kbd}: elija el nombre, el destino, el formato, la calidad y las opciones de procesamiento adicionales.
- **Descarga directa** o [Ctrl]{.kbd}+[D]{.kbd}: comienza de inmediato con los valores predeterminados de las Preferencias de YouTube.
- **Agregar a la cola de descargas** o [Ctrl]{.kbd}+[Q]{.kbd}: agrega el video actual con esos valores predeterminados sin iniciar la cola.
- **Descripción del video\...** o [Alt]{.kbd}+[D]{.kbd}: obtiene el título y la descripción de quien lo subió y los muestra en una ventana de texto de solo lectura.
- **Copiar enlace del video**: copia la dirección estable de visualización de YouTube en lugar de la dirección temporal de la transmisión multimedia.
- **Abrir el video en el navegador** y **Abrir el canal en el navegador**: abren la página correspondiente de YouTube en el navegador predeterminado.
- **Ir al canal**: explora el canal de quien lo subió dentro de Luna.

Una descarga repetida no sobrescribe un archivo existente con el mismo nombre; Luna elige un nombre numerado sin usar.

### Elección de las opciones de descarga

El cuadro de diálogo Descargar comienza con los valores predeterminados de las Preferencias de YouTube y le permite cambiarlos para un solo elemento. Puede elegir:

- Un nombre de archivo y una carpeta de destino.
- Salida de video o solo de audio.
- Calidad de video y MP4 o MKV para video.
- MP3, M4A, Opus, FLAC o WAV y una tasa de bits de audio para la salida solo de audio.
- Cuántos fragmentos descarga yt-dlp a la vez.
- Si se incrustan metadatos, una miniatura, capítulos o subtítulos.
- Si se deben eliminar las secciones de SponsorBlock.

Elija **Descargar** para comenzar de inmediato. Su ventana de progreso no modal informa el tamaño descargado, el porcentaje, la velocidad, el tiempo estimado y las tareas finales, como combinar, convertir o incrustar información. Puede seguir usando Luna mientras esta ventana está abierta. Elija **Minimizar a la cola** para ocultar la ventana de progreso individual y continuar la descarga en la cola.

Elija **Agregar a la cola** en su lugar para conservar todas las opciones elegidas en el cuadro de diálogo como configuración de ese elemento de la cola.

### Cola de descargas

Presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[Q]{.kbd} o elija **Herramientas \> Cola de descargas\...** para abrir la cola. La lista muestra el título de cada video y su estado actual. Un enlace ingresado manualmente se muestra como el título cuando Luna no ha consultado sus metadatos.

- **Agregar\...** acepta un enlace de video de YouTube por línea.
- **Iniciar** comienza todos los elementos de la cola y reintenta los elementos fallidos.
- **Detener** cancela la descarga activa seleccionada y la devuelve a la cola para iniciarla más tarde.
- **Detener todo** cancela todas las descargas activas y deja los elementos sin terminar en la cola.
- Presione [Supr]{.kbd} para quitar el elemento seleccionado después de una confirmación. Quitar un elemento activo también lo cancela.
- **Cerrar** oculta la ventana de la cola sin detener las descargas activas.

El cuadro de estado y la barra de progreso total aparecen solo mientras la cola contiene elementos. Durante un lote en ejecución, informan el elemento actual, el porcentaje total, la velocidad combinada y el tiempo estimado restante. Un elemento descargado puede permanecer en estado de finalización mientras yt-dlp convierte, combina, incrusta o mueve su salida.

La cola ejecuta hasta el límite de **Descargas simultáneas** de las Preferencias de YouTube. Las filas terminadas permanecen disponibles para su revisión hasta que se cierra la ventana de la cola, y luego se borran. Luna envía una notificación de escritorio cuando todos los elementos visibles de la cola terminan correctamente.

Si cierra Luna mientras la cola todavía tiene elementos sin terminar, Luna pregunta si desea salir y cancelarlos o volver a la aplicación.

### Videos y transmisiones favoritos

Los favoritos guardan enlaces, mientras que los marcadores guardan posiciones dentro de archivos locales. Abra los favoritos con **Archivo \> Videos favoritos\...** o [Ctrl]{.kbd}+[Mayús]{.kbd}+[F]{.kbd}.

El cuadro de diálogo Favoritos puede abrir, agregar, editar o quitar entradas. Cada entrada tiene un nombre, un enlace y un tipo:

- **Video** requiere un enlace de video de YouTube.
- **Lista de reproducción** requiere un enlace de lista de reproducción de YouTube.
- **Enlace combinado** requiere una dirección que contenga identificadores de video y de lista de reproducción, y abre su video.
- **Transmisión genérica** acepta cualquier dirección de transmisión `http` o `https`.

Luna valida el formato de la dirección cuando se guarda una entrada. Comprueba si el contenido remoto sigue disponible solo cuando usted lo abre.

### Componentes de YouTube y cookies

Luna usa yt-dlp y un entorno de ejecución de JavaScript para la reproducción de YouTube, la exploración de canales y las descargas. La búsqueda, los listados de listas de reproducción, las descripciones y las sugerencias de búsqueda usan el cliente de metadatos de Luna, pero un resultado no se puede reproducir hasta que los componentes estén instalados. Use **Descargar componentes de YouTube** en Preferencias para instalarlos o actualizarlos.

La página de componentes ofrece tres canales de actualización:

- **Stable** cambia con menos frecuencia y se recomienda para el uso normal.
- **Nightly** sigue las compilaciones nocturnas de yt-dlp.
- **Master** sigue el código fuente más reciente y puede ser menos confiable.

Use **Ayuda \> Actualizaciones \> Actualizar componentes de YouTube** para actualizar un yt-dlp instalado. Habilite **Buscar actualizaciones de yt-dlp al iniciar** para una comprobación en segundo plano que solo muestra un aviso cuando hay una actualización disponible.

Algunos videos con restricción de edad, solo para miembros o protegidos por verificación requieren cookies del navegador. Puede elegir un archivo de cookies en formato Netscape o usar **Importar desde Firefox** para que Luna lea las cookies actuales de Firefox en cada solicitud. Estas opciones son mutuamente excluyentes, y el estado de cookies de solo lectura informa qué origen está activo. Trate los archivos de cookies exportados como datos privados de la cuenta.

## 13. Grabación de audio {#13-recording-audio}

Luna puede grabar micrófonos y otras entradas, todo lo que se reproduce a través de un dispositivo de salida, o el audio de un programa en particular. Se pueden mezclar varias fuentes en una sola grabación con niveles de volumen independientes.

### Grabación rápida con el micrófono predeterminado

Si no se han creado fuentes en la sesión actual, presione [F9]{.kbd} para grabar el dispositivo de entrada predeterminado de Windows usando las Preferencias de grabación guardadas. Presione [F7]{.kbd} para pausar o reanudar, y [F8]{.kbd} para detener.

Un tono ascendente confirma que la grabación está comenzando. Un tono descendente confirma que se ha detenido. El inicio y la detención correctos no agregan un mensaje hablado sobre esos tonos.

### Apertura de la interfaz de grabación

Elija **Grabación \> Abrir la interfaz de grabación\...** o presione [Alt]{.kbd}+[R]{.kbd}. La ventana tiene una lista de fuentes, controles de fuente, configuración de salida y controles de Iniciar/Pausar.

Cerrar esta ventana no detiene una grabación activa. La grabación pertenece a la aplicación y continúa hasta que use Detener, [F8]{.kbd}, o salga de Luna.

### Agregar fuentes

Active **Agregar** y elija:

- **Dispositivo de entrada\...** para un micrófono, una entrada de línea u otro dispositivo de captura de Windows.
- **Salida del sistema\...** para todo lo que se reproduce a través de un dispositivo de altavoces o auriculares seleccionado.
- **Programa\...** para el audio de un solo programa. Esto requiere Windows 10 versión 2004 o posterior.

Dé a la fuente un nombre descriptivo, elija el dispositivo o el programa y establezca su volumen. Una fuente de dispositivo puede seguir el dispositivo predeterminado actual de Windows en lugar de un dispositivo con nombre.

Para una fuente de programa, **Capturar todo excepto esta aplicación** invierte la selección y captura en cambio el audio de otras aplicaciones. Solo aparecen en la lista los programas con una sesión de audio de Windows; inicie audio en el programa de destino si falta.

Use **Editar\...** para cambiar la fuente seleccionada y **Quitar\...** para eliminarla de la sesión. El control deslizante de volumen de la fuente cambia su nivel en la mezcla final.

Las fuentes son solo de sesión. Permanecen cuando se cierra y se vuelve a abrir la ventana de grabación durante la misma sesión de Luna, pero no se restauran después de que Luna se cierra porque los dispositivos y los identificadores de proceso pueden dejar de representar las mismas fuentes.

### Formato y destino de la grabación

Elija el formato, la frecuencia de muestreo, la cantidad de canales, la calidad donde corresponda y la carpeta de destino:

- **WAV** no tiene compresión y normalmente produce el archivo más grande.
- **FLAC** no tiene pérdida y normalmente es más pequeño que WAV.
- **MP3** y **AAC** tienen pérdida y usan la tasa de bits seleccionada para equilibrar tamaño y calidad.
- **Mono** suele ser suficiente para un micrófono o para voz.
- **Estéreo** conserva canales izquierdo y derecho separados para música y salida del sistema.

Las frecuencias de muestreo, las cantidades de canales y las tasas de bits disponibles se obtienen de los codificadores instalados con Windows. Por lo tanto, las opciones cambian según el formato seleccionado y pueden variar según el sistema. Luna asigna a cada grabación un nombre con marca de tiempo para evitar reemplazar un archivo anterior.

Los cambios realizados dentro de la interfaz de grabación se aplican a su sesión. Para cambiar los valores predeterminados usados después del próximo inicio o por los atajos directos, use **Preferencias \> Grabación**.

### Inicio, pausa y detención

La ventana de grabación requiere al menos una fuente configurada para que **Iniciar** pase a estar disponible. Una vez que comienza la grabación, los controles de fuente y de salida se bloquean para que el formato de grabación y la mezcla se mantengan coherentes.

- El botón **Iniciar** pasa a ser **Detener** durante la grabación.
- El botón **Pausar** pasa a ser **Reanudar** mientras está en pausa.
- [F9]{.kbd}, [F7]{.kbd} y [F8]{.kbd} funcionan desde la ventana principal de Luna.
- Los atajos globales correspondientes funcionan incluso cuando otra aplicación tiene el foco.

Si solo se pueden abrir algunas fuentes, Luna comienza con las fuentes disponibles y enumera las que fallaron. Si no se puede abrir ninguna, la grabación no comienza.

Elija **Grabación \> Abrir carpeta de grabaciones** para abrir el destino activo. La carpeta predeterminada es `Documents\Luna Player\Recordings`.

## 14. IPTV {#14-iptv}

Las fuentes y los canales de IPTV están en **Herramientas \> IPTV**. Luna admite listas de reproducción M3U o M3U8 extendidas desde un archivo o una dirección web, cuentas de Xtream Codes y portales Stalker.

### Administración de fuentes

Elija **Herramientas \> IPTV \> Administrar fuentes de IPTV\...** o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[I]{.kbd}. La lista de fuentes ofrece los comandos **Abrir**, **Agregar\...**, **Editar\...** y **Quitar\...**.

Cada fuente tiene un nombre y un tipo. Según ese tipo, Luna solicita una ruta o dirección de lista de reproducción, un servidor Xtream con nombre de usuario y contraseña, o un portal Stalker con dirección MAC y número de serie opcional. También puede indicar una dirección opcional de guía de programación XMLTV. Los canales en vivo de Xtream se pueden abrir como HLS en lugar de como transmisiones de transporte cuando **Abrir los canales en vivo como HLS** está habilitado.

Las credenciales de IPTV guardadas están protegidas para el usuario actual de Windows. No comparta el archivo de datos de fuentes de IPTV porque sigue perteneciendo a su cuenta y a su equipo.

### Exploración de canales

Abra una fuente desde el administrador, o elija **Herramientas \> IPTV \> Explorar canales\...** para volver a abrir la fuente actual. El explorador de canales puede filtrar por TV en vivo, películas o series, por categoría y por el texto escrito en el cuadro Buscar.

Presione [Intro]{.kbd} sobre una fila seleccionada o active **Reproducir** para abrirla. Cuando hay información de la guía de programación disponible, presione [Ctrl]{.kbd}+[I]{.kbd} para escuchar qué se está emitiendo ahora y qué sigue. Presione [F1]{.kbd} en el explorador de canales para escuchar un resumen de sus controles.

## 15. Convertidor de medios {#15-media-converter}

Elija **Herramientas \> Convertidor de medios\...** para convertir uno o más archivos multimedia a un formato de audio. Agregue archivos individuales o agregue una carpeta; al agregar una carpeta se incluyen los medios compatibles de sus subcarpetas. Las rutas duplicadas se ignoran.

El convertidor escribe AAC, AC-3, AIFF, FLAC, M4A, MKA, MP3, OGA, OGG, Opus, WAV y WMA. Elige un formato inicial razonable a partir del primer archivo de origen, pero usted puede cambiarlo. Según el formato de salida, también puede elegir una frecuencia de muestreo, canales mono o estéreo, tasa de bits, y el modo y el perfil de tasa de bits de Opus.

Establezca **Máximo de conversiones simultáneas** para controlar cuántos archivos se procesan a la vez. De forma predeterminada, los archivos convertidos se escriben junto a sus orígenes. Desmarque **Conservar las ubicaciones originales de los archivos** para enviar todos los resultados a una única carpeta de destino.

**Eliminar los archivos originales después de la conversión** quita cada origen solo después de que ese archivo se convierte correctamente. Use esta opción con cuidado; los originales eliminados no se envían a la Papelera de reciclaje.

La ventana de progreso informa el archivo actual, su porcentaje, la cantidad de archivos restantes, el porcentaje total y el tiempo estimado. Puede cancelar el trabajo restante. Cuando termina la conversión, Luna informa éxito total o enumera los archivos que fallaron y el motivo.

## 16. Temporizador de sueño {#16-sleep-timer}

Elija **Herramientas \> Temporizador de sueño \> Establecer temporizador\...** o presione [Ctrl]{.kbd}+[Mayús]{.kbd}+[S]{.kbd}. Un temporizador puede activarse después de una duración elegida o al final de la pista actual. Elija si pausa la reproducción o detiene y descarga el elemento actual.

Para una duración, elija 15, 30, 45, 60, 90 o 120 minutos, o ingrese horas y minutos personalizados. Un temporizador de fin de pista requiere un elemento abierto e impide el avance automático normal cuando ese elemento termina.

Habilite **Reducir gradualmente el volumen antes de activarse** para bajar el volumen durante los últimos 1 a 120 segundos. Luna restablece el volumen original después de que el temporizador se activa o se cancela; el valor reducido no se guarda como su volumen preferido.

- Presione [Ctrl]{.kbd}+[T]{.kbd} o elija **Tiempo restante** para escuchar el tiempo restante o que el temporizador está esperando el final de la pista.
- Elija **Cancelar temporizador** para desactivarlo. Abrir la ventana del temporizador mientras hay uno activo también ofrece **Desactivar temporizador**.
- Luna anuncia una advertencia de un minuto para los temporizadores de duración.

Las últimas opciones del temporizador se recuerdan, pero una cuenta regresiva activa no sobrevive al cierre de Luna.

## 17. Referencia de Preferencias {#17-preferences-reference}

Abra las Preferencias con **Archivo \> Preferencias\...** o [Ctrl]{.kbd}+[P]{.kbd}. Seleccione una página en el árbol de categorías. Presione Aceptar para validar y aplicar todas las páginas, o Cancelar para descartar los cambios no aceptados.

Presione [F1]{.kbd} mientras una opción tiene el foco para escuchar una explicación detallada de esa opción.

### General

  Opción                                                                      Finalidad
  --------------------------------------------------------------------------- ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Idioma                                                                      Usa el idioma de visualización de Windows o una traducción incluida. Reinicie Luna después de cambiarlo.
  Tema                                                                        Sigue a Windows o fuerza una apariencia clara u oscura. El alto contraste siempre sigue a Windows. Reinicie Luna después de cambiarlo.
  Recordar la posición del último archivo                                     Restaura el último archivo local activo y su posición en el próximo inicio.
  Leer el nombre del archivo al navegar                                       Anuncia el elemento al que se llega con Anterior o Siguiente.
  Buscar actualizaciones de la aplicación al iniciar                          Comprueba en silencio después del inicio y solo muestra un aviso cuando hay una versión más nueva lista.
  Guardar la configuración al cerrar                                          Guarda los cambios de la sesión, como el volumen y la velocidad, cuando Luna se cierra. Los cambios de Preferencias aceptados explícitamente se siguen guardando de inmediato.
  Abrir los archivos en la ventana existente cuando Luna ya está en ejecución Envía los archivos abiertos desde Windows a la ventana actual de Luna. Desmárquela para abrir cada solicitud en una ventana independiente.
  Deshabilitar los controles multimedia del sistema                           Quita a Luna de la superposición multimedia de Windows e ignora las teclas multimedia de hardware.
  Mostrar la pista en reproducción en el título de la ventana                 Muestra el elemento actual y si está en pausa en la barra de título de Luna. Está desactivada de forma predeterminada para evitar anuncios repetidos del lector de pantalla.
  Interrumpir la línea actual cuando aparece un nuevo subtítulo               Permite que un nuevo subtítulo hablado corte el anterior para mantener el ritmo de la imagen.
  Nivel de detalle                                                            Elige los anuncios completos del nivel Principiante o los anuncios más breves del nivel Avanzado.
  Qué desea abrir con los archivos?                                           Controla si un archivo abierto trae consigo ningún archivo vecino, su carpeta, o su carpeta y subcarpetas.
  Registrar/Anular el registro de extensiones de archivo                      Agrega o quita los registros de medios compatibles de Luna para el usuario actual de Windows.

### Copia de seguridad y restauración

  Comando                                       Finalidad
  --------------------------------------------- -------------------------------------------------------------------------------------------------------
  Exportar configuración                        Guarda las preferencias aplicadas actualmente en un archivo JSON sin mover la copia de trabajo.
  Importar configuración                        Carga un archivo JSON de configuración válido y reemplaza los valores mostrados en Preferencias.
  Exportar marcadores                           Guarda todos los marcadores en un archivo JSON.
  Importar marcadores                           Reemplaza la colección de marcadores actual por una colección exportada válida.
  Restablecer configuración                     Restaura todas las preferencias y los atajos a sus valores predeterminados después de una confirmación.
  Abrir la carpeta de configuración del usuario Abre el directorio de configuración de Luna en el Explorador de archivos.

La importación reemplaza la colección correspondiente; no combina entradas. Guarde los archivos exportados en un lugar separado de la carpeta de configuración de Luna si están destinados a ser copias de seguridad.

### Audio

  Opción                                       Finalidad
  -------------------------------------------- --------------------------------------------------------------------------------------------------------------------
  Valor de desplazamiento personalizado        Cantidad de segundos usada por la cantidad de desplazamiento Personalizada; se aceptan decimales positivos como 2.5.
  Paso de velocidad                            Cantidad que suma o resta cada comando de velocidad.
  Paso de tono                                 Cambio de tono por pulsación, de 0.001 a 12 semitonos.
  Paso de volumen                              Cambio de volumen por pulsación, de 1 a 20 puntos porcentuales.
  Paso de balance                              Movimiento izquierda/derecha por pulsación, de 1 a 100 puntos porcentuales.
  Qué sucede cuando termina un archivo?        Avanza, repite o se detiene al final.
  Volver al principio con varios archivos      Conecta el final y el comienzo de una lista de varios elementos.
  Guardar la posición actual de cada archivo   Mantiene una posición de reanudación independiente para cada elemento local.
  Habilitar normalización dinámica y limitador Uniforma el volumen y contiene los picos.
  Reproducir el audio en mono                  Mezcla los canales izquierdo y derecho en mono.

### Eliminación de silencios

Consulte [Eliminación de silencios y procesamiento de audio](#8-silence-removal-and-audio-processing) para ver los controles básicos y avanzados.

### YouTube {#youtube}

  Opción                                                   Finalidad
  -------------------------------------------------------- --------------------------------------------------------------------------------------------------------------
  Calidad de video                                         Elige la altura de imagen preferida para la reproducción de video.
  Calidad de audio                                         Elige la tasa de bits preferida para la reproducción solo de audio.
  Cantidad de resultados de búsqueda                       Establece el objetivo inicial de 5 a 100; se cargan más resultados al llegar al final.
  Mostrar sugerencias de búsqueda al escribir              Obtiene y muestra posibles finalizaciones para una consulta de YouTube.
  Comportamiento del enlace de video+lista de reproducción Pregunta cada vez, siempre reproduce el video o siempre abre la lista de reproducción.
  Carpeta de descargas                                     Establece el destino base, o usa `Downloads\Luna Player` cuando se deja vacía.
  Ordenar las descargas por tipo                           Coloca los archivos en las subcarpetas `videos` y `audios`.
  Tipo de descarga predeterminado                          Elige video o audio para las descargas directas y la selección inicial del cuadro de diálogo Descargar.
  Calidad y formato de descarga de video                   Establece el límite de altura y el contenedor MP4 o MKV usados de forma predeterminada.
  Calidad y formato de descarga de audio                   Establece la tasa de bits y el formato MP3, M4A, Opus, FLAC o WAV usados de forma predeterminada.
  Fragmentos simultáneos                                   Establece cuántas partes de un archivo obtiene yt-dlp a la vez, de 1 a 16.
  Descargas simultáneas                                    Establece cuántos elementos de la cola pueden ejecutarse a la vez, de 1 a 16.
  Incrustar metadatos, miniatura, capítulos o subtítulos   Agrega la información seleccionada a los archivos descargados donde el formato de salida lo admita.
  Eliminar segmentos de SponsorBlock                       Corta de las descargas las secciones compatibles de patrocinio, introducción y similares.
  Archivo de cookies / Importar desde Firefox              Proporciona cookies de cuenta para los videos que las requieren. Solo puede haber un origen de cookies activo.
  Canal de actualización de yt-dlp                         Elige Stable, Nightly o Master.
  Buscar actualizaciones de yt-dlp al iniciar              Realiza una comprobación silenciosa del componente después del inicio.
  Descargar componentes de YouTube                         Instala o actualiza yt-dlp y las herramientas de apoyo.

### Grabación

Esta página establece los valores predeterminados usados por los atajos de grabación directa y alimenta la interfaz de grabación al comienzo de cada sesión de Luna. Contiene el formato de audio, la frecuencia de muestreo, los canales, la calidad de audio para los formatos comprimidos y la carpeta de grabaciones.

### Atajos de teclado

Esta página enumera todas las acciones que se pueden ejecutar mientras la ventana principal de Luna tiene el foco. Cada acción puede tener un atajo principal; algunas acciones también tienen una ranura secundaria predefinida.

1.  Seleccione una acción.
2.  Active **Editar atajo principal** o, donde esté disponible, **Editar atajo secundario**.
3.  Presione la combinación deseada. Presione [Esc]{.kbd} para cancelar la captura.
4.  Presione Aceptar en Preferencias para aplicar los cambios.

Luna rechaza una combinación ya asignada a otra acción del mismo conjunto de atajos. Los atajos locales pueden usar teclas imprimibles, teclas de navegación, teclas de función de F1 a F24 y los modificadores Ctrl, Mayús o Alt. Use **Restablecer valores predeterminados** para restaurar toda la página de atajos locales después de una confirmación.

### Atajos globales

Los atajos globales funcionan mientras otra aplicación tiene el foco. Se limitan intencionalmente a comandos de reproducción, navegación, volumen y grabación que resultan útiles fuera de Luna.

Seleccione una acción, active **Editar atajo** y presione la combinación deseada. Los atajos globales también pueden usar la tecla de Windows. Windows puede rechazar un atajo ya reservado por el sistema operativo o por otra aplicación; Luna informa los atajos que no pudo registrar.

Los atajos globales son independientes de los atajos locales. Cambiar uno no cambia el otro.

## 18. Actualizaciones de la aplicación {#18-application-updates}

### Comprobación manual

Elija **Ayuda \> Actualizaciones \> Buscar actualizaciones de la aplicación**. Aparece un cuadro de diálogo de progreso cancelable mientras Luna consulta la información de la versión.

Si hay una versión más nueva lista, Luna muestra las versiones instalada y disponible y una lista desplazable de cambios. Elija **Actualizar** para descargarla o **Más tarde** para dejar sin cambios la versión actual.

Luna verifica que el instalador o el archivo portátil real exista antes de ofrecer la actualización. Si la información de la versión se publicó antes de que su paquete terminara de subirse, una comprobación manual le pide que lo intente de nuevo más tarde en lugar de presentar una descarga que fallará.

### Comprobaciones al iniciar

Habilite **Buscar actualizaciones de la aplicación al iniciar** para una comprobación silenciosa en segundo plano. No muestra ninguna ventana de progreso ni mensaje de que no hay actualizaciones; solo aparece un aviso cuando hay un paquete compatible más nuevo disponible.

### Instalación de una actualización descargada

La ventana de descarga muestra el tamaño total, el tamaño descargado, el porcentaje y un botón Cancelar. Después de una descarga correcta, Luna inicia su pequeño asistente de actualización y se cierra:

- Una copia instalada inicia el instalador de la actualización. Su progreso de instalación permanece visible y Luna se abre cuando termina la instalación.
- Una copia portátil reemplaza los archivos extraídos de la aplicación y abre el reproductor actualizado.

No inicie otro proceso de Luna mientras se aplica la actualización.

## 19. Ayuda e información de versiones {#19-help-and-release-information}

- **Ayuda \> Guía del usuario** o [F1]{.kbd} abre la guía instalada que coincide con el idioma de la interfaz de Luna. Si no hay instalada una guía en el idioma regional o base correspondiente, Luna abre la guía en inglés.
- **Ayuda \> Acerca de** muestra una descripción, la versión, la información de derechos de autor y un enlace al sitio web del proyecto.
- **Ayuda \> Notas de la versión** abre la página de versiones de GitHub para la versión instalada en su navegador predeterminado.

La guía del usuario se instala como HTML local y no requiere una conexión a internet. Las notas de la versión y el sitio web del proyecto sí la requieren.

## 20. Configuración y datos del usuario {#20-settings-and-user-data}

Luna almacena los datos del usuario en `%APPDATA%\Luna Player`:

  Archivo             Contenido
  ------------------- ---------------------------------------------------------------------------------------------------------------------
  `settings.json`     Preferencias y personalizaciones de atajos
  `bookmarks.json`    Marcadores con nombre de archivos locales
  `positions.json`    Posiciones de reproducción por archivo
  `equalizer.json`    Ajustes preestablecidos creados por el usuario y cambios en los ajustes preestablecidos suministrados del ecualizador
  `favorites.json`    Enlaces favoritos de YouTube y de transmisiones
  `iptv-sources.json` Fuentes de IPTV guardadas, con los campos de cuenta protegidos para el usuario actual de Windows
  `recents.json`      Archivos, carpetas y listas de reproducción abiertos recientemente

Use **Preferencias \> Copia de seguridad y restauración \> Abrir la carpeta de configuración del usuario** para abrir la carpeta exacta en lugar de escribir la ruta.

No edite estos archivos mientras Luna se está ejecutando. Use los controles de exportación e importación de la configuración y los marcadores siempre que sea posible.

Si `settings.json` no se puede leer o contiene valores no válidos, Luna se inicia con los valores predeterminados, muestra el motivo y protege el archivo existente para que no se sobrescriba. Importe una copia de seguridad de configuración válida o use **Restablecer configuración** para reemplazarlo deliberadamente. Los marcadores, favoritos, ajustes preestablecidos del ecualizador, fuentes de IPTV y recientes no válidos también se informan en lugar de reemplazarse silenciosamente.

## 21. Referencia completa de atajos predeterminados {#21-complete-default-shortcut-reference}

Estos son los valores de fábrica. Es posible que sus asignaciones actuales difieran después de la personalización. La lista autorizada para su instalación está en **Preferencias \> Atajos de teclado** y **Atajos globales**.

### Archivos, información y Preferencias

  Acción                                                 Atajo principal                      Atajo secundario
  ------------------------------------------------------ ------------------------------------ ----------------
  Abrir archivos                                         [Ctrl]{.kbd}+[O]{.kbd}               ---
  Abrir un enlace de transmisión de red                  [Ctrl]{.kbd}+[L]{.kbd}               ---
  Abrir todos los archivos de una carpeta                [Ctrl]{.kbd}+[Mayús]{.kbd}+[O]{.kbd} ---
  Mostrar el archivo actual en el Explorador de archivos [Ctrl]{.kbd}+[F]{.kbd}               ---
  Propiedades del archivo de Windows                     [Alt]{.kbd}+[Intro]{.kbd}            ---
  Cuadro de diálogo Archivos abiertos                    [F2]{.kbd}                           ---
  Cerrar el archivo actual                               [Ctrl]{.kbd}+[W]{.kbd}               ---
  Cerrar todos los archivos                              [Ctrl]{.kbd}+[Mayús]{.kbd}+[W]{.kbd} ---
  Preferencias                                           [Ctrl]{.kbd}+[P]{.kbd}               ---
  Leer la información del archivo actual                 [F]{.kbd}                            ---
  Leer el título del medio actual                        [I]{.kbd}                            ---
  Salir de Luna                                          Ninguno                              ---

### Navegación por la lista de reproducción y modos

  Acción                                         Atajo principal             Atajo secundario
  ---------------------------------------------- --------------------------- ----------------
  Archivo anterior                               [Mayús]{.kbd}+[Tab]{.kbd}   [Re Pág]{.kbd}
  Archivo siguiente                              [Tab]{.kbd}                 [Av Pág]{.kbd}
  Primer archivo                                 [Ctrl]{.kbd}+[Inicio]{.kbd} ---
  Ir al número de archivo                        [Ctrl]{.kbd}+[G]{.kbd}      ---
  Último archivo                                 [Ctrl]{.kbd}+[Fin]{.kbd}    ---
  Activar o desactivar la reproducción aleatoria [Ctrl]{.kbd}+[Z]{.kbd}      ---
  Activar o desactivar la repetición del archivo [Ctrl]{.kbd}+[R]{.kbd}      ---

### Reproducción y desplazamiento

  Acción                                        Atajo principal                                     Atajo secundario
  --------------------------------------------- --------------------------------------------------- ----------------
  Reproducir o pausar                           [Espacio]{.kbd}                                     [Intro]{.kbd}
  Retroceder un paso                            [Flecha izquierda]{.kbd}                            ---
  Avanzar un paso                               [Flecha derecha]{.kbd}                              ---
  Retroceder dos pasos                          [Mayús]{.kbd}+[Flecha izquierda]{.kbd}              ---
  Avanzar dos pasos                             [Mayús]{.kbd}+[Flecha derecha]{.kbd}                ---
  Retroceder cuatro pasos                       [Ctrl]{.kbd}+[Mayús]{.kbd}+[Flecha izquierda]{.kbd} ---
  Avanzar cuatro pasos                          [Ctrl]{.kbd}+[Mayús]{.kbd}+[Flecha derecha]{.kbd}   ---
  Principio                                     [Inicio]{.kbd}                                      ---
  Final                                         [Fin]{.kbd}                                         ---
  Ir al tiempo                                  [Ctrl]{.kbd}+[Mayús]{.kbd}+[G]{.kbd}                ---
  Elegir tarjeta de sonido                      [Ctrl]{.kbd}+[Mayús]{.kbd}+[A]{.kbd}                ---
  Entrar en pantalla completa o salir de ella   [F11]{.kbd}                                         ---
  Abrir la lista de pistas de audio             [Ctrl]{.kbd}+[Mayús]{.kbd}+[T]{.kbd}                ---
  Pista de audio anterior                       [Mayús]{.kbd}+[Re Pág]{.kbd}                        ---
  Pista de audio siguiente                      [Mayús]{.kbd}+[Av Pág]{.kbd}                        ---
  Activar o desactivar la lectura de subtítulos [Ctrl]{.kbd}+[U]{.kbd}                              ---
  Cargar subtítulo desde un archivo             [Ctrl]{.kbd}+[J]{.kbd}                              ---
  Cargar subtítulo desde una dirección web      [Ctrl]{.kbd}+[Mayús]{.kbd}+[J]{.kbd}                ---
  Recordar el subtítulo durante esta sesión     [Ctrl]{.kbd}+[Mayús]{.kbd}+[U]{.kbd}                ---

### Selección de la cantidad de desplazamiento

  Acción                          Atajo
  ------------------------------- -----------------------
  Seleccionar paso de 1 segundo   [Mayús]{.kbd}+[1]{.kbd}
  Seleccionar paso de 5 segundos  [Mayús]{.kbd}+[2]{.kbd}
  Seleccionar paso de 10 segundos [Mayús]{.kbd}+[3]{.kbd}
  Seleccionar paso de 20 segundos [Mayús]{.kbd}+[4]{.kbd}
  Seleccionar paso de 30 segundos [Mayús]{.kbd}+[5]{.kbd}
  Seleccionar paso de 1 minuto    [Mayús]{.kbd}+[6]{.kbd}
  Seleccionar paso de 2 minutos   [Mayús]{.kbd}+[7]{.kbd}
  Seleccionar paso de 3 minutos   [Mayús]{.kbd}+[8]{.kbd}
  Seleccionar paso de 5 minutos   [Mayús]{.kbd}+[9]{.kbd}
  Seleccionar paso de 10 minutos  [Mayús]{.kbd}+[0]{.kbd}
  Seleccionar paso personalizado  [Mayús]{.kbd}+[-]{.kbd}

### Saltos por porcentaje

  Posición Atajo                  Posición         Atajo
  -------- ---------------------- ---------------- ------------------------------------
  10%      [Ctrl]{.kbd}+[1]{.kbd} 15%              [Ctrl]{.kbd}+[Mayús]{.kbd}+[1]{.kbd}
  20%      [Ctrl]{.kbd}+[2]{.kbd} 25%              [Ctrl]{.kbd}+[Mayús]{.kbd}+[2]{.kbd}
  30%      [Ctrl]{.kbd}+[3]{.kbd} 35%              [Ctrl]{.kbd}+[Mayús]{.kbd}+[3]{.kbd}
  40%      [Ctrl]{.kbd}+[4]{.kbd} 45%              [Ctrl]{.kbd}+[Mayús]{.kbd}+[4]{.kbd}
  50%      [Ctrl]{.kbd}+[5]{.kbd} 55%              [Ctrl]{.kbd}+[Mayús]{.kbd}+[5]{.kbd}
  60%      [Ctrl]{.kbd}+[6]{.kbd} 65%              [Ctrl]{.kbd}+[Mayús]{.kbd}+[6]{.kbd}
  70%      [Ctrl]{.kbd}+[7]{.kbd} 75%              [Ctrl]{.kbd}+[Mayús]{.kbd}+[7]{.kbd}
  80%      [Ctrl]{.kbd}+[8]{.kbd} 85%              [Ctrl]{.kbd}+[Mayús]{.kbd}+[8]{.kbd}
  90%      [Ctrl]{.kbd}+[9]{.kbd} 95%              [Ctrl]{.kbd}+[Mayús]{.kbd}+[9]{.kbd}
  100%     [Ctrl]{.kbd}+[0]{.kbd} 100% alternativo [Ctrl]{.kbd}+[Mayús]{.kbd}+[0]{.kbd}

### Volumen y estado hablado

  Acción                                                  Atajo
  ------------------------------------------------------- ----------------------------------------------
  Subir el volumen                                        [Flecha arriba]{.kbd}
  Bajar el volumen                                        [Flecha abajo]{.kbd}
  Establecer el volumen al máximo                         [Ctrl]{.kbd}+[Alt]{.kbd}+[Flecha arriba]{.kbd}
  Establecer el volumen al mínimo                         [Ctrl]{.kbd}+[Alt]{.kbd}+[Flecha abajo]{.kbd}
  Leer el volumen                                         [V]{.kbd}
  Leer el tiempo transcurrido                             [E]{.kbd}
  Leer el tiempo restante                                 [R]{.kbd}
  Leer la duración total                                  [T]{.kbd}
  Leer el porcentaje de posición                          [P]{.kbd}
  Leer la velocidad de reproducción                       [S]{.kbd}
  Cambiar entre el nivel de detalle Principiante/Avanzado [Ctrl]{.kbd}+[Mayús]{.kbd}+[V]{.kbd}

### Velocidad, tono, balance, bucles y filtros

  Acción                                           Atajo
  ------------------------------------------------ -------------------------------------
  Aumentar la velocidad de reproducción            [Ctrl]{.kbd}+[Flecha arriba]{.kbd}
  Disminuir la velocidad de reproducción           [Ctrl]{.kbd}+[Flecha abajo]{.kbd}
  Restablecer la velocidad de reproducción         [Alt]{.kbd}+[Y]{.kbd}
  Subir el tono                                    [Mayús]{.kbd}+[Flecha arriba]{.kbd}
  Bajar el tono                                    [Mayús]{.kbd}+[Flecha abajo]{.kbd}
  Restablecer el tono                              [Alt]{.kbd}+[Mayús]{.kbd}+[P]{.kbd}
  Leer el tono                                     [Mayús]{.kbd}+[P]{.kbd}
  Balance a la izquierda                           [Ctrl]{.kbd}+[Flecha izquierda]{.kbd}
  Balance a la derecha                             [Ctrl]{.kbd}+[Flecha derecha]{.kbd}
  Leer el balance                                  [B]{.kbd}
  Activar o desactivar la eliminación de silencios [Ctrl]{.kbd}+[M]{.kbd}
  Establecer el inicio del bucle A-B               [\[]{.kbd}
  Establecer el final del bucle A-B                [\]]{.kbd}
  Borrar el bucle A-B                              [Retroceso]{.kbd}

### Edición y archivos marcados

  Acción                                                 Atajo
  ------------------------------------------------------ ------------------------------------
  Cambiar el nombre del archivo actual                   [Mayús]{.kbd}+[F2]{.kbd}
  Eliminar el archivo actual                             [Mayús]{.kbd}+[Supr]{.kbd}
  Copiar el archivo actual al portapapeles               [Ctrl]{.kbd}+[Mayús]{.kbd}+[C]{.kbd}
  Pegar/abrir un archivo o carpeta desde el portapapeles [Ctrl]{.kbd}+[V]{.kbd}
  Marcar o desmarcar el archivo actual                   [Ctrl]{.kbd}+[K]{.kbd}
  Marcar o desmarcar todos los archivos                  [Ctrl]{.kbd}+[A]{.kbd}
  Borrar todas las marcas                                [Ctrl]{.kbd}+[Mayús]{.kbd}+[K]{.kbd}
  Leer la cantidad de archivos marcados                  [K]{.kbd}
  Copiar los archivos marcados a una carpeta             Ninguno
  Mover los archivos marcados a una carpeta              Ninguno
  Copiar los archivos marcados al portapapeles           Ninguno
  Eliminar los archivos marcados                         Ninguno

### Marcadores {#bookmarks}

  Acción                                Atajo
  ------------------------------------- -------------------------------------------------
  Agregar marcador                      [Mayús]{.kbd}+[M]{.kbd}
  Administrar marcadores                [Ctrl]{.kbd}+[Mayús]{.kbd}+[M]{.kbd}
  Saltar a las ranuras de marcador 1--9 [Alt]{.kbd}+[1]{.kbd} hasta [Alt]{.kbd}+[9]{.kbd}
  Saltar a la ranura de marcador 10     [Alt]{.kbd}+[0]{.kbd}

### YouTube, herramientas, grabación, actualizaciones y ayuda

  Acción                                                      Atajo
  ----------------------------------------------------------- ------------------------------------
  Abrir enlace de YouTube                                     [Ctrl]{.kbd}+[Mayús]{.kbd}+[Y]{.kbd}
  Buscar en YouTube                                           [Ctrl]{.kbd}+[Y]{.kbd}
  Videos favoritos                                            [Ctrl]{.kbd}+[Mayús]{.kbd}+[F]{.kbd}
  Abrir la cola de descargas                                  [Ctrl]{.kbd}+[Mayús]{.kbd}+[Q]{.kbd}
  Agregar el video actual de YouTube a la cola                [Ctrl]{.kbd}+[Q]{.kbd}
  Descargar directamente el video actual de YouTube           [Ctrl]{.kbd}+[D]{.kbd}
  Elegir las opciones de descarga del video actual de YouTube [Ctrl]{.kbd}+[Mayús]{.kbd}+[D]{.kbd}
  Mostrar la descripción del video actual                     [Alt]{.kbd}+[D]{.kbd}
  Copiar el enlace del video actual                           [Ctrl]{.kbd}+[Mayús]{.kbd}+[C]{.kbd}
  Abrir el video actual en el navegador                       Ninguno
  Abrir el canal del video actual en el navegador             Ninguno
  Explorar el canal del video actual en Luna                  Ninguno
  Abrir el convertidor de medios                              Ninguno
  Administrar fuentes de IPTV                                 [Ctrl]{.kbd}+[Mayús]{.kbd}+[I]{.kbd}
  Explorar canales de IPTV                                    Ninguno
  Establecer el temporizador de sueño                         [Ctrl]{.kbd}+[Mayús]{.kbd}+[S]{.kbd}
  Cancelar el temporizador de sueño                           Ninguno
  Leer el tiempo restante del temporizador de sueño           [Ctrl]{.kbd}+[T]{.kbd}
  Abrir la interfaz de grabación                              [Alt]{.kbd}+[R]{.kbd}
  Iniciar la grabación                                        [F9]{.kbd}
  Pausar o reanudar la grabación                              [F7]{.kbd}
  Detener la grabación                                        [F8]{.kbd}
  Abrir la carpeta de grabaciones                             Ninguno
  Abrir la guía del usuario                                   [F1]{.kbd}
  Acerca de Luna                                              Ninguno
  Abrir las notas de la versión                               Ninguno
  Buscar actualizaciones de la aplicación                     Ninguno
  Actualizar componentes de YouTube                           Ninguno

### Atajos globales predeterminados

Estos comandos funcionan en todo el sistema mientras Luna se está ejecutando, incluso si otro programa tiene el foco.

  Acción                         Atajo global
  ------------------------------ ------------------------------------------------
  Reproducir o pausar            [Win]{.kbd}+[Alt]{.kbd}+[Espacio]{.kbd}
  Retroceder un paso             [Win]{.kbd}+[Alt]{.kbd}+[Flecha izquierda]{.kbd}
  Avanzar un paso                [Win]{.kbd}+[Alt]{.kbd}+[Flecha derecha]{.kbd}
  Subir el volumen               [Win]{.kbd}+[Alt]{.kbd}+[Flecha arriba]{.kbd}
  Bajar el volumen               [Win]{.kbd}+[Alt]{.kbd}+[Flecha abajo]{.kbd}
  Archivo anterior               [Win]{.kbd}+[Alt]{.kbd}+[Re Pág]{.kbd}
  Archivo siguiente              [Win]{.kbd}+[Alt]{.kbd}+[Av Pág]{.kbd}
  Iniciar la grabación           [Win]{.kbd}+[Alt]{.kbd}+[F9]{.kbd}
  Pausar o reanudar la grabación [Win]{.kbd}+[Alt]{.kbd}+[F7]{.kbd}
  Detener la grabación           [Win]{.kbd}+[Alt]{.kbd}+[F8]{.kbd}

## 22. Formatos compatibles {#22-supported-formats}

Luna filtra la selección de archivos y carpetas usando las siguientes extensiones. La reproducción la gestiona mpv, por lo que otros formatos admitidos por ese motor también pueden reproducirse cuando se abren directamente.

**Audio:** AAC, AC-3, AIFF, ALAC, APE, AU, DTS, E-AC-3, FLAC, M4A, MKA, MP1, MP2, MP3, MPC, OGA, OGG, OGM, Opus, TAK, TrueHD (`.thd`), TTA, WAV, WMA y WavPack.

**Video:** 3G2, 3GP, AVI, FLV, IVF, M2TS, M4V, MJ2, MKV, MOV, MP4, MPEG, MPG, MXF, OGV, RMVB, TS, WebM, WMV y Y4M.

**Listas de reproducción:** M3U y M3U8, incluidas las transmisiones HLS.

## 23. Solución de problemas {#23-troubleshooting}

### Un archivo o una carpeta abre menos elementos de los esperados

Revise **Preferencias \> General \> Qué desea abrir con los archivos?** El valor predeterminado abre solo el archivo seleccionado. Los comandos de carpeta incluyen solo las extensiones reconocidas como medios. Las carpetas inaccesibles y las carpetas del sistema se omiten durante los análisis recursivos.

### Una transmisión en vivo no tiene duración ni porcentaje

Las transmisiones en vivo a menudo no publican una duración fija. Ir al tiempo, los saltos por porcentaje, el tiempo restante y el desplazamiento hasta el final requieren una duración conocida y pueden no estar disponibles.

### Un video de YouTube no se abre

Pruebe estos pasos en orden:

1.  Confirme que la dirección se abre en un navegador y que nombra un video o una lista de reproducción en lugar de un canal.
2.  Elija **Ayuda \> Actualizaciones \> Actualizar componentes de YouTube**.
3.  Si los componentes no se han instalado, elija **Preferencias \> YouTube \> Descargar componentes de YouTube**.
4.  Pruebe primero el canal de actualización Stable; use Nightly o Master solo si Stable no puede manejar un cambio reciente del servicio.
5.  Para un video con restricción de edad, solo para miembros o protegido por verificación, configure un archivo de cookies o las cookies de Firefox en las Preferencias de YouTube.

### Una descarga de YouTube termina de descargar pero sigue trabajando

Después de que yt-dlp recibe todos los datos multimedia, puede que todavía necesite combinar video y audio, convertir el resultado, incrustar información, procesar capítulos o mover el archivo terminado. El título y el texto de la ventana de progreso nombran este trabajo. No cancele solo porque el porcentaje haya llegado al 100%.

### Una descarga continúa después de que se cierra su ventana de progreso

Elegir **Minimizar a la cola** transfiere deliberadamente el progreso visible a **Herramientas \> Cola de descargas\...**. Cerrar la ventana de la cola también deja el trabajo activo en ejecución. Detenga el elemento desde la cola si ya no lo desea, o cierre Luna y confirme que las descargas sin terminar deben cancelarse.

### IPTV no tiene información de programación

La información de programación aparece solo cuando el proveedor la suministra o la fuente tiene una dirección de EPG XMLTV que coincide. Los identificadores de canal de la guía deben coincidir con los identificadores de la fuente. La reproducción puede seguir funcionando cuando no hay guía de programación disponible.

### La grabación no encuentra un programa

La captura de programas requiere Windows 10 versión 2004 o posterior. El programa también debe tener una sesión de audio de Windows activa. Inicie la reproducción en ese programa, vuelva a abrir el cuadro de diálogo de fuentes y elíjalo en la lista actualizada.

### La grabación comienza sin una de sus fuentes

Luna puede continuar cuando se abre al menos una fuente. Lea la advertencia que enumera las fuentes que fallaron y luego compruebe si sus dispositivos están conectados, habilitados y disponibles. Detenga la grabación antes de editar la lista de fuentes.

### Un atajo global no funciona

Abra **Preferencias \> Atajos globales** y aplique de nuevo la asignación. Windows no permite que dos aplicaciones registren la misma combinación global. Elija otra combinación si Luna informa que uno o más atajos no se pudieron registrar.

### La configuración no se guarda después de un error de inicio

Luna protege un `settings.json` no válido o ilegible en lugar de sobrescribirlo. Abra Preferencias e importe una copia de seguridad de configuración válida o elija **Copia de seguridad y restauración \> Restablecer configuración**. Si necesita el original para un diagnóstico, cópielo primero desde la carpeta de configuración del usuario.

### Los marcadores o los favoritos informan datos no válidos

Un archivo de almacenamiento no válido se conserva en lugar de reemplazarse silenciosamente. Abra la carpeta de configuración del usuario, haga una copia del archivo JSON afectado y restaure una copia de seguridad válida conocida. Los marcadores se pueden restaurar a través de Preferencias. Los favoritos requieren actualmente restaurar el propio archivo `favorites.json` mientras Luna está cerrado.

### La guía del usuario no se abre

La guía instalada debería estar en `docs\<código-de-idioma>\user-guide.html` junto al ejecutable. Vuelva a extraer el archivo portátil completo o repare/reinstale la aplicación si falta la carpeta `docs`. Luna recurre del idioma regional a su idioma base y luego al inglés.

## 24. Editor y licencias {#24-publisher-and-licensing}

Luna Player es publicado por **Diamond Star**.

Copyright © 2026 Diamond Star.

El código fuente original de Luna Player está licenciado bajo la Licencia Apache, versión 2.0. El enlace de mpv traducido en `src/Mpv.cs` está licenciado bajo la Licencia Pública General Reducida de GNU, versión 2.1 o posterior.

Luna Player también usa componentes de terceros con licencias independientes, incluidos mpv, FFmpeg, wxWidgets, Prism, NAudio y PyYt. Cada componente se mantiene bajo su propia licencia. Consulte `NOTICE.txt`, instalado junto con Luna Player, para ver las declaraciones de derechos de autor, las versiones de los componentes, los nombres de las licencias y las ubicaciones del código fuente. Los textos de licencia pertinentes se instalan en la carpeta `licenses`.

El resumen de licencias de esta guía se ofrece por conveniencia. `LICENSE.txt`, `NOTICE.txt` y los archivos de la carpeta `licenses` contienen los términos y las atribuciones oficiales. En el repositorio del código fuente, los archivos correspondientes de nivel superior se llaman `LICENSE` y `NOTICE`.

## 25. Contacto y soporte {#25-contact-and-support}

Luna Player es desarrollado y publicado por Diamond Star. Puede usar los siguientes canales:

- **Correo electrónico:** <ramymaherali55@gmail.com>
- **Telegram:** [Contactar a Diamond Star en Telegram](https://t.me/diamondStar35)
- **Informes de errores:** [Problemas de Luna Player en GitHub](https://github.com/diamondStar35/luna_player/issues)
- **Código fuente y versiones:** [Luna Player en GitHub](https://github.com/diamondStar35/luna_player)

Para un informe de errores, incluya la versión de Luna que se muestra en **Ayuda \> Acerca de**, su versión de Windows, lo que esperaba, lo que ocurrió y los pasos más breves que reproducen el problema. Incluya el texto exacto de cualquier mensaje de error. Elimine los nombres de archivo privados, las direcciones web, los datos de cuentas y otra información personal de los registros o las capturas de pantalla antes de compartirlos públicamente.
