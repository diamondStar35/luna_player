::: {role="main"}
# Guía del usuario de Luna Player {#luna-player-user-guide}

Luna Player es un reproductor de audio y vídeo accesible mediante
teclado para Windows. Combina reproducción de medios locales,
transmisiones en red, reproducción y descarga de YouTube, grabación de
audio, gestión de archivos, marcadores, anuncios por voz y atajos
personalizables locales y del sistema.

Esta guía describe la versión actual de Luna Player. Los nombres de los
menús y los atajos siguientes corresponden a la interfaz en inglés y a
la configuración predeterminada de atajos.

::: toc
-   [1. Antes de empezar](#1-before-you-begin)
-   [2. Interfaz y accesibilidad](#2-interface-and-accessibility)
-   [3. Inicio rápido](#3-quick-start)
-   [4. Abrir contenido multimedia](#4-opening-media)
-   [5. Controles de reproducción](#5-playback-controls)
-   [6. Trabajo con varios archivos](#6-working-with-several-files)
-   [7. Bucles A-B](#7-a-b-loops)
-   [8. Eliminación de silencios y procesamiento de
    audio](#8-silence-removal-and-audio-processing)
-   [9. Gestión de archivos locales](#9-local-file-management)
-   [10. Archivos marcados y operaciones en
    lote](#10-marked-files-and-batch-operations)
-   [11. Marcadores](#11-bookmarks)
-   [12. YouTube](#12-youtube)
-   [13. Grabación de audio](#13-recording-audio)
-   [14. Referencia de preferencias](#14-preferences-reference)
-   [15. Actualizaciones de la aplicación](#15-application-updates)
-   [16. Ayuda e información de la
    versión](#16-help-and-release-information)
-   [17. Configuración y datos del usuario](#17-settings-and-user-data)
-   [18. Referencia completa de atajos
    predeterminados](#18-complete-default-shortcut-reference)
-   [19. Formatos compatibles](#19-supported-formats)
-   [20. Solución de problemas](#20-troubleshooting)
-   [21. Editor y licencias](#21-publisher-and-licensing)
-   [22. Contacto y soporte](#22-contact-and-support)
:::

## 1. Antes de empezar {#1-before-you-begin}

### Requisitos del sistema {#system-requirements}

Luna Player requiere Windows 10 de 64 bits versión 1809 o posterior. La
captura del sonido de un programa específico requiere Windows 10 versión
2004 o posterior. Otras funciones de reproducción y grabación permanecen
disponibles en versiones anteriores compatibles.

### Ediciones instalable y portable {#installed-and-portable-editions}

Las versiones de Luna Player están disponibles como un instalador y como
un archivo ZIP portable.

-   Para instalar Luna Player, ejecute el instalador de la versión y
    siga los pasos. El instalador puede crear accesos directos en el
    menú Inicio y en el escritorio, y puede registrar tipos de medios
    compatibles.
-   Para usar la edición portable, extraiga todo el archivo ZIP en una
    carpeta y ejecute `LunaPlayer.exe`. Mantenga juntos todos los
    archivos y carpetas extraídos.

Ambas ediciones utilizan la misma carpeta de configuración de usuario.
El actualizador integrado reconoce qué edición se está ejecutando y
descarga el instalador o archivo portable correspondiente.

### Iniciar Luna Player {#starting-luna-player}

Puede iniciar Luna Player directamente, abrir un archivo compatible
desde Windows o pasarle archivos y carpetas mediante la línea de
comandos. Luna se ejecuta como una sola instancia: abrir más contenido
multimedia mientras ya se está ejecutando envía ese contenido a la
ventana existente.

Si la opción **Remember last file position** está habilitada, iniciar
Luna sin otro archivo restaura el último archivo local y su última
posición de reproducción si dicho archivo sigue disponible.

## 2. Interfaz y accesibilidad {#2-interface-and-accessibility}

### Ventana principal {#main-window}

La ventana principal contiene una barra de menú y cinco botones:

-   **Previous**: pasa al elemento anterior.
-   **Rewind**: retrocede la cantidad de salto seleccionada.
-   **Play** o **Pause**: cambia el estado de reproducción.
-   **Forward**: avanza la cantidad de salto seleccionada.
-   **Next**: pasa al elemento siguiente.

Todas las órdenes también están disponibles desde un menú o atajo. Los
elementos que no se pueden aplicar al contenido actual están
desactivados. Por ejemplo, las propiedades del archivo están disponibles
para un archivo local pero no para una transmisión web, y el menú
**Video options** se habilita solo para un vídeo de YouTube activo.

### Navegación por teclado {#keyboard-navigation}

Utilice la navegación estándar de Windows en toda la aplicación:

-   Presione [Alt]{.kbd} para acceder a la barra de menú, luego use las
    teclas de dirección y [Enter]{.kbd}.
-   Presione [Tab]{.kbd} y [Shift]{.kbd}+[Tab]{.kbd} para desplazarse
    entre los controles de los cuadros de diálogo.
-   Use las teclas de dirección para desplazarse por listas y opciones.
-   Presione [Enter]{.kbd} para activar el elemento seleccionado cuando
    el diálogo lo permita.
-   Presione [Escape]{.kbd} para cerrar la mayoría de los diálogos sin
    aplicar cambios.
-   Presione [F1]{.kbd} en la ventana principal para abrir esta guía.

En un control de Preferences, [F1]{.kbd} tiene un propósito diferente:
Luna verbaliza ayuda contextual para ese control. Si el árbol de
categorías tiene el foco, explica cómo desplazarse entre las páginas de
configuración.

### Anuncios por voz {#speech-announcements}

Luna informa del estado de reproducción, tiempo, volumen, navegación,
operaciones de archivos y otros cambios a través de un lector de
pantalla compatible. Hay dos niveles de detalle disponibles:

-   **Beginner**: utiliza mensajes completos y explicativos.
-   **Advanced**: utiliza confirmaciones más breves.

Presione [Ctrl]{.kbd}+[Shift]{.kbd}+[V]{.kbd} para cambiar el nivel
inmediatamente, o elíjalo en la página de preferencias **General**.
Habilite **Speak file name when navigating (Previous/Next)** si desea
que Luna anuncie cada elemento al que se llega con Anterior o Siguiente.

### Controles multimedia de Windows {#windows-media-controls}

Luna se integra con la superposición de medios de Windows y teclas
multimedia de hardware compatibles. La superposición puede mostrar el
título actual y la línea de tiempo. Los botones de reproducción, pausa,
anterior, siguiente, retroceso y avance rápido invocan las órdenes
correspondientes de Luna.

## 3. Inicio rápido {#3-quick-start}

Para reproducir un archivo local:

1.  Presione [Ctrl]{.kbd}+[O]{.kbd}.
2.  Elija un archivo multimedia o una lista de reproducción M3U/M3U8.
3.  Presione [Espacio]{.kbd} para pausar o reanudar.
4.  Presione [Izquierda]{.kbd} o [Derecha]{.kbd} para saltar.
5.  Presione [Arriba]{.kbd} o [Abajo]{.kbd} para cambiar el volumen.

Para reproducir todos los archivos compatibles de una carpeta, presione
[Ctrl]{.kbd}+[Shift]{.kbd}+[O]{.kbd} y elija la carpeta. Use [Tab]{.kbd}
y [Shift]{.kbd}+[Tab]{.kbd} para moverse entre los elementos cargados.

Para reproducir un vídeo de YouTube, presione
[Ctrl]{.kbd}+[Shift]{.kbd}+[Y]{.kbd} e introduzca su dirección. Para
buscar en su lugar, presione [Ctrl]{.kbd}+[Y]{.kbd}.

## 4. Abrir contenido multimedia {#4-opening-media}

### Abrir un archivo {#opening-a-file}

Elija **File \> Open File\...** o presione [Ctrl]{.kbd}+[O]{.kbd}. El
diálogo acepta archivos de audio, vídeo, M3U y M3U8 compatibles.

La opción **What would you like to open with files?** controla lo que
sucede cuando se abre un archivo multimedia desde Windows o desde el
diálogo de archivos:

-   **Open the file only**: carga únicamente el archivo seleccionado.
-   **Open the file and the main folder files**: carga los archivos
    compatibles de la misma carpeta y selecciona el archivo solicitado.
-   **Open the file with the main and subfolder files**: escanea la
    carpeta y todas sus subcarpetas, carga cada archivo multimedia
    compatible encontrado y selecciona el archivo solicitado.

Un escaneo recursivo muestra un cuadro de diálogo de progreso que se
puede cancelar e informa cuántos archivos ha encontrado. Los archivos se
ordenan según el orden natural de Windows.

Cuando se envían varios archivos a Luna juntos y está seleccionado
**Open the file only**, Luna carga todos los archivos seleccionados
explícitamente en lugar de descartar todos menos uno.

### Abrir una carpeta {#opening-a-folder}

Elija **File \> Open Folder\...** o presione
[Ctrl]{.kbd}+[Shift]{.kbd}+[O]{.kbd}.

Con cualquiera de los dos primeros modos de apertura de archivos, Luna
carga los medios compatibles directamente desde la carpeta elegida. Con
el modo de subcarpetas, escanea de forma recursiva. Las carpetas
inaccesibles y del sistema se omiten durante el escaneo recursivo.

### Abrir una transmisión de red {#opening-a-network-stream}

Elija **File \> Open Link\...** o presione [Ctrl]{.kbd}+[L]{.kbd}.
Introduzca una dirección `http` o `https`.

Use esta orden para transmisiones directas de medios y listas de
reproducción M3U o M3U8 remotas. Use **Open YouTube Link\...** para
direcciones de vídeos y listas de reproducción de YouTube.

### Abrir listas de reproducción {#opening-playlists}

Luna admite listas de reproducción M3U y M3U8 locales y remotas. Las
entradas relativas en una lista local se resuelven desde la carpeta de
la lista; las entradas relativas en una lista de red se resuelven desde
su dirección URL.

Un manifiesto HLS M3U8 se trata como un único flujo de datos y se pasa
al motor de reproducción; no se expande en una lista de segmentos
individuales. Otras listas de reproducción se convierten en una lista de
archivos abiertos que contiene sus rutas locales y enlaces web
reproducibles.

### Abrir medios desde el portapapeles {#opening-media-from-the-clipboard}

Copie un archivo o carpeta en el Explorador de archivos, vuelva a Luna y
presione [Ctrl]{.kbd}+[V]{.kbd}. Luna abrirá la primera ruta válida
existente en el portapapeles según el modo de apertura configurado.

### Asociaciones de archivos {#file-associations}

El instalador puede registrar Luna para tipos de archivo compatibles.
También puede usar **Preferences \> General \> Register file
extensions** o **Unregister file extensions**.

El registro añade Luna a la lista de aplicaciones de Windows para
formatos compatibles. Es posible que Windows aún requiera que seleccione
Luna en **Configuración \> Aplicaciones \> Aplicaciones
predeterminadas** antes de que se convierta en el reproductor
predeterminado.

## 5. Controles de reproducción {#5-playback-controls}

### Reproducir y pausar {#play-and-pause}

Presione [Espacio]{.kbd} o [Enter]{.kbd}, elija **Player \>
Play/Pause**, o utilice el botón principal Reproducir/Pausar.

Si se puede volver a abrir un elemento finalizado o descargado,
Reproducir/Pausar lo recarga. En el nivel Beginner, Luna anuncia
\"Reproducir\" o \"Pausar\"; el nivel Advanced evita estos mensajes de
estado más largos.

### Salto de tiempo (desplazamiento) {#seeking}

Presione [Izquierda]{.kbd} o [Derecha]{.kbd} para avanzar o retroceder
un paso. Se pueden realizar movimientos más grandes sin cambiar el paso
seleccionado:

-   [Shift]{.kbd}+[Izquierda]{.kbd} o [Shift]{.kbd}+[Derecha]{.kbd}: se
    mueve dos pasos.
-   [Ctrl]{.kbd}+[Shift]{.kbd}+[Izquierda]{.kbd} o
    [Ctrl]{.kbd}+[Shift]{.kbd}+[Derecha]{.kbd}: se mueve cuatro pasos.
-   [Inicio]{.kbd}: va al principio.
-   [Fin]{.kbd}: va al final.

La cantidad de salto predeterminada es de 5 segundos. Elija otro valor
desde **Player \> Seek amount**, o presione [Shift]{.kbd} más un número:

  Atajo                     Cantidad de salto
  ------------------------- ----------------------------------------------
  [Shift]{.kbd}+[1]{.kbd}   1 segundo
  [Shift]{.kbd}+[2]{.kbd}   5 segundos
  [Shift]{.kbd}+[3]{.kbd}   10 segundos
  [Shift]{.kbd}+[4]{.kbd}   20 segundos
  [Shift]{.kbd}+[5]{.kbd}   30 segundos
  [Shift]{.kbd}+[6]{.kbd}   1 minuto
  [Shift]{.kbd}+[7]{.kbd}   2 minutos
  [Shift]{.kbd}+[8]{.kbd}   3 minutos
  [Shift]{.kbd}+[9]{.kbd}   5 minutos
  [Shift]{.kbd}+[0]{.kbd}   10 minutos
  [Shift]{.kbd}+[-]{.kbd}   Valor personalizado de Preferencias de Audio

El valor seleccionado se guarda inmediatamente. Cambiar **Custom seek
value (seconds)** no selecciona el valor personalizado automáticamente;
selecciónelo en el menú o con [Shift]{.kbd}+[-]{.kbd} cuando desee
usarlo.

### Ir a un punto temporal {#going-to-a-time}

Elija **Player \> Go to time\...** o presione
[Ctrl]{.kbd}+[Shift]{.kbd}+[G]{.kbd}. Introduzca las horas, minutos y
segundos relevantes para la duración actual. Luna evita que la posición
seleccionada supere el final del archivo.

### Saltar por porcentaje {#jumping-by-percentage}

Elija **Player \> Jump to Percentage** o use los atajos de porcentaje
predeterminados:

-   [Ctrl]{.kbd}+[1]{.kbd} a [Ctrl]{.kbd}+[9]{.kbd}: saltan del 10% al
    90%.
-   [Ctrl]{.kbd}+[Shift]{.kbd}+[1]{.kbd} a
    [Ctrl]{.kbd}+[Shift]{.kbd}+[9]{.kbd}: saltan del 15% al 95%.
-   [Ctrl]{.kbd}+[0]{.kbd} o [Ctrl]{.kbd}+[Shift]{.kbd}+[0]{.kbd}: salta
    al 100%.

Los saltos por porcentaje requieren una duración conocida y, por lo
tanto, no están disponibles en algunas emisiones en directo.

### Obtener información de reproducción {#getting-playback-information}

Luna puede verbalizar la información de reproducción sin mover el foco:

-   [E]{.kbd}: anuncia el tiempo transcurrido.
-   [R]{.kbd}: anuncia el tiempo restante.
-   [T]{.kbd}: anuncia la duración total.
-   [P]{.kbd}: anuncia el porcentaje actual.
-   [V]{.kbd}: anuncia el volumen actual.
-   [S]{.kbd}: anuncia la velocidad actual.
-   [Shift]{.kbd}+[P]{.kbd}: anuncia el ajuste de tono (pitch).
-   [B]{.kbd}: anuncia el valor de balance estéreo (panorámica).

### Nombre de archivo, ruta y título del medio {#file-name-path-and-media-title}

Presione [F]{.kbd} repetidamente para obtener información
progresivamente más detallada sobre el elemento actual:

1.  La primera pulsación anuncia el nombre del archivo, el nombre de la
    transmisión o el título del vídeo de YouTube.
2.  Una segunda pulsación rápida anuncia la ruta local completa o la
    dirección de la transmisión.
3.  Una tercera pulsación rápida copia esa ruta o dirección al
    portapapeles.

Si hace una pausa superior a un breve instante, la secuencia comenzará
de nuevo con el nombre en pantalla.

Presione [I]{.kbd} para que se anuncie el título guardado en los
metadatos del medio. Esto es independiente del nombre del archivo; un
archivo local puede no tener ningún título integrado o tener uno
diferente al nombre del archivo.

### Volumen {#volume}

Presione [Arriba]{.kbd} o [Abajo]{.kbd} para cambiar el volumen según el
paso configurado. El paso predeterminado es de 5 puntos porcentuales.

-   [Ctrl]{.kbd}+[Alt]{.kbd}+[Arriba]{.kbd}: ajusta el volumen al máximo
    de Luna.
-   [Ctrl]{.kbd}+[Alt]{.kbd}+[Abajo]{.kbd}: ajusta el volumen al 5%.
-   [V]{.kbd}: anuncia el volumen actual.

Luna permite una amplificación por encima del 100%, hasta un máximo de
2000%. Una amplificación elevada puede aumentar el ruido de fondo y la
distorsión del audio original. La normalización dinámica y el limitador
pueden ayudar a contener los picos, pero comience con un nivel moderado
para proteger su audición y sus equipos.

### Velocidad de reproducción {#playback-speed}

Presione [Ctrl]{.kbd}+[Arriba]{.kbd} o [Ctrl]{.kbd}+[Abajo]{.kbd} para
cambiar la velocidad de reproducción. Presione [Alt]{.kbd}+[Y]{.kbd}
para volver a 1x. La velocidad está limitada entre 0.5x y 4x, y el paso
se puede configurar en la página de Preferencias de Audio.

### Tono (Pitch) {#pitch}

Los cambios de tono no alteran la velocidad de reproducción.

-   [Shift]{.kbd}+[Arriba]{.kbd}: sube el tono.
-   [Shift]{.kbd}+[Abajo]{.kbd}: baja el tono.
-   [Alt]{.kbd}+[Shift]{.kbd}+[P]{.kbd}: restaura el tono original.
-   [Shift]{.kbd}+[P]{.kbd}: anuncia el ajuste actual en semitonos.

El tono está limitado a 12 semitonos por encima o por debajo del
original. El paso se puede configurar entre 0.001 y 12 semitonos.

### Balance estéreo (Panorámica) {#stereo-pan}

-   [Ctrl]{.kbd}+[Izquierda]{.kbd}: desplaza el sonido hacia la
    izquierda.
-   [Ctrl]{.kbd}+[Derecha]{.kbd}: desplaza el sonido hacia la derecha.
-   [B]{.kbd}: anuncia el porcentaje de balance actual.

Cero es centrado, los valores negativos representan la izquierda y los
positivos la derecha. El paso de balance es configurable entre 1 y 100
puntos porcentuales.

### Elegir un dispositivo de salida {#choosing-an-output-device}

Elija **Player \> Sound Cards\...** o presione
[Ctrl]{.kbd}+[Shift]{.kbd}+[A]{.kbd}. Seleccione un dispositivo de
salida de audio de Windows y presione Aceptar. El dispositivo elegido se
guarda de inmediato.

## 6. Trabajo con varios archivos {#6-working-with-several-files}

### Anterior, siguiente, primero y último {#previous-next-first-and-last}

-   [Shift]{.kbd}+[Tab]{.kbd} o [Re Pág]{.kbd}: reproduce el elemento
    anterior.
-   [Tab]{.kbd} o [Av Pág]{.kbd}: reproduce el elemento siguiente.
-   [Ctrl]{.kbd}+[Inicio]{.kbd}: reproduce el primer elemento.
-   [Ctrl]{.kbd}+[Fin]{.kbd}: reproduce el último elemento.

Si la opción **Wrap to top for multiple files** está activada, al pulsar
Siguiente en el último elemento se vuelve al primero, y al pulsar
Anterior en el primero se va al último. El avance automático sigue esta
misma regla.

### Ir a un número de archivo {#going-to-a-file-number}

Elija **Player \> Go to file\...** o presione [Ctrl]{.kbd}+[G]{.kbd}.
Introduzca un número del 1 al total de elementos cargados.

### Diálogo de Archivos Abiertos {#opened-files-dialog}

Elija **File \> Opened Files\...** o presione [F2]{.kbd}. El elemento
actual estará seleccionado en la lista.

-   Elija un elemento y active **Jump to selected**, o presione
    [Enter]{.kbd} sobre él para reproducirlo.
-   Active **Playlist info** para calcular el número de archivos, el
    tamaño total, la duración total, el tiempo transcurrido en la lista
    y el tiempo restante. El análisis se puede cancelar y el resultado
    se abre en una ventana de texto de solo lectura.

La lista está diseñada para mantener su fluidez incluso con un número
muy elevado de archivos cargados.

### Reproducción aleatoria {#shuffle}

Elija **Player \> Shuffle** o presione [Ctrl]{.kbd}+[Z]{.kbd}. Activar
la reproducción aleatoria crea un orden aleatorio manteniendo
seleccionado el elemento actual.

Mientras está activa, las funciones Anterior, Siguiente, Primer archivo,
Último archivo, Ir a archivo y el cuadro de diálogo de Archivos abiertos
operarán según el orden aleatorio. Desactivar la función restaura el
orden normal de la lista manteniendo el elemento actual.

### Repetición y comportamiento al final del archivo {#repeat-and-end-of-file-behavior}

Elija **Player \> Repeat File** o presione [Ctrl]{.kbd}+[R]{.kbd} para
repetir el archivo actual. Esta opción de reproducción tiene prioridad
mientras está activada.

La página de Preferencias de Audio también controla la acción normal al
finalizar un archivo:

-   **Advance to the next file**: inicia el siguiente elemento.
-   **Loop the file**: inicia de nuevo el mismo elemento.
-   **Do nothing**: detiene la reproducción al final.

### Recordar posiciones {#remembering-positions}

Dos ajustes sirven para propósitos diferentes:

-   **Remember last file position**: restaura el archivo local activo y
    el punto de reproducción la próxima vez que se inicie Luna.
-   **Save current position for each file**: recuerda una posición
    independiente para cada elemento local y vuelve a ella cuando se
    vuelve a visitar.

Si ambas están desactivadas, los archivos comenzarán normalmente desde
el principio.

## 7. Bucles A-B {#7-a-b-loops}

Un bucle A-B repite una sección del elemento actual.

1.  Desplácese hasta el inicio deseado y presione [\[]{.kbd}. Esto
    establece el punto A.
2.  Desplácese hasta una posición posterior y presione [\]]{.kbd}. Esto
    establece el punto B e inicia la repetición del bucle entre A y B.
3.  Presione [Retroceso]{.kbd} para borrar el bucle y continuar desde el
    punto B.

Mientras un bucle está activo, los saltos normales se limitan al rango
seleccionado. [Inicio]{.kbd} va al punto A y [Fin]{.kbd} va al punto B.
El punto B debe ser posterior al punto A.

La selección se aplica solo al elemento actual y se borra al pasar a
otro elemento.

## 8. Eliminación de silencios y procesamiento de audio {#8-silence-removal-and-audio-processing}

### Activar o desactivar la eliminación de silencios {#turning-silence-removal-on-or-off}

Elija **Player \> Enable silence removal filter** o presione
[Ctrl]{.kbd}+[M]{.kbd}. Luna guarda el estado inmediatamente.

La eliminación de silencios acorta las secciones silenciosas mientras se
reproduce el contenido. No modifica el archivo original. Los resultados
dependen del archivo original: un umbral muy agresivo puede tratar el
habla o la música suaves como silencio.

### Ajustes básicos de silencio {#basic-silence-settings}

Abra **Preferences \> Silence removal**.

-   **Minimum silence duration (seconds)**: controla cuánto debe durar
    una sección silenciosa para poder ser recortada. Aumentar este valor
    preserva más pausas cortas.
-   **Silence threshold**: es el nivel, en decibelios, por debajo del
    cual el audio se considera silencio. Un valor menos negativo, como
    -20, trata audios más altos como silencio; un valor más negativo,
    como -50, limita la eliminación al audio más silencioso.

Los valores predeterminados son 0.5 segundos y -30 dB.

### Ajustes avanzados de silencio {#advanced-silence-settings}

Habilite **Show advanced settings** para configurar el filtro subyacente
con mayor precisión:

-   **Leading silent parts to trim**: 0 conserva el principio; 1 elimina
    el silencio inicial hasta encontrar sonido sostenido. Valores más
    altos continúan a través de más secciones de sonido.
-   **Sound required before leading trim stops**: cuánto tiempo debe
    permanecer el audio por encima del umbral antes de que Luna lo
    conserve.
-   **Silent parts to trim after audio starts**: -1 acorta cada pausa
    posterior que cumpla los requisitos, 0 conserva el silencio
    posterior y un valor positivo limita la cantidad de pausas
    recortadas.
-   **Minimum inner silence length**: la duración mínima de pausa válida
    una vez comenzado el sonido.
-   **Pause to keep after trimmed silence**: conserva una parte de cada
    pausa para que las palabras no se solapen.
-   **Detection window size**: la ventana de tiempo utilizada para medir
    el nivel de volumen. Ventanas más grandes son más estables; ventanas
    más pequeñas reaccionan más rápido.
-   **Detection mode**: Peak reacciona a la muestra más alta y sonidos
    breves; RMS utiliza la energía promedio para una detección más
    suave.

Haga pequeños cambios y pruébelos con material representativo. El filtro
afecta a la reproducción inmediatamente tras aceptar las Preferencias.

### Normalización, limitador y mono {#normalization-limiting-and-mono}

La página de Preferencias de Audio contiene dos filtros adicionales:

-   **Enable dynamic normalize and limiter**: nivela los cambios de
    volumen y frena los picos para reducir la distorsión (clipping).
-   **Play audio as Mono**: combina los canales izquierdo y derecho en
    una sola salida mono.

Ninguna de las dos opciones modifica el archivo de origen.

### Ecualizador {#equalizer}

El ecualizador sube y baja rangos de frecuencia individuales. Elija
**Player \> Equalizer** y seleccione un elemento de la lista. El cambio
surte efecto al instante y se recuerda entre sesiones.

La lista comienza con **Off**, seguida de seis ajustes predeterminados
para problemas comunes de escucha:

-   **Bass Boost** y **Bass Reduce**: aumentan o reducen los graves. El
    efecto es más fuerte por debajo de los 100 Hz, sigue siendo
    sustancial a 170 Hz y desaparece hacia los 600 Hz, cubriendo el
    rango de un bombo y un bajo sin engrosar las voces.
-   **Treble Boost**: eleva todo desde aproximadamente 4 kHz hacia
    arriba.
-   **Headphones**: añade cuerpo en los graves, suaviza el rango de 3.5
    kHz donde los auriculares pueden sonar estidentes y abre los agudos.
-   **Voice**: elimina el retumbo y el sonido acartonado y realza la
    presencia, que es lo que hace inteligibles las consonantes. Es
    adecuado para audiolibros, podcasts y conferencias en lugar de
    música.
-   **Loudness**: eleva ambos extremos y deja intacto el medio, para
    escuchar a volumen bajo cuando el oído es menos sensible a los
    extremos.

A continuación vienen los ajustes por géneros: Classical, Club, Dance,
Live, Pop, Reggae, Rock, Ska, Soft, Soft Rock y Techno. **Custom** está
en último lugar; no cambia nada hasta que se edita y es el punto de
partida para sus propios ajustes.

Un ajuste que aumenta un rango hace que el sonido sea más fuerte en
lugar de solo diferente, y no se atenúa ningún rango para hacer que el
cambio parezca mayor. Por lo tanto, el material ya masterizado al límite
puede saturar. La opción **Enable dynamic normalize and limiter** en la
página de Preferencias de Audio evita esto y está activada por defecto;
si la desactiva, reduzca el volumen en su lugar.

**Off** aplana todas las bandas en lugar de quitar el ecualizador. El
último ajuste utilizado se recuerda mientras el ecualizador está
apagado, por lo que seleccionarlo de nuevo vuelve a aplicarlo.

### Editar un ajuste predeterminado {#editing-a-preset}

Elija **Player \> Equalizer \> Edit current preset** para abrir el
ajuste en uso. Todos los ajustes se pueden editar, incluidos los que
incluye Luna.

El diálogo contiene un grupo por banda: un filtro tipo \"shelf\" bajo,
quince bandas de 25 Hz a 16 kHz y un filtro tipo \"shelf\" alto. Cada
grupo contiene tres campos.

-   **Frequency**: es el centro de la banda o el punto donde gira el
    filtro shelf.
-   **Gain**: indica cuánto se sube o baja la banda, en decibelios, de
    -20 a 20.
-   **Q**: es el ancho de la banda, de 0.1 a 10. Los valores más grandes
    son más estrechos.

Mientras el ajuste editado sea el que se está usando, cada cambio se
aplica a medida que escribe, para que pueda juzgarlo de oído antes de
guardarlo. **Save** conserva el resultado; **Cancel** restaura el ajuste
como estaba.

**Base preset** rellena cada banda desde otro ajuste. Úselo para empezar
desde algo cercano a lo que busca. **Reset to defaults** devuelve las
bandas al estado de fábrica del ajuste elegido.

Editar un ajuste predeterminado de Luna guarda solo los valores
modificados, por lo que las bandas no alteradas seguirán las futuras
mejoras que reciba Luna. Restablecer descarta dichos cambios. El nombre
de un ajuste integrado de fábrica no se puede cambiar.

### Gestionar ajustes predeterminados {#managing-presets}

Elija **Player \> Equalizer \> Manage presets** para ver la lista
completa.

-   **New**: crea un ajuste, comenzando desde el ajuste base que elija
    en el editor.
-   **Edit**: abre el ajuste seleccionado.
-   **Delete**: lo elimina tras confirmarlo.
-   **Close**: sale de la lista.

Las opciones de editar y eliminar se aplican a sus propios ajustes y
están ocultas cuando hay seleccionado un ajuste original de Luna; use
**Edit current preset** para esos casos. Para copiar un ajuste, cree uno
nuevo y elija el original como base.

Los ajustes que cree aparecerán en el menú Equalizer junto con el resto
y se guardarán en `equalizer.json`, junto al archivo de configuración.

## 9. Gestión de archivos locales {#9-local-file-management}

Las órdenes de renombrar, eliminar, abrir carpeta contenedora y
propiedades de Windows funcionan únicamente con archivos locales. No
están disponibles para transmisiones de red ni medios de YouTube.

### Mostrar el archivo en Windows {#showing-the-file-in-windows}

-   Presione [Ctrl]{.kbd}+[F]{.kbd} o elija **File \> Open Containing
    Folder** para abrir el Explorador de archivos con el elemento actual
    seleccionado.
-   Presione [Alt]{.kbd}+[Enter]{.kbd} o elija **File \> File
    properties\...** para abrir las propiedades de Windows del archivo.

### Renombrar un archivo {#renaming-a-file}

Elija **Edit \> Rename\...** o presione [Shift]{.kbd}+[F2]{.kbd}.
Escriba el nuevo nombre de archivo. Si omite la extensión, Luna
mantendrá la extensión actual. Luna rechazará un nombre vacío o uno que
ya exista en esa carpeta.

### Eliminar un archivo {#deleting-a-file}

Elija **Edit \> Delete** o presione [Shift]{.kbd}+[Delete]{.kbd}.
Confirme la acción para eliminar permanentemente el archivo del disco y
quitarlo de la lista cargada. Luna no mueve el archivo a la papelera de
reciclaje.

Eliminar no es lo mismo que **Close File**. Cerrar elimina el elemento
solo de la lista de Luna; eliminar borra el archivo del disco.

### Copiar un archivo {#copying-a-file}

Presione [Ctrl]{.kbd}+[Shift]{.kbd}+[C]{.kbd} para copiar el archivo
actual al portapapeles de Windows. Péguelo en el Explorador de archivos
o en otro programa que admita la opción de pegar archivos.

### Cerrar archivos {#closing-files}

-   Presione [Ctrl]{.kbd}+[W]{.kbd} para quitar el elemento actual de
    Luna sin eliminarlo del disco.
-   Presione [Ctrl]{.kbd}+[Shift]{.kbd}+[W]{.kbd} para quitar todos los
    elementos cargados.

## 10. Archivos marcados y operaciones en lote {#10-marked-files-and-batch-operations}

Las marcas le permiten seleccionar varios archivos locales mientras
navega para operar con ellos conjuntamente.

-   Presione [Ctrl]{.kbd}+[K]{.kbd} para marcar o desmarcar el archivo
    actual.
-   Presione [Ctrl]{.kbd}+[A]{.kbd} para marcar todos los archivos o
    desmarcarlos todos si ya lo estaban.
-   Presione [Ctrl]{.kbd}+[Shift]{.kbd}+[K]{.kbd} para limpiar todas las
    marcas.
-   Presione [K]{.kbd} para escuchar el número de archivos marcados.

Cuando haya al menos un archivo marcado, se habilitará el menú **Actions
for marked files**:

-   **Copy to folder\...**: copia los archivos marcados a una carpeta
    seleccionada.
-   **Move to folder\...**: los mueve a otra carpeta y elimina de Luna
    las entradas movidas con éxito.
-   **Copy to clipboard**: coloca los archivos en el portapapeles de
    Windows.
-   **Delete**: solicita confirmación y elimina permanentemente los
    archivos del disco sin pasar por la papelera de reciclaje,
    quitándolos de la lista de Luna.

Copiar y mover muestran un cuadro de progreso que se puede cancelar.
Luna informará si la operación ha sido un éxito total, parcial, si se
canceló o si falló. Si algún archivo falla, los procesados correctamente
mantendrán su nuevo estado.

## 11. Marcadores {#11-bookmarks}

Los marcadores guardan posiciones de reproducción con nombre para
archivos locales. Se guardan por cada archivo y no están disponibles
para transmisiones o vídeos de YouTube.

### Añadir un marcador {#adding-a-bookmark}

1.  Reproduzca o avance hasta la posición deseada.
2.  Elija **Bookmarks \> Add a new bookmark** o presione
    [Shift]{.kbd}+[M]{.kbd}.
3.  Acepte el nombre generado basado en el tiempo o escriba un nombre
    descriptivo.

Los marcadores se ordenan según su punto de reproducción. Los diez
primeros en ese orden ocuparán las ranuras asignadas a los atajos
directos.

### Ir a un marcador numerado {#jumping-to-a-numbered-bookmark}

Presione [Alt]{.kbd}+[1]{.kbd} a [Alt]{.kbd}+[9]{.kbd} para las ranuras
1 a 9. Presione [Alt]{.kbd}+[0]{.kbd} para la ranura 10. Luna anunciará
si la ranura solicitada está vacía.

### Gestionar marcadores {#managing-bookmarks}

Elija **Bookmarks \> Manage bookmarks** o presione
[Ctrl]{.kbd}+[Shift]{.kbd}+[M]{.kbd}. El diálogo lista los nombres de
los marcadores y sus posiciones para el archivo actual.

-   **Jump**: traslada la reproducción al marcador seleccionado.
-   **Edit**: cambia su nombre.
-   **Delete**: lo elimina tras confirmarlo.
-   Activar un marcador de la lista salta directamente a él.

Utilice la página de Copia de seguridad y restauración de Preferencias
para exportar o importar toda la colección de marcadores.

## 12. YouTube {#12-youtube}

Las funciones de YouTube requieren conexión a Internet. La
disponibilidad puede variar si YouTube realiza cambios en su servicio.
Luna incluye un resolvedor integrado y opcionalmente puede utilizar
yt-dlp.

### Abrir un enlace de YouTube {#opening-a-youtube-link}

Elija **File \> Open YouTube Link\...** o presione
[Ctrl]{.kbd}+[Shift]{.kbd}+[Y]{.kbd}. Introduzca la dirección de un
vídeo o lista de reproducción. Las direcciones de canales no son
aceptadas por esta orden.

Algunas direcciones de YouTube identifican tanto un vídeo como una lista
de reproducción. Luna puede preguntar si desea reproducir el vídeo o
abrir la lista, o bien seguir el comportamiento predeterminado
seleccionado en las Preferencias de YouTube.

### Buscar en YouTube {#searching-youtube}

Elija **File \> Search YouTube\...** o presione [Ctrl]{.kbd}+[Y]{.kbd}.
Introduzca el texto de búsqueda y desplácese por la lista de resultados.

-   Presione [Enter]{.kbd} en el resultado seleccionado o active
    **Play** para reproducirlo.
-   Active **Download** para guardarlo.
-   Presione [Ctrl]{.kbd}+[C]{.kbd} para copiar el enlace del resultado
    seleccionado.
-   Presione [Ctrl]{.kbd}+[B]{.kbd} para abrirlo en el navegador
    predeterminado.
-   Presione [Ctrl]{.kbd}+[N]{.kbd} para navegar a los resultados del
    canal del creador.
-   Abra el menú contextual para acceder a estas mismas opciones.

Cuando la selección llega al final de la página actual, Luna solicita
automáticamente más resultados hasta que YouTube no devuelva más.

Las ventanas de resultados de listas de reproducción y canales funcionan
de la misma manera. Siguiente avanza a través de la sesión de resultados
activa de YouTube, buscando el próximo vídeo cuando sea necesario. Al
presionar [Escape]{.kbd} en la ventana principal después de iniciar la
reproducción, se puede volver a la lista de resultados relacionada si la
sesión todavía está disponible.

### Reproducción de solo audio y calidad {#audio-only-playback-and-quality}

Abra **Preferences \> YouTube**.

-   **Play videos as audio only**: solicita únicamente el sonido,
    reduciendo el consumo de datos y acelerando el inicio.
-   **Video quality**: permite elegir entre Baja (Low), Media (Medium) o
    la Mejor (Best) cuando el vídeo está habilitado.

Las mismas opciones se utilizan al descargar un vídeo. El modo de solo
audio guarda el sonido sin imagen; de lo contrario, Luna descargará la
calidad de vídeo seleccionada.

### Opciones de vídeo {#video-options}

Mientras un vídeo de YouTube está activo, el menú **Video options**
ofrece:

-   **Download\...** o [Ctrl]{.kbd}+[D]{.kbd}: permite elegir una
    carpeta y guardar el vídeo actual mostrando el progreso y la opción
    de cancelación.
-   **Video description\...** o [Alt]{.kbd}+[D]{.kbd}: obtiene el título
    y la descripción del canal y los muestra en una ventana de texto de
    solo lectura.
-   **Copy video link**: copia la dirección permanente del vídeo en
    lugar de la dirección temporal de la transmisión.

Una descarga repetida no sobrescribe un archivo existente con el mismo
nombre; Luna elegirá un nombre numerado disponible.

### Vídeos y transmisiones favoritos {#favorite-videos-and-streams}

Los favoritos guardan enlaces, mientras que los marcadores guardan
posiciones dentro de archivos locales. Abra los favoritos mediante
**File \> Favorite videos\...** o [Ctrl]{.kbd}+[Shift]{.kbd}+[F]{.kbd}.

El diálogo de Favoritos permite abrir, añadir, editar o eliminar
entradas. Cada entrada consta de un nombre, enlace y tipo:

-   **Video**: requiere un enlace de vídeo de YouTube.
-   **Playlist**: requiere un enlace de lista de reproducción de
    YouTube.
-   **Combined link**: requiere una dirección que contenga
    identificadores de vídeo y de lista, abriendo el vídeo.
-   **Generic stream**: acepta cualquier dirección de transmisión `http`
    o `https`.

Luna valida el formato de la dirección al guardar una entrada. Solo
comprueba si el contenido remoto sigue disponible cuando se intenta
abrir.

### Resolvedor integrado y yt-dlp {#built-in-resolver-and-yt-dlp}

Por defecto, Luna utiliza su propio resolvedor de YouTube. Si no se
puede abrir un vídeo, habilite **Use yt-dlp to resolve streams** en las
Preferencias de YouTube. Luna ofrecerá descargar el componente requerido
si este no se encuentra.

yt-dlp es siempre necesario para la opción de resolución por yt-dlp,
pero el resolvedor integrado y otras funciones de YouTube pueden operar
sin él. La página de componentes ofrece tres canales de actualización:

-   **Stable**: cambia con menos frecuencia y es la recomendada para un
    uso normal.
-   **Nightly**: sigue las compilaciones nocturnas de yt-dlp.
-   **Master**: sigue el código fuente más reciente y puede ser menos
    estable.

Utilice **Download YouTube components** en Preferencias para instalar
yt-dlp y los ejecutables de Deno faltantes. Utilice **Help \> Updates \>
Update YouTube components** para actualizar una versión instalada de
yt-dlp. Habilite **Check for yt-dlp updates on startup** para una
comprobación en segundo plano que solo le avisará cuando haya una
actualización disponible.

## 13. Grabación de audio {#13-recording-audio}

Luna puede grabar micrófonos y otras entradas, todo lo que se reproduce
a través de un dispositivo de salida, o el audio de un programa en
particular. Se pueden mezclar múltiples fuentes en una sola grabación
con niveles de volumen independientes.

### Grabación rápida con el micrófono predeterminado {#fast-recording-with-the-default-microphone}

Si no se ha creado ninguna fuente en la sesión actual, presione
[F9]{.kbd} para grabar desde el dispositivo de entrada predeterminado de
Windows utilizando la configuración de Grabación guardada. Presione
[F7]{.kbd} para pausar o reanudar, y [F8]{.kbd} para detener la
grabación.

Un tono ascendente confirma el inicio de la grabación. Un tono
descendente confirma su finalización. El inicio y fin no añaden mensajes
hablados sobre estos tonos.

### Abrir la interfaz de grabación {#opening-the-recording-interface}

Elija **Recording \> Open the recording interface\...** o presione
[Alt]{.kbd}+[R]{.kbd}. La ventana dispone de una lista de fuentes,
controles de fuente, ajustes de salida y controles de Inicio/Pausa.

Cerrar esta ventana no detiene una grabación activa. La grabación
pertenece a la aplicación y continuará hasta que utilice Detener,
presione [F8]{.kbd} o salga de Luna.

### Añadir fuentes {#adding-sources}

Active **Add** y elija:

-   **Input device\...**: para un micrófono, entrada de línea u otro
    dispositivo de captura de Windows.
-   **System output\...**: para todo lo que se reproduzca a través de un
    altavoz o auriculares seleccionados.
-   **Program\...**: para el audio de un programa específico. Requiere
    Windows 10 versión 2004 o posterior.

Asigne un nombre descriptivo a la fuente, elija el dispositivo o
programa y establezca su volumen. Una fuente de dispositivo puede seguir
al dispositivo predeterminado de Windows actual en lugar de a uno con
nombre fijo.

Para una fuente de programa, la opción **Capture everything except this
application** invierte la selección y captura el audio de las demás
aplicaciones. Solo los programas con una sesión de audio activa en
Windows aparecerán en la lista; inicie la reproducción de audio en el
programa correspondiente si no aparece.

Utilice **Edit\...** para cambiar la fuente seleccionada y
**Remove\...** para borrarla de la sesión. El deslizador de volumen de
la fuente cambia su nivel en la mezcla final.

Las fuentes son solo para la sesión actual. Se conservan al cerrar y
reabrir la ventana de grabación durante la misma sesión de Luna, pero no
se restauran tras salir de la aplicación, ya que los dispositivos e
identificadores de procesos pueden cambiar.

### Formato de grabación y destino {#recording-format-and-destination}

Elija el formato, la frecuencia de muestreo, el número de canales, la
calidad (si procede) y la carpeta de destino:

-   **WAV**: no tiene compresión y suele generar el archivo de mayor
    tamaño.
-   **FLAC**: es sin pérdidas y generalmente ocupa menos que WAV.
-   **MP3** y **AAC**: tienen pérdida y usan la tasa de bits
    seleccionada para equilibrar el tamaño y la calidad.
-   **Mono**: suele ser suficiente para un micrófono o voz hablada.
-   **Stereo**: conserva los canales izquierdo y derecho por separado
    para música y salida del sistema.

Las frecuencias de muestreo, los canales y las tasas de bits disponibles
se obtienen de los codificadores instalados en Windows. Por lo tanto,
las opciones varían según el formato seleccionado y el sistema. Luna
asigna a cada grabación un nombre con marca de tiempo para evitar
sobrescribir archivos anteriores.

Los cambios realizados dentro de la interfaz de grabación se aplican a
su sesión. Para cambiar los valores predeterminados utilizados en
futuros inicios o por los atajos directos, utilice **Preferences \>
Recording**.

### Iniciar, pausar y detener {#starting-pausing-and-stopping}

La ventana de grabación requiere al menos una fuente configurada antes
de que la opción **Start** esté disponible. Una vez iniciada la
grabación, los controles de fuente y salida se bloquean para que el
formato de grabación y la mezcla permanezcan constantes.

-   El botón **Start** se convierte en **Stop** durante la grabación.
-   El botón **Pause** se convierte en **Resume** durante la pausa.
-   [F9]{.kbd}, [F7]{.kbd} y [F8]{.kbd} funcionan desde la ventana
    principal de Luna.
-   Los atajos globales correspondientes funcionan incluso cuando otra
    aplicación tiene el foco.

Si solo se pueden abrir algunas fuentes, Luna se iniciará con las
fuentes disponibles e informará de las que hayan fallado. Si no se puede
abrir ninguna, la grabación no se iniciará.

Elija **Recording \> Open recordings folder** para abrir la carpeta de
destino activa. La carpeta predeterminada es
`Documentos\Luna Player\Recordings`.

## 14. Referencia de preferencias {#14-preferences-reference}

Abra Preferencias desde **File \> Preferences\...** o presione
[Ctrl]{.kbd}+[P]{.kbd}. Seleccione una página en el árbol de categorías.
Presione Aceptar para validar y aplicar todas las páginas, o Cancelar
para descartar los cambios no aceptados.

Presione [F1]{.kbd} mientras un ajuste tiene el foco para escuchar una
explicación detallada de esa opción.

### General

  Ajuste                                    Propósito
  ----------------------------------------- -----------------------------------------------------------------------------------------------------------------------------------------------------------
  Language                                  Utiliza el idioma de visualización de Windows o una traducción incluida. Reinicie Luna después de cambiarlo.
  Remember last file position               Restaura el último archivo local activo y su posición en el siguiente inicio.
  Speak file name when navigating           Anuncia el elemento alcanzado mediante Anterior o Siguiente.
  Check for app updates on startup          Busca actualizaciones en segundo plano al iniciar y solo avisa si hay una versión nueva lista.
  Save settings on close                    Guarda los cambios de la sesión (como volumen y velocidad) al salir de Luna. Los cambios aceptados explícitamente en Preferencias se guardan al instante.
  Verbosity                                 Permite elegir entre anuncios completos (Beginner) o anuncios breves (Advanced).
  What would you like to open with files?   Controla si al abrir un archivo se cargan solo él mismo, su carpeta o su carpeta y subcarpetas.
  Register/Unregister file extensions       Añade o elimina el registro de formatos compatibles de Luna para el usuario actual de Windows.

### Copia de seguridad y restauración {#backup-and-restore}

  Orden                       Propósito
  --------------------------- ----------------------------------------------------------------------------------------
  Export settings             Guarda las preferencias aplicadas en un archivo JSON sin mover la copia de trabajo.
  Import settings             Carga un archivo JSON de configuración válido y reemplaza los valores de Preferencias.
  Export bookmarks            Guarda todos los marcadores en un archivo JSON.
  Import bookmarks            Reemplaza la colección de marcadores actual por una válida exportada previamente.
  Reset settings              Restaura cada preferencia y atajo a su valor predeterminado tras confirmarlo.
  Open user settings folder   Abre el directorio de configuración de Luna en el Explorador de archivos.

Importar reemplaza la colección correspondiente; no combina entradas.
Conserve los archivos exportados en un lugar separado de la carpeta de
configuración de Luna si se usan como copias de seguridad.

### Audio

  Ajuste                                 Propósito
  -------------------------------------- ------------------------------------------------------------------------------------------------
  Custom seek value                      Número de segundos utilizado para el salto personalizado; admite decimales positivos como 2.5.
  Speed step                             Cantidad que se suma o resta en cada orden de velocidad.
  Pitch step                             Cambio de tono por pulsación, de 0.001 a 12 semitonos.
  Volume step                            Cambio de volumen por pulsación, de 1 a 20 puntos porcentuales.
  Pan step                               Movimiento del balance por pulsación, de 1 a 100 puntos porcentuales.
  What happens after a file ends?        Avanza, repite en bucle o se detiene al llegar al final.
  Wrap to top for multiple files         Conecta el final y el principio de una lista de varios elementos.
  Save current position for each file    Mantiene una posición de reanudación independiente para cada archivo local.
  Enable dynamic normalize and limiter   Nivela el volumen y controla los picos.
  Play audio as Mono                     Mezcla los canales izquierdo y derecho en mono.

### Eliminación de silencios {#silence-removal}

Consulte [Eliminación de silencios y procesamiento de
audio](#8-silence-removal-and-audio-processing) para los controles
básicos y avanzados.

### YouTube {#youtube}

  Ajuste                                Propósito
  ------------------------------------- ---------------------------------------------------------------------------------
  Play videos as audio only             Solicita únicamente sonido sin imagen.
  Video quality                         Selecciona Baja, Media o la Mejor para la reproducción de vídeos y descargas.
  Number of search results              Establece el objetivo inicial de 5 a 100; se cargarán más al llegar al final.
  Video+playlist link behavior          Pregunta cada vez, reproduce siempre el vídeo o abre siempre la lista.
  Use yt-dlp to resolve streams         Utiliza el componente descargado yt-dlp en lugar del resolvedor propio de Luna.
  yt-dlp update channel                 Elige entre Estable (Stable), Nocturno (Nightly) o Principal (Master).
  Check for yt-dlp updates on startup   Realiza una comprobación silenciosa del componente tras iniciar.
  Download YouTube components           Instala o actualiza yt-dlp y herramientas de soporte.

### Grabación {#recording}

Esta página establece los valores predeterminados utilizados por los
atajos directos de grabación y configura la interfaz de grabación al
comienzo de cada sesión de Luna. Contiene el formato de audio, la
frecuencia de muestreo, los canales, la calidad de audio para formatos
comprimidos y la carpeta de grabaciones.

### Atajos de teclado {#keyboard-shortcuts}

Esta página enumera cada acción que se puede ejecutar mientras la
ventana principal de Luna tiene el foco. Cada acción puede tener un
atajo primario; algunas acciones también disponen de un espacio
secundario predefinido.

1.  Seleccione una acción.
2.  Active **Edit Primary Shortcut** o, si está disponible, **Edit
    Secondary Shortcut**.
3.  Presione la combinación deseada. Presione [Escape]{.kbd} para
    cancelar la captura.
4.  Presione Aceptar en Preferencias para aplicar los cambios.

Luna rechazará una combinación que ya esté asignada a otra acción en el
mismo conjunto de atajos. Los atajos locales pueden usar teclas
imprimibles, teclas de navegación, teclas de función F1 a F24 y los
modificadores Ctrl, Shift o Alt. Utilice **Reset to Defaults** para
restaurar la página completa de atajos locales tras su confirmación.

### Atajos globales {#global-shortcuts}

Los atajos globales funcionan mientras otra aplicación tiene el foco.
Están limitados intencionadamente a órdenes de reproducción, navegación,
volumen y grabación que resultan útiles fuera de Luna.

Seleccione una acción, active **Edit Shortcut** y presione la
combinación deseada. Los atajos globales también pueden utilizar la
tecla Windows. Es posible que Windows rechace un atajo reservado por el
sistema operativo u otra aplicación; Luna informará de los atajos que no
haya podido registrar.

Los atajos globales son independientes de los atajos locales. Cambiar
uno no modifica el otro.

## 15. Actualizaciones de la aplicación {#15-application-updates}

### Comprobación manual {#checking-manually}

Elija **Help \> Updates \> Check for app updates**. Aparecerá un cuadro
de diálogo de progreso que se puede cancelar mientras Luna comprueba la
información sobre la nueva versión.

Si hay una versión nueva disponible, Luna mostrará la versión instalada,
la disponible y una lista desplazable de cambios. Elija **Update** para
descargarla o **Later** para mantener la versión actual sin cambios.

Luna verifica que el instalador real o el archivo portable existan antes
de ofrecer la actualización. Si la información de lanzamiento se publicó
antes de terminar de subir el paquete, una comprobación manual le pedirá
que lo intente de nuevo más tarde en lugar de ofrecer una descarga
fallida.

### Comprobación al inicio {#startup-checks}

Habilite **Check for app updates on startup** para una comprobación en
segundo plano. No mostrará ventanas de progreso ni mensajes en caso de
no haber novedades; solo aparecerá un aviso cuando exista un paquete
compatible más reciente.

### Instalar una actualización descargada {#installing-a-downloaded-update}

La ventana de descarga muestra el tamaño total, el descargado, el
porcentaje y un botón Cancelar. Tras una descarga correcta, Luna
iniciará su asistente de actualización y se cerrará:

-   En una copia instalada, iniciará el instalador de la actualización.
    El progreso del proceso permanecerá visible y Luna se abrirá al
    finalizar.
-   En una copia portable, reemplazará los archivos de la aplicación
    extraídos y abrirá el reproductor actualizado.

No inicie otro proceso de Luna mientras se esté aplicando la
actualización.

## 16. Ayuda e información de la versión {#16-help-and-release-information}

-   **Help \> User guide** o [F1]{.kbd}: abre la guía instalada que
    coincida con el idioma de la interfaz de Luna. Si no hay instalada
    ninguna guía en el idioma regional o base, Luna abrirá la guía en
    inglés.
-   **Help \> About**: muestra una descripción, versión, información de
    derechos de autor y un enlace al sitio web del proyecto.
-   **Help \> Release notes**: abre la página de notas de la versión en
    GitHub en su navegador predeterminado.

La guía del usuario está instalada como HTML local y no requiere
conexión a Internet. Las notas de la versión y el sitio web del proyecto
sí requieren conexión.

## 17. Configuración y datos del usuario {#17-settings-and-user-data}

Luna almacena los datos de usuario en `%APPDATA%\Luna Player`:

  Archivo            Contenido
  ------------------ -------------------------------------------------------
  `settings.json`    Preferencias y atajos personalizados
  `bookmarks.json`   Marcadores guardados con nombre para archivos locales
  `positions.json`   Posiciones de reproducción por archivo
  `favorites.json`   Enlaces favoritos de YouTube y transmisiones

Utilice **Preferences \> Backup and restore \> Open user settings
folder** para abrir la carpeta exacta sin tener que escribir la ruta.

No edite estos archivos mientras Luna esté en ejecución. Utilice las
funciones de exportación e importación de la configuración y marcadores
siempre que sea posible.

Si `settings.json` no se puede leer o contiene valores no válidos, Luna
se iniciará con los valores predeterminados, mostrará el motivo y
protegerá el archivo existente para que no sea sobrescrito. Importe una
copia de seguridad válida o use **Reset settings** para reemplazarlo
deliberadamente. Los archivos de marcadores y favoritos no válidos
también se notificarán en lugar de reemplazarse en silencio.

## 18. Referencia completa de atajos predeterminados {#18-complete-default-shortcut-reference}

Estos son los valores predeterminados de fábrica. Sus atajos actuales
pueden variar según los haya personalizado. La lista oficial para su
instalación se encuentra en **Preferences \> Keyboard Shortcuts** y
**Global Shortcuts**.

### Archivos, información y Preferencias {#files-information-and-preferences}

  Acción                                             Atajo primario                         Atajo secundario
  -------------------------------------------------- -------------------------------------- ------------------
  Abrir archivos                                     [Ctrl]{.kbd}+[O]{.kbd}                 ---
  Abrir un enlace de transmisión en red              [Ctrl]{.kbd}+[L]{.kbd}                 ---
  Abrir todos los archivos de una carpeta            [Ctrl]{.kbd}+[Shift]{.kbd}+[O]{.kbd}   ---
  Mostrar archivo actual en Explorador de archivos   [Ctrl]{.kbd}+[F]{.kbd}                 ---
  Propiedades de archivo de Windows                  [Alt]{.kbd}+[Enter]{.kbd}              ---
  Diálogo de Archivos Abiertos                       [F2]{.kbd}                             ---
  Cerrar archivo actual                              [Ctrl]{.kbd}+[W]{.kbd}                 ---
  Cerrar todos los archivos                          [Ctrl]{.kbd}+[Shift]{.kbd}+[W]{.kbd}   ---
  Preferencias                                       [Ctrl]{.kbd}+[P]{.kbd}                 ---
  Anunciar información del archivo actual            [F]{.kbd}                              ---
  Anunciar título del medio actual                   [I]{.kbd}                              ---
  Salir de Luna                                      Ninguno                                ---

### Navegación por lista y modos {#playlist-navigation-and-modes}

  Acción                                      Atajo primario                Atajo secundario
  ------------------------------------------- ----------------------------- ------------------
  Archivo anterior                            [Shift]{.kbd}+[Tab]{.kbd}     [Re Pág]{.kbd}
  Archivo siguiente                           [Tab]{.kbd}                   [Av Pág]{.kbd}
  Primer archivo                              [Ctrl]{.kbd}+[Inicio]{.kbd}   ---
  Ir al número de archivo                     [Ctrl]{.kbd}+[G]{.kbd}        ---
  Último archivo                              [Ctrl]{.kbd}+[Fin]{.kbd}      ---
  Activar/Desactivar reproducción aleatoria   [Ctrl]{.kbd}+[Z]{.kbd}        ---
  Activar/Desactivar repetición de archivo    [Ctrl]{.kbd}+[R]{.kbd}        ---

### Reproducción y saltos de tiempo {#playback-and-seeking}

  Acción                          Atajo primario                                 Atajo secundario
  ------------------------------- ---------------------------------------------- ------------------
  Reproducir o pausar             [Espacio]{.kbd}                                [Enter]{.kbd}
  Retroceder un paso              [Izquierda]{.kbd}                              ---
  Avanzar un paso                 [Derecha]{.kbd}                                ---
  Retroceder dos pasos            [Shift]{.kbd}+[Izquierda]{.kbd}                ---
  Avanzar dos pasos               [Shift]{.kbd}+[Derecha]{.kbd}                  ---
  Retroceder cuatro pasos         [Ctrl]{.kbd}+[Shift]{.kbd}+[Izquierda]{.kbd}   ---
  Avanzar cuatro pasos            [Ctrl]{.kbd}+[Shift]{.kbd}+[Derecha]{.kbd}     ---
  Principio                       [Inicio]{.kbd}                                 ---
  Final                           [Fin]{.kbd}                                    ---
  Ir a un punto temporal          [Ctrl]{.kbd}+[Shift]{.kbd}+[G]{.kbd}           ---
  Seleccionar tarjeta de sonido   [Ctrl]{.kbd}+[Shift]{.kbd}+[A]{.kbd}           ---

### Selección del paso de salto {#seek-amount-selection}

  Acción                            Atajo
  --------------------------------- -------------------------
  Seleccionar paso de 1 segundo     [Shift]{.kbd}+[1]{.kbd}
  Seleccionar paso de 5 segundos    [Shift]{.kbd}+[2]{.kbd}
  Seleccionar paso de 10 segundos   [Shift]{.kbd}+[3]{.kbd}
  Seleccionar paso de 20 segundos   [Shift]{.kbd}+[4]{.kbd}
  Seleccionar paso de 30 segundos   [Shift]{.kbd}+[5]{.kbd}
  Seleccionar paso de 1 minuto      [Shift]{.kbd}+[6]{.kbd}
  Seleccionar paso de 2 minutos     [Shift]{.kbd}+[7]{.kbd}
  Seleccionar paso de 3 minutos     [Shift]{.kbd}+[8]{.kbd}
  Seleccionar paso de 5 minutos     [Shift]{.kbd}+[9]{.kbd}
  Seleccionar paso de 10 minutos    [Shift]{.kbd}+[0]{.kbd}
  Seleccionar paso personalizado    [Shift]{.kbd}+[-]{.kbd}

### Saltos por porcentaje {#percentage-jumps}

  Posición   Atajo                    Posición           Atajo
  ---------- ------------------------ ------------------ --------------------------------------
  10%        [Ctrl]{.kbd}+[1]{.kbd}   15%                [Ctrl]{.kbd}+[Shift]{.kbd}+[1]{.kbd}
  20%        [Ctrl]{.kbd}+[2]{.kbd}   25%                [Ctrl]{.kbd}+[Shift]{.kbd}+[2]{.kbd}
  30%        [Ctrl]{.kbd}+[3]{.kbd}   35%                [Ctrl]{.kbd}+[Shift]{.kbd}+[3]{.kbd}
  40%        [Ctrl]{.kbd}+[4]{.kbd}   45%                [Ctrl]{.kbd}+[Shift]{.kbd}+[4]{.kbd}
  50%        [Ctrl]{.kbd}+[5]{.kbd}   55%                [Ctrl]{.kbd}+[Shift]{.kbd}+[5]{.kbd}
  60%        [Ctrl]{.kbd}+[6]{.kbd}   65%                [Ctrl]{.kbd}+[Shift]{.kbd}+[6]{.kbd}
  70%        [Ctrl]{.kbd}+[7]{.kbd}   75%                [Ctrl]{.kbd}+[Shift]{.kbd}+[7]{.kbd}
  80%        [Ctrl]{.kbd}+[8]{.kbd}   85%                [Ctrl]{.kbd}+[Shift]{.kbd}+[8]{.kbd}
  90%        [Ctrl]{.kbd}+[9]{.kbd}   95%                [Ctrl]{.kbd}+[Shift]{.kbd}+[9]{.kbd}
  100%       [Ctrl]{.kbd}+[0]{.kbd}   100% alternativa   [Ctrl]{.kbd}+[Shift]{.kbd}+[0]{.kbd}

### Volumen y estado verbalizado {#volume-and-spoken-status}

  Acción                                       Atajo
  -------------------------------------------- -----------------------------------------
  Aumentar volumen                             [Arriba]{.kbd}
  Disminuir volumen                            [Abajo]{.kbd}
  Ajustar volumen al máximo                    [Ctrl]{.kbd}+[Alt]{.kbd}+[Arriba]{.kbd}
  Ajustar volumen al mínimo                    [Ctrl]{.kbd}+[Alt]{.kbd}+[Abajo]{.kbd}
  Anunciar volumen                             [V]{.kbd}
  Anunciar tiempo transcurrido                 [E]{.kbd}
  Anunciar tiempo restante                     [R]{.kbd}
  Anunciar duración total                      [T]{.kbd}
  Anunciar porcentaje de posición              [P]{.kbd}
  Anunciar velocidad de reproducción           [S]{.kbd}
  Cambiar nivel de detalle Beginner/Advanced   [Ctrl]{.kbd}+[Shift]{.kbd}+[V]{.kbd}

### Velocidad, tono, balance, bucles y filtros {#speed-pitch-pan-loops-and-filters}

  Acción                                        Atajo
  --------------------------------------------- -------------------------------------
  Aumentar velocidad de reproducción            [Ctrl]{.kbd}+[Arriba]{.kbd}
  Disminuir velocidad de reproducción           [Ctrl]{.kbd}+[Abajo]{.kbd}
  Restablecer velocidad de reproducción         [Alt]{.kbd}+[Y]{.kbd}
  Aumentar tono                                 [Shift]{.kbd}+[Arriba]{.kbd}
  Disminuir tono                                [Shift]{.kbd}+[Abajo]{.kbd}
  Restablecer tono                              [Alt]{.kbd}+[Shift]{.kbd}+[P]{.kbd}
  Anunciar tono                                 [Shift]{.kbd}+[P]{.kbd}
  Balance hacia la izquierda                    [Ctrl]{.kbd}+[Izquierda]{.kbd}
  Balance hacia la derecha                      [Ctrl]{.kbd}+[Derecha]{.kbd}
  Anunciar balance                              [B]{.kbd}
  Activar/Desactivar eliminación de silencios   [Ctrl]{.kbd}+[M]{.kbd}
  Establecer inicio de bucle A-B                [\[]{.kbd}
  Establecer fin de bucle A-B                   [\]]{.kbd}
  Borrar bucle A-B                              [Retroceso]{.kbd}

### Edición y archivos marcados {#editing-and-marked-files}

  Acción                                                Atajo
  ----------------------------------------------------- --------------------------------------
  Renombrar archivo actual                              [Shift]{.kbd}+[F2]{.kbd}
  Eliminar archivo actual                               [Shift]{.kbd}+[Supr]{.kbd}
  Copiar archivo actual al portapapeles                 [Ctrl]{.kbd}+[Shift]{.kbd}+[C]{.kbd}
  Pegar/abrir archivo o carpeta desde el portapapeles   [Ctrl]{.kbd}+[V]{.kbd}
  Marcar o desmarcar archivo actual                     [Ctrl]{.kbd}+[K]{.kbd}
  Marcar o desmarcar todos los archivos                 [Ctrl]{.kbd}+[A]{.kbd}
  Limpiar todas las marcas                              [Ctrl]{.kbd}+[Shift]{.kbd}+[K]{.kbd}
  Anunciar recuento de archivos marcados                [K]{.kbd}
  Copiar archivos marcados a una carpeta                Ninguno
  Mover archivos marcados a una carpeta                 Ninguno
  Copiar archivos marcados al portapapeles              Ninguno
  Eliminar archivos marcados                            Ninguno

### Marcadores {#bookmarks}

  Acción                      Atajo
  --------------------------- -----------------------------------------------
  Añadir marcador             [Shift]{.kbd}+[M]{.kbd}
  Gestionar marcadores        [Ctrl]{.kbd}+[Shift]{.kbd}+[M]{.kbd}
  Ir a los marcadores 1 a 9   [Alt]{.kbd}+[1]{.kbd} a [Alt]{.kbd}+[9]{.kbd}
  Ir al marcador 10           [Alt]{.kbd}+[0]{.kbd}

### YouTube, grabación, actualizaciones y ayuda {#youtube-recording-updates-and-help}

  Acción                                       Atajo
  -------------------------------------------- --------------------------------------
  Abrir enlace de YouTube                      [Ctrl]{.kbd}+[Shift]{.kbd}+[Y]{.kbd}
  Buscar en YouTube                            [Ctrl]{.kbd}+[Y]{.kbd}
  Vídeos favoritos                             [Ctrl]{.kbd}+[Shift]{.kbd}+[F]{.kbd}
  Descargar vídeo de YouTube actual            [Ctrl]{.kbd}+[D]{.kbd}
  Mostrar descripción del vídeo actual         [Alt]{.kbd}+[D]{.kbd}
  Copiar enlace del vídeo actual               Ninguno
  Abrir interfaz de grabación                  [Alt]{.kbd}+[R]{.kbd}
  Iniciar grabación                            [F9]{.kbd}
  Pausar o reanudar grabación                  [F7]{.kbd}
  Detener grabación                            [F8]{.kbd}
  Abrir carpeta de grabaciones                 Ninguno
  Abrir guía del usuario                       [F1]{.kbd}
  Acerca de Luna                               Ninguno
  Abrir notas de la versión                    Ninguno
  Comprobar actualizaciones de la aplicación   Ninguno
  Actualizar componentes de YouTube            Ninguno

### Atajos globales predeterminados {#default-global-shortcuts}

Estas órdenes funcionan en todo el sistema mientras Luna está en
ejecución, incluso si otro programa tiene el foco.

  Acción                        Atajo global
  ----------------------------- -------------------------------------------
  Reproducir o pausar           [Win]{.kbd}+[Alt]{.kbd}+[Espacio]{.kbd}
  Retroceder un paso            [Win]{.kbd}+[Alt]{.kbd}+[Izquierda]{.kbd}
  Avanzar un paso               [Win]{.kbd}+[Alt]{.kbd}+[Derecha]{.kbd}
  Aumentar volumen              [Win]{.kbd}+[Alt]{.kbd}+[Arriba]{.kbd}
  Disminuir volumen             [Win]{.kbd}+[Alt]{.kbd}+[Abajo]{.kbd}
  Archivo anterior              [Win]{.kbd}+[Alt]{.kbd}+[Re Pág]{.kbd}
  Archivo siguiente             [Win]{.kbd}+[Alt]{.kbd}+[Av Pág]{.kbd}
  Iniciar grabación             [Win]{.kbd}+[Alt]{.kbd}+[F9]{.kbd}
  Pausar o reanudar grabación   [Win]{.kbd}+[Alt]{.kbd}+[F7]{.kbd}
  Detener grabación             [Win]{.kbd}+[Alt]{.kbd}+[F8]{.kbd}

## 19. Formatos compatibles {#19-supported-formats}

Luna filtra la selección de archivos y carpetas utilizando las
siguientes extensiones. La reproducción es gestionada por mpv, por lo
que otros formatos admitidos por dicho motor también podrían
reproducirse al abrirse directamente.

**Audio:** AAC, AC-3, AIFF, ALAC, APE, AU, DTS, E-AC-3, FLAC, M4A, MKA,
MP1, MP2, MP3, MPC, OGA, OGG, OGM, Opus, TAK, TrueHD (`.thd`), TTA, WAV,
WMA y WavPack.

**Vídeo:** 3G2, 3GP, AVI, FLV, IVF, M2TS, M4V, MJ2, MKV, MOV, MP4, MPEG,
MPG, MXF, OGV, RMVB, TS, WebM, WMV y Y4M.

**Listas de reproducción:** M3U y M3U8, incluyendo emisiones HLS.

## 20. Solución de problemas {#20-troubleshooting}

### Un archivo o carpeta abre menos elementos de los esperados {#a-file-or-folder-opens-fewer-items-than-expected}

Compruebe **Preferences \> General \> What would you like to open with
files?**. Por defecto, solo se abre el archivo seleccionado. Las órdenes
para carpetas solo incluyen extensiones reconocidas como medios
multimedia. Las carpetas inaccesibles y del sistema se omiten en los
escaneos recursivos.

### Una emisión en directo no muestra duración ni porcentaje {#a-live-stream-has-no-duration-or-percentage}

Las transmisiones en directo a menudo no publican una duración fija. Las
funciones ir a tiempo, saltos por porcentaje, tiempo restante y saltar
al final requieren una duración conocida y pueden no estar disponibles.

### Un vídeo de YouTube no se abre {#a-youtube-video-does-not-open}

Pruebe estos pasos en orden:

1.  Confirme que la dirección se abre en un navegador y que corresponde
    a un vídeo o lista de reproducción, y no a un canal.
2.  Elija **Help \> Updates \> Update YouTube components**.
3.  Habilite la opción **Use yt-dlp to resolve streams** dentro de las
    Preferencias de YouTube.
4.  Pruebe primero el canal Estable (Stable); use Nocturno (Nightly) o
    Principal (Master) solo si el canal Estable no puede gestionar un
    cambio reciente del servicio.

### La grabación no encuentra un programa {#recording-cannot-find-a-program}

La captura de programas requiere Windows 10 versión 2004 o posterior.
Además, el programa debe tener una sesión de audio activa en Windows.
Inicie la reproducción en ese programa, vuelva a abrir el diálogo de
fuentes y selecciónelo en la lista actualizada.

### La grabación se inicia sin alguna de sus fuentes {#recording-starts-without-one-of-its-sources}

Luna puede continuar cuando al menos una fuente consigue abrirse. Lea el
aviso que enumera las fuentes fallidas y compruebe si sus dispositivos
están conectados, habilitados y disponibles. Detenga la grabación antes
de editar la lista de fuentes.

### Un atajo global no funciona {#a-global-shortcut-does-not-work}

Abra **Preferences \> Global Shortcuts** y aplique la combinación de
nuevo. Windows no permite que dos aplicaciones registren el mismo atajo
global. Elija otra combinación si Luna informa que uno o más atajos no
se pudieron registrar.

### La configuración no se guarda tras un error de inicio {#settings-will-not-save-after-a-startup-error}

Luna protege los archivos `settings.json` no válidos o ilegibles en
lugar de sobrescribirlos. Abra Preferencias e importe una copia de
seguridad válida o elija **Backup and restore \> Reset settings**. Si
necesita el original para un diagnóstico, cópielo primero desde la
carpeta de configuración de usuario.

### Los marcadores o favoritos notifican datos no válidos {#bookmarks-or-favorites-report-invalid-data}

Los archivos de almacenamiento corruptos se conservan en lugar de
reemplazarse en silencio. Abra la carpeta de configuración de usuario,
haga una copia del archivo JSON afectado y restaure una copia de
seguridad válida. Los marcadores se pueden restaurar a través de
Preferencias. Los favoritos actualmente requieren restaurar el propio
archivo `favorites.json` mientras Luna está cerrado.

### La guía del usuario no se abre {#the-user-guide-does-not-open}

La guía instalada debe estar ubicada en
`docs\<código-de-idioma>\user-guide.html` junto al ejecutable. Vuelva a
extraer el archivo portable completo o repare/reinstale la aplicación si
falta la carpeta `docs`. Luna intentará buscar primero el idioma
regional, luego el idioma base y finalmente el inglés.

## 21. Editor y licencias {#21-publisher-and-licensing}

Luna Player es publicado por **Diamond Star**.

Copyright © 2026 Diamond Star.

El código fuente original de Luna Player está bajo la licencia Apache
License, Versión 2.0. El enlace traducido de mpv en `src/Mpv.cs` se
distribuye bajo la licencia GNU Lesser General Public License, versión
2.1 o posterior.

Luna Player también utiliza componentes de terceros bajo licencias
independientes, incluyendo mpv, FFmpeg, wxWidgets, Prism, NAudio y
YoutubeExplode. Cada componente permanece bajo su propia licencia.
Consulte `NOTICE.txt`, instalado junto a Luna Player, para ver las
declaraciones de derechos de autor, versiones de componentes, nombres de
licencias y ubicaciones del código fuente. Los textos completos de las
licencias están en la carpeta `licenses`.

El resumen de licencias de esta guía se facilita por conveniencia.
`LICENSE.txt`, `NOTICE.txt` y los archivos de la carpeta `licenses`
contienen los términos y atribuciones oficiales. En el repositorio de
origen, los archivos principales equivalentes se llaman `LICENSE` y
`NOTICE`.

## 22. Contacto y soporte {#22-contact-and-support}

Luna Player es desarrollado y publicado por Diamond Star. Puede utilizar
los siguientes canales de contacto:

-   **Correo electrónico:** <ramymaherali55@gmail.com>
-   **Telegram:** [Contactar con Diamond Star en
    Telegram](https://t.me/diamondStar35)
-   **Informe de fallos:** [Incidencias de Luna Player en
    GitHub](https://github.com/diamondStar35/luna_player/issues)
-   **Código fuente y publicaciones:** [Luna Player en
    GitHub](https://github.com/diamondStar35/luna_player)

Para enviar un informe de fallo, incluya la versión de Luna mostrada en
**Help \> About**, su versión de Windows, qué esperaba que sucediera,
qué ocurrió y los pasos breves para reproducir el problema. Incluya el
texto exacto de cualquier mensaje de error. Elimine nombres de archivos
privados, direcciones web, datos de cuentas y otra información personal
de registros o capturas de pantalla antes de compartirlos públicamente.
:::
