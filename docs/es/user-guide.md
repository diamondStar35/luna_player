# Guía del usuario de Luna Player

Luna Player es un reproductor de audio y video para Windows pensado para usarse con el teclado. Reproduce medios locales, discos DVD-Video y DVD-Audio, transmisiones de red, YouTube, Spotify, radio por internet e IPTV; lee subtítulos en voz alta; descarga y convierte medios; graba audio; y ofrece administración de archivos, marcadores, anuncios por voz y atajos locales y de todo el sistema configurables.

Esta guía describe la versión actual de Luna Player. Los nombres de menús y los atajos que aparecen a continuación usan la interfaz en español y la configuración de atajos predeterminada.

[TOC]

## 1. Antes de comenzar

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

## 2. Interfaz y accesibilidad

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

- Presione <kbd>Alt</kbd> para acceder a la barra de menús y luego use las teclas de flecha y <kbd>Intro</kbd>.
- Presione <kbd>Tab</kbd> y <kbd>Mayús</kbd>+<kbd>Tab</kbd> para moverse entre los controles de los cuadros de diálogo.
- Use las teclas de flecha para desplazarse por listas y opciones.
- Presione <kbd>Intro</kbd> para activar el elemento seleccionado donde un cuadro de diálogo lo admita.
- Presione <kbd>Esc</kbd> para cerrar la mayoría de los cuadros de diálogo sin aplicar ningún cambio.
- Presione <kbd>F1</kbd> en la ventana principal para abrir esta guía.

En un control de Preferencias, <kbd>F1</kbd> tiene otra función: Luna lee en voz alta la ayuda contextual de ese control. Si el foco está en el árbol de categorías, explica cómo moverse entre las páginas de configuración.

### Anuncios por voz

Luna informa por medio de un lector de pantalla compatible el estado de la reproducción, el tiempo, el volumen, la navegación, las operaciones con archivos y otros cambios. Hay dos niveles de detalle disponibles:

- **Principiante** usa mensajes completos y explicativos.
- **Avanzado** usa confirmaciones más breves.

Presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>V</kbd> para cambiar el nivel de detalle de inmediato, o elíjalo en la página **General** de Preferencias. Habilite **Leer el nombre del archivo al navegar (Anterior/Siguiente)** si desea que Luna anuncie cada elemento al que se llega con Anterior o Siguiente.

### Controles multimedia de Windows

Luna se integra con la superposición multimedia de Windows y con las teclas multimedia de hardware compatibles. La superposición puede mostrar el título actual y la línea de tiempo. Los botones de reproducir, pausar, anterior, siguiente, retroceder y avance rápido invocan los comandos correspondientes de Luna.

Si un teclado, unos auriculares con micrófono u otro programa envían comandos multimedia a la aplicación equivocada o los envían dos veces, habilite **Deshabilitar los controles multimedia del sistema** en la página General de Preferencias. Esto quita a Luna de la superposición multimedia de Windows y evita que gestione las teclas multimedia de hardware.

## 3. Inicio rápido

Para reproducir un archivo local:

1. Presione <kbd>Ctrl</kbd>+<kbd>O</kbd>.
2. Elija un archivo multimedia, una lista de reproducción M3U/M3U8 o una lista de reproducción JSON de Luna.
3. Presione <kbd>Espacio</kbd> para pausar o reanudar.
4. Presione <kbd>Flecha izquierda</kbd> o <kbd>Flecha derecha</kbd> para avanzar o retroceder.
5. Presione <kbd>Flecha arriba</kbd> o <kbd>Flecha abajo</kbd> para cambiar el volumen.

Para reproducir todos los archivos compatibles de una carpeta, presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>O</kbd> y elija la carpeta. Use <kbd>Av Pág</kbd> y <kbd>Re Pág</kbd> para moverse entre los elementos cargados.

Para reproducir un video de YouTube, presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Y</kbd> e ingrese su dirección. Para buscar en cambio, presione <kbd>Ctrl</kbd>+<kbd>Y</kbd>.

Para encontrar una emisora de radio por internet, presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>R</kbd>. Para abrir Spotify, presione <kbd>Alt</kbd>+<kbd>S</kbd>; siga la configuración de la cuenta en la [sección de Spotify](#13-spotify) antes de reproducir su primera pista.

## 4. Apertura de medios

### Apertura de un archivo

Elija **Archivo > Abrir archivo...** o presione <kbd>Ctrl</kbd>+<kbd>O</kbd>. El cuadro de diálogo acepta archivos de audio, video, M3U, M3U8 y listas de reproducción JSON de Luna compatibles.

La opción **Qué desea abrir con los archivos?** controla lo que sucede cuando se abre un archivo multimedia desde Windows o desde el cuadro de diálogo de archivos:

- **Abrir solo el archivo** carga únicamente el archivo seleccionado.
- **Abrir el archivo y los archivos de la carpeta principal** carga los archivos compatibles de la misma carpeta y selecciona el archivo solicitado.
- **Abrir el archivo con los archivos de la carpeta principal y las subcarpetas** analiza la carpeta y todas las carpetas inferiores, carga todos los archivos multimedia compatibles que encuentre y selecciona el archivo solicitado.

Un análisis recursivo muestra un cuadro de diálogo de progreso cancelable e informa cuántos archivos multimedia ha encontrado. Los archivos se ordenan según el orden natural de Windows.

Cuando se envían varios archivos juntos a Luna y está seleccionada la opción **Abrir solo el archivo**, Luna carga todos esos archivos seleccionados explícitamente en lugar de descartar todos menos uno.

### Apertura de una carpeta

Elija **Archivo > Abrir carpeta...** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>O</kbd>.

Con cualquiera de los dos primeros modos de apertura de archivos, Luna carga los medios compatibles que están directamente dentro de la carpeta elegida. Con el modo de subcarpetas, realiza un análisis recursivo. Las carpetas inaccesibles y del sistema se omiten durante un análisis recursivo.

### Apertura de un DVD

Elija **Archivo > Abrir DVD...**. En el selector de carpetas, seleccione la unidad de DVD o una carpeta que contenga el directorio `VIDEO_TS` o `AUDIO_TS` de un disco. También puede seleccionar ese directorio directamente. Use **Tipo de disco** para elegir **DVD-Video** o **DVD-Audio**. DVD-Video reproduce la banda sonora de un video además de su imagen; DVD-Audio es el formato de disco de audio independiente, no otro nombre para la banda sonora de un DVD de video.

Luna lee en segundo plano los títulos reproducibles del disco. Un título es un programa independiente del disco, como una película, un extra o un grupo de pistas de música. Deje **Título** en **Automático** para usar el título reproducible más largo, o seleccione un título concreto cuando haya varios disponibles. Elija **Abrir** para comenzar la reproducción. Si la inspección ya terminó, la reproducción se abre sin una ventana de progreso inicial. De lo contrario, **Abriendo DVD** muestra el progreso de la inspección que ya está en curso; no vuelve a comenzar a leer el disco.

Cuando un disco tiene varios títulos, Anterior y Siguiente se mueven entre títulos. Cuando solo hay un título reproducible, se mueven entre sus capítulos. También puede usar **Reproductor > Capítulos** y los atajos de capítulos para desplazarse dentro de un título. Los menús del disco no se muestran; use las listas de títulos y de capítulos para llegar directamente al contenido.

Luna informa un error si la ubicación no contiene el formato de disco seleccionado o no tiene títulos reproducibles. Si DVD-Audio produce un mensaje sobre títulos de audio ausentes, pruebe DVD-Video solo si el disco es un DVD de video. Algunos discos DVD-Audio también contienen un área DVD-Video independiente, cuyo contenido puede diferir del área de audio.

El instalador y el comando **Registrar extensiones de archivo** también registran acciones de reproducción automática de DVD. Windows puede ofrecer Luna cuando se inserta un DVD y como **Reproducir DVD con Luna Player** en el Explorador. Elija Luna en la configuración de Reproducción automática de Windows si desea que gestione automáticamente la inserción de discos. Abrir un disco desde el Explorador detecta su tipo, dando preferencia al área DVD-Audio cuando están presentes ambos formatos, y muestra **Abriendo DVD** mientras lo lee. Estos registros se aplican a los DVD; los discos Blu-ray y los CD de audio no están cubiertos por esta función.

### Apertura de una transmisión de red

Elija **Archivo > Abrir enlace...** o presione <kbd>Ctrl</kbd>+<kbd>L</kbd>. Ingrese una dirección `http` o `https`.

Use este comando para transmisiones multimedia directas y listas de reproducción M3U o M3U8 remotas. Use **Abrir enlace de YouTube...** para direcciones de videos y listas de reproducción de YouTube.

### Apertura de listas de reproducción

Luna admite listas de reproducción M3U y M3U8 locales y remotas. Las entradas relativas de una lista de reproducción local se resuelven a partir de la carpeta de la lista; las entradas relativas de una lista de reproducción de red se resuelven a partir de su dirección.

Un manifiesto HLS M3U8 se trata como una sola transmisión y se pasa al motor de reproducción. No se expande en una lista de segmentos de medios. Las demás listas de reproducción se convierten en una lista de archivos abiertos que contiene sus rutas locales reproducibles y sus enlaces web.

### Cómo agregar a la lista actual

Elija **Archivo > Agregar archivos...** o presione <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>F</kbd> para seleccionar uno o más archivos. Luna agrega los archivos seleccionados al final de la lista de archivos abiertos. La pista actual sigue reproduciéndose y los archivos que ya estaban en la lista conservan su orden.

Para agregar una carpeta, elija **Archivo > Agregar carpeta...** o presione <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>D</kbd>. Luna agrega los archivos multimedia compatibles que están directamente dentro de esa carpeta. No analiza las subcarpetas, sea cual sea el modo de apertura de archivos seleccionado en Preferencias. Si guarda la lista como una lista de reproducción con nombre o como un archivo JSON, Luna recuerda la carpeta en sí, de modo que los cambios en su contenido se reflejan cuando vuelve a abrir la lista de reproducción.

Agregar archivos y guardar la lista actual no están disponibles mientras está en una sesión de exploración en línea. Salga de esa sesión antes de usar estos comandos.

### Listas de reproducción guardadas

Use **Archivo > Listas de reproducción** para conservar colecciones que puede volver a abrir sin elegir sus archivos cada vez. Cada lista de reproducción guardada tiene un nombre y una descripción opcional. Puede crear una lista de reproducción vacía, armar una seleccionando archivos y carpetas, o guardar la lista que ya tiene abierta.

Puede dejar Mis listas de reproducción abierto mientras usa la ventana principal del reproductor. Elegir **Mis listas de reproducción...** otra vez trae al frente la ventana existente. La lista se actualiza cuando crea, guarda o quita una lista de reproducción, y abrir una lista de reproducción deja el administrador abierto para que pueda elegir otra más tarde.

Elegir **Nueva** o **Editar** abre un editor que pertenece a Mis listas de reproducción. El editor deshabilita temporalmente Mis listas de reproducción, mientras que la ventana principal del reproductor sigue siendo utilizable y la reproducción continúa. Termine o cancele la edición para devolver el control a Mis listas de reproducción.

- **Mis listas de reproducción...** abre sus listas de reproducción guardadas. La lista **Elementos** muestra sus nombres y descripciones. Seleccione una lista de reproducción para habilitar sus comandos Abrir, Editar, Quitar y Exportar.
- **Crear lista de reproducción...** abre el editor de listas de reproducción con una lista vacía. Ingrese un nombre, agregue los archivos o las carpetas que desee y elija **Guardar**. La lista de reproducción aparece entonces en Mis listas de reproducción.
- **Guardar la lista de reproducción actual...** usa la lista de archivos abiertos actual como punto de partida. Ingrese un nombre y, si lo desea, una descripción, y luego elija **Guardar** para agregarla a Mis listas de reproducción. No necesita crear una lista de reproducción vacía y volver a agregar los mismos archivos. Para cambiar su contenido más tarde, selecciónela en Mis listas de reproducción y elija **Editar**.
- **Guardar lista de reproducción como...** guarda la lista actual en un archivo fuera de Mis listas de reproducción. Elija un nombre de archivo, un destino y el formato M3U8, M3U o JSON. Si elige JSON, Luna también solicita el nombre de la lista de reproducción y una descripción opcional. Cancelar cualquiera de los dos cuadros de diálogo deja el destino sin cambios.

En Mis listas de reproducción, **Abrir** reemplaza la lista de archivos abiertos y reproduce la primera pista disponible. **Editar** le permite cambiar el nombre, la descripción, el contenido y el orden de la lista de reproducción guardada sin interrumpir la reproducción. Estos cambios surten efecto en la reproducción la próxima vez que abra esa lista de reproducción. **Quitar** pregunta “Desea quitar esta lista de reproducción?” antes de eliminar la lista de reproducción guardada; sus archivos multimedia y carpetas permanecen en el disco. **Exportar** guarda una copia de la lista de reproducción seleccionada en uno de los formatos de lista de reproducción compatibles.

Después de que una lista de reproducción se guarda o se exporta, Luna muestra un mensaje de **Éxito** en la ventana principal que dice “Lista de reproducción guardada.” Quitar una lista de reproducción guardada muestra “Lista de reproducción quitada.” en un mensaje de Éxito. Elija **Aceptar** para cerrar la confirmación. Estos mensajes confirman cambios completados; cancelar un editor o un cuadro de diálogo de guardado no muestra un mensaje de éxito.

Si algunos archivos o carpetas no están disponibles, Luna los informa al abrir la lista de reproducción. Si no queda ningún archivo reproducible, Luna conserva la lista actual y el estado de reproducción. Las entradas no disponibles permanecen en la lista de reproducción guardada para que pueda hacerlas disponibles de nuevo o quitarlas en el editor.

Los archivos guardados con Guardar lista de reproducción como o con Exportar no aparecen automáticamente en Mis listas de reproducción. Para abrir un archivo de lista de reproducción JSON de Luna, elija **Archivo > Abrir archivo...** y seleccione el filtro de listas de reproducción JSON. Mis listas de reproducción almacena sus propios archivos JSON en la carpeta `Playlists` dentro de la carpeta de configuración del usuario de Luna; consulte **Configuración y datos del usuario** para ver su ubicación.

### Edición de una lista de reproducción

Ingrese un nombre de lista de reproducción y una descripción opcional. El nombre es obligatorio al guardar, y las listas de reproducción de Mis listas de reproducción deben tener nombres diferentes. La descripción es un lugar para explicar qué contiene la lista de reproducción o cuándo la usa. Puede guardar una lista de reproducción antes de agregar archivos y volver a ella más tarde a través de Mis listas de reproducción.

Elija **Agregar > Archivos...** para seleccionar uno o más archivos individuales en el cuadro de diálogo de archivos. Elija **Agregar > Carpeta...** para incluir una carpeta. La lista **Elementos** muestra cada archivo o carpeta en las columnas **Nombre** y **Ruta completa**, de modo que pueda distinguir archivos con el mismo nombre en ubicaciones diferentes. También puede presionar <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>F</kbd> para agregar archivos o <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>D</kbd> para agregar una carpeta.

Seleccione las entradas que desea cambiar. Con la lista de entradas enfocada, están disponibles las siguientes teclas:

| Tecla | Acción |
| --- | --- |
| <kbd>Supr</kbd> | Quita las entradas seleccionadas solo de la lista de reproducción |
| <kbd>Mayús</kbd>+<kbd>Flecha arriba</kbd> / <kbd>Mayús</kbd>+<kbd>Flecha abajo</kbd> | Mueve las entradas seleccionadas hacia arriba / abajo, conservando su orden relativo |
| <kbd>Ctrl</kbd>+<kbd>A</kbd> | Selecciona todas las entradas |

Los botones **Quitar**, **Subir** y **Bajar** realizan las mismas operaciones. Quitar está disponible cuando hay al menos una entrada seleccionada. Subir está disponible cuando la selección puede moverse sin pasar de la primera entrada; Bajar está disponible cuando puede moverse sin pasar de la última entrada. Quitar una entrada cambia solo la lista de reproducción y no elimina el archivo ni la carpeta del disco.

Elija **Guardar** para conservar los cambios de nombre, descripción y entradas. Si cierra el editor después de hacer cambios, incluso con **Cancelar**, <kbd>Esc</kbd> o <kbd>Alt</kbd>+<kbd>F4</kbd>, Luna pregunta si desea guardarlos. Elija **Sí** para guardar, **No** para descartar los cambios o **Cancelar** para seguir editando. Cerrar sin guardar deja sin cambios la lista de reproducción guardada anteriormente.

Si el guardado falla, el editor permanece abierto con sus cambios para que pueda corregir el problema o intentarlo de nuevo. Cuando sale de Luna con editores de listas de reproducción todavía abiertos, Luna comprueba sus cambios sin guardar antes de salir. Elegir **Cancelar** mantiene abiertos a Luna y al editor.

### Carpetas de listas de reproducción y formatos de archivo

Una carpeta sigue siendo una sola entrada cuando guarda una lista de reproducción en Mis listas de reproducción o como un archivo JSON. Cada vez que abre esa lista de reproducción, Luna lee los archivos compatibles que están directamente dentro de la carpeta y los ordena por nombre. Por lo tanto, agregar o quitar archivos en la carpeta cambia lo que Luna carga la próxima vez que la abre. Las subcarpetas no se incluyen. Los archivos ausentes y las carpetas vacías o inaccesibles se informan, y sus entradas permanecen en la lista de reproducción guardada.

Elija **JSON** cuando desee conservar el nombre, la descripción y las entradas de carpeta de la lista de reproducción. Elija **M3U** o **M3U8** cuando desee una lista de rutas y títulos de medios individuales que pueda abrir en otro reproductor. Ambos formatos M3U usan UTF-8. Cuando exporta una carpeta a uno de estos formatos, Luna escribe los archivos compatibles que contiene en ese momento en lugar de la carpeta en sí. Los cambios posteriores en la carpeta no se reflejan en ese archivo exportado; exporte la lista de reproducción de nuevo para incluirlos.

El orden de la lista de reproducción se conserva cuando se guarda o se exporta, incluidas las entradas repetidas. Los archivos de una carpeta aparecen juntos en la posición de esa carpeta en la lista. Guardar o exportar una lista de reproducción nunca mueve, copia ni elimina los archivos multimedia que contiene.

### Apertura de medios desde el portapapeles

Copie un archivo o una carpeta en el Explorador de archivos, vuelva a Luna y presione <kbd>Ctrl</kbd>+<kbd>V</kbd>. Luna abre la primera ruta existente del portapapeles según el modo de apertura de archivos configurado.

### Reapertura de medios recientes

El submenú **Archivo > Recientes** conserva listas separadas de archivos, carpetas y listas de reproducción abiertos recientemente. Elija una entrada para abrirla de nuevo. Cada lista tiene su propio comando **Borrar**, y una lista vacía muestra un mensaje deshabilitado en lugar de un comando.

El historial se almacena en la carpeta de configuración del usuario de Luna. Borrar una lista reciente elimina solo su historial; no elimina ningún medio.

### Asociaciones de archivos

El instalador puede registrar Luna para los tipos de archivo compatibles. También puede usar **Preferencias > General > Registrar extensiones de archivo** o **Anular el registro de extensiones de archivo**.

El registro agrega Luna a la lista de aplicaciones de Windows para los medios compatibles. Es posible que Windows todavía le exija seleccionar Luna en **Configuración > Aplicaciones > Aplicaciones predeterminadas** para que se convierta en el reproductor predeterminado.

## 5. Controles de reproducción

### Reproducir y pausar

Presione <kbd>Espacio</kbd> o <kbd>Intro</kbd>, elija **Reproductor > Reproducir/Pausar**, o use el botón Reproducir/Pausar de la ventana principal.

Si un elemento terminado o descargado se puede volver a abrir, Reproducir/Pausar lo vuelve a cargar. En el nivel Principiante, Luna anuncia Reproducir o Pausar; el nivel Avanzado evita estos mensajes de estado más largos.

### Avanzar y retroceder

Presione <kbd>Flecha izquierda</kbd> o <kbd>Flecha derecha</kbd> para moverse un paso de desplazamiento. Hay movimientos mayores disponibles sin cambiar el paso seleccionado:

- <kbd>Mayús</kbd>+<kbd>Flecha izquierda</kbd> o <kbd>Mayús</kbd>+<kbd>Flecha derecha</kbd> se mueve dos pasos.
- <kbd>Inicio</kbd> se mueve al principio.
- <kbd>Fin</kbd> se mueve al final.

La cantidad de desplazamiento predeterminada es de 5 segundos. Elija otro valor en **Reproductor > Cantidad de desplazamiento**, o presione <kbd>Mayús</kbd> más un número:

| Atajo | Cantidad de desplazamiento |
| --- | --- |
| <kbd>Mayús</kbd>+<kbd>1</kbd> | 1 segundo |
| <kbd>Mayús</kbd>+<kbd>2</kbd> | 5 segundos |
| <kbd>Mayús</kbd>+<kbd>3</kbd> | 10 segundos |
| <kbd>Mayús</kbd>+<kbd>4</kbd> | 20 segundos |
| <kbd>Mayús</kbd>+<kbd>5</kbd> | 30 segundos |
| <kbd>Mayús</kbd>+<kbd>6</kbd> | 1 minuto |
| <kbd>Mayús</kbd>+<kbd>7</kbd> | 2 minutos |
| <kbd>Mayús</kbd>+<kbd>8</kbd> | 3 minutos |
| <kbd>Mayús</kbd>+<kbd>9</kbd> | 5 minutos |
| <kbd>Mayús</kbd>+<kbd>0</kbd> | 10 minutos |
| <kbd>Mayús</kbd>+<kbd>-</kbd> | Valor personalizado de las Preferencias de audio |

La cantidad seleccionada se guarda de inmediato. Cambiar **Valor de desplazamiento personalizado (segundos)** no selecciona automáticamente el valor personalizado; selecciónelo desde el menú o con <kbd>Mayús</kbd>+<kbd>-</kbd> cuando desee usarlo.

### Ir a un tiempo

Elija **Reproductor > Ir al tiempo...** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>G</kbd>. Ingrese las horas, los minutos y los segundos que sean pertinentes para la duración actual. Luna impide que la posición seleccionada supere el final del archivo.

### Saltar por porcentaje

Elija **Reproductor > Saltar al porcentaje** o use los atajos de porcentaje predeterminados:

- <kbd>Ctrl</kbd>+<kbd>1</kbd> hasta <kbd>Ctrl</kbd>+<kbd>9</kbd> saltan del 10% al 90%.
- <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>1</kbd> hasta <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>9</kbd> saltan del 15% al 95%.
- <kbd>Ctrl</kbd>+<kbd>0</kbd> o <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>0</kbd> salta al 100%.

Los saltos por porcentaje requieren una duración conocida y, por lo tanto, no están disponibles en algunas transmisiones en vivo.

### Obtención de información de reproducción

Luna puede leer en voz alta información de reproducción sin mover el foco:

- <kbd>E</kbd> lee el tiempo transcurrido.
- <kbd>R</kbd> lee el tiempo restante.
- <kbd>T</kbd> lee la duración total.
- <kbd>P</kbd> lee el porcentaje actual.
- <kbd>V</kbd> lee el volumen actual.
- <kbd>S</kbd> lee la velocidad actual.
- <kbd>Mayús</kbd>+<kbd>P</kbd> lee el ajuste de tono.
- <kbd>B</kbd> lee el valor del balance izquierdo/derecho.

### Nombre de archivo, ruta y título del medio

Presione <kbd>F</kbd> repetidamente para obtener información progresivamente más detallada sobre el elemento actual:

1. La primera pulsación lee el nombre del archivo, el nombre de la transmisión o el título del video de YouTube.
2. Una segunda pulsación rápida lee la ruta local completa o la dirección de la transmisión.
3. Una tercera pulsación rápida copia esa ruta o dirección al portapapeles.

Si hace una pausa de más de un breve instante, la secuencia vuelve a comenzar con el nombre mostrado.

Presione <kbd>I</kbd> para leer el título almacenado en los metadatos del medio. Es independiente del nombre del archivo; un archivo local puede no tener un título incrustado o puede tener un título distinto de su nombre.

### Volumen

Presione <kbd>Flecha arriba</kbd> o <kbd>Flecha abajo</kbd> para cambiar el volumen según el paso de volumen configurado. El paso predeterminado es de 5 puntos porcentuales.

- <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Flecha arriba</kbd> establece el volumen en el máximo de Luna.
- <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Flecha abajo</kbd> establece el volumen en 5%.
- <kbd>V</kbd> lee el volumen actual.

Luna permite la amplificación por encima del 100%, hasta el 2000%. Una amplificación alta puede aumentar el ruido de fondo y la distorsión de la fuente. La normalización dinámica y el limitador pueden ayudar a contener los picos, pero comience con un nivel moderado para proteger su audición y sus equipos.

### Velocidad de reproducción

Presione <kbd>Ctrl</kbd>+<kbd>Flecha arriba</kbd> o <kbd>Ctrl</kbd>+<kbd>Flecha abajo</kbd> para cambiar la velocidad de reproducción. Presione <kbd>Alt</kbd>+<kbd>Y</kbd> para volver a 1x. La velocidad está limitada de 0.5x a 4x, y el paso se puede configurar en la página de Preferencias de audio.

### Tono

Los cambios de tono no modifican la velocidad de reproducción.

- <kbd>Mayús</kbd>+<kbd>Flecha arriba</kbd> sube el tono.
- <kbd>Mayús</kbd>+<kbd>Flecha abajo</kbd> baja el tono.
- <kbd>Alt</kbd>+<kbd>Mayús</kbd>+<kbd>P</kbd> restablece el tono original.
- <kbd>Mayús</kbd>+<kbd>P</kbd> lee el ajuste actual en semitonos.

El tono está limitado a 12 semitonos por encima o por debajo del original. El paso se puede configurar de 0.001 a 12 semitonos.

### Balance estéreo

- <kbd>Ctrl</kbd>+<kbd>Flecha izquierda</kbd> mueve el sonido hacia la izquierda.
- <kbd>Ctrl</kbd>+<kbd>Flecha derecha</kbd> mueve el sonido hacia la derecha.
- <kbd>B</kbd> lee el porcentaje de balance actual.

Cero es el centro, los valores negativos son izquierda y los valores positivos son derecha. El paso de balance se puede configurar de 1 a 100 puntos porcentuales.

### Elección de un dispositivo de salida

Elija **Reproductor > Tarjetas de sonido...** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>A</kbd>. Seleccione un dispositivo de salida de audio de Windows y presione Aceptar. El dispositivo seleccionado se guarda de inmediato.

### Video en pantalla completa

Elija **Reproductor > Pantalla completa** o presione <kbd>F11</kbd> para entrar en el modo de pantalla completa o salir de él. Este comando solo está disponible mientras el elemento actual tiene video. Presionar <kbd>Esc</kbd> también sale del modo de pantalla completa.

### Elección de una pista de audio

Los archivos pueden contener varias pistas de audio, como diferentes idiomas o comentarios. Use **Reproductor > Pista de audio**:

- Presione <kbd>Mayús</kbd>+<kbd>Av Pág</kbd> para la pista siguiente.
- Presione <kbd>Mayús</kbd>+<kbd>Re Pág</kbd> para la pista anterior.
- Elija **Lista de pistas...** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>T</kbd> para seleccionar de una lista.

Luna describe cada pista usando su título, la disposición de canales y el idioma cuando esa información está disponible. Al pasar de la primera o de la última pista de audio no se vuelve al otro extremo.

### Lectura de subtítulos en voz alta

El submenú **Reproductor > Subtítulos** enumera las pistas de subtítulos del archivo actual. Elija una pista para que se lea en voz alta cada línea de subtítulo a medida que aparece, o elija **Desactivado** para dejar de leer los subtítulos. Presione <kbd>Ctrl</kbd>+<kbd>U</kbd> para activar o desactivar la última pista seleccionada.

También puede cargar subtítulos que no están incrustados en el video:

- Elija **Cargar desde archivo...** o presione <kbd>Ctrl</kbd>+<kbd>J</kbd> para un archivo de subtítulos de este equipo.
- Elija **Cargar desde URL...** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>J</kbd> para una dirección `http` o `https`.

Habilite **Recordar el subtítulo durante esta sesión**, o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>U</kbd>, para mantener el idioma de subtítulo seleccionado en toda la lista de reproducción actual. Esta elección dura solo hasta que Luna se cierre. Si el archivo siguiente no tiene subtítulo en ese idioma, Luna deja sin cambios su selección de subtítulo.

De forma predeterminada, Luna termina de leer una línea de subtítulo antes de leer la siguiente. Habilite **Interrumpir la línea actual cuando aparece un nuevo subtítulo** en la página General de Preferencias si es más importante mantener el ritmo de la imagen que escuchar cada línea completa.

### Capítulos y audiolibros

Los audiolibros M4B se abren como otros archivos de audio. **Reproductor > Capítulos** enumera los capítulos del archivo actual como **número. título: duración**. La duración es la longitud del capítulo completo. Este menú no está disponible cuando el archivo no tiene capítulos.

- Presione <kbd>Ctrl</kbd>+<kbd>Re Pág</kbd> para el capítulo anterior o <kbd>Ctrl</kbd>+<kbd>Av Pág</kbd> para el capítulo siguiente. Estos comandos permanecen dentro del libro actual y anuncian el capítulo de destino.
- Presione <kbd>C</kbd> para escuchar el nombre del capítulo actual.
- Seleccione un capítulo en el menú para saltar a su comienzo. La reproducción normal no anuncia los nombres de los capítulos cuando cambian de forma natural.
- Marque **Detener la reproducción al final del capítulo actual** para pausar una sola vez al final de ese capítulo. Si pasa a otro capítulo mientras está marcada, la detención sigue a ese capítulo. Tiene prioridad sobre la repetición y el avance automático, y luego se borra sola. Presione Reproducir para continuar con el capítulo siguiente.

Marque **Reproductor > Capítulos > Modo audiolibro** para tratar los capítulos como elementos individuales de la lista de reproducción. Luna recuerda esta elección. Activarlo o desactivarlo conserva la misma posición de escucha y el mismo estado de pausa.

Los archivos sin capítulos usan la reproducción normal incluso cuando el modo audiolibro está habilitado. No necesita desactivar el modo antes de abrir música u otro archivo sin capítulos.

En el modo audiolibro, la lista de archivos abiertos expande cada libro en sus capítulos; los archivos sin capítulos siguen siendo elementos únicos. Los comandos de archivo anterior/siguiente, la reproducción aleatoria, la repetición, el comportamiento al final del archivo y los temporizadores de sueño de fin de pista se aplican al capítulo activo. El tiempo transcurrido, el restante y el total se refieren a ese capítulo. Retroceder se detiene en su comienzo; llegar a su final, incluso desplazándose, sigue el comportamiento final configurado. Los bucles A-B y los puntos de selección permanecen dentro del capítulo, y seleccionar un capítulo diferente los borra. Los anuncios automáticos de capítulos siguen la opción **Leer el nombre del archivo al navegar (Anterior/Siguiente)**.

Los marcadores y las posiciones de escucha recordadas pertenecen al libro completo. Un marcador en otro capítulo selecciona ese capítulo y su posición guardada. Copiar, cambiar el nombre, eliminar, marcar o cerrar un capítulo actúa sobre su libro de origen completo, incluidas todas sus entradas de capítulos.

## 6. Trabajo con varios archivos

### Anterior, siguiente, primero y último

- <kbd>Re Pág</kbd> reproduce el elemento anterior. <kbd>Mayús</kbd>+<kbd>Tab</kbd> es su atajo secundario.
- <kbd>Av Pág</kbd> reproduce el elemento siguiente. <kbd>Tab</kbd> es su atajo secundario.
- <kbd>Ctrl</kbd>+<kbd>Inicio</kbd> reproduce el primer elemento.
- <kbd>Ctrl</kbd>+<kbd>Fin</kbd> reproduce el último elemento.

Si **Volver al principio con varios archivos** está habilitado, Siguiente en el último elemento vuelve al primero y Anterior en el primero vuelve al último. El avance automático sigue la misma regla.

Puede desactivar los atajos secundarios en **Preferencias > Atajos de teclado**. Esto permite que <kbd>Tab</kbd> y <kbd>Mayús</kbd>+<kbd>Tab</kbd> se muevan entre los controles de la ventana principal mientras <kbd>Av Pág</kbd> y <kbd>Re Pág</kbd> siguen cambiando de pista.

### Ir a un número de archivo

Elija **Reproductor > Ir al archivo...** o presione <kbd>Ctrl</kbd>+<kbd>G</kbd>. Ingrese un número desde 1 hasta la cantidad de elementos cargados.

### Cuadro de diálogo Archivos abiertos

Elija **Archivo > Archivos abiertos...** o presione <kbd>F2</kbd>. El elemento actual aparece seleccionado en la lista.

- Elija un elemento y active **Saltar al seleccionado**, o presione <kbd>Intro</kbd> sobre él, para reproducirlo.
- Active **Información de la lista de reproducción** para calcular la cantidad de archivos, el tamaño total, la duración total, el tiempo transcurrido en la lista de reproducción y el tiempo restante. El análisis es cancelable y el resultado se abre en una ventana de texto de solo lectura.

La lista está diseñada para seguir respondiendo incluso con una cantidad muy grande de archivos cargados.

### Reproducción aleatoria

Elija **Reproductor > Reproducción aleatoria** o presione <kbd>Ctrl</kbd>+<kbd>Z</kbd>. Al habilitar la reproducción aleatoria se genera un orden de reproducción al azar manteniendo seleccionado el elemento actual.

Mientras la reproducción aleatoria está activada, Anterior, Siguiente, Primer archivo, Último archivo, Ir al archivo y el cuadro de diálogo Archivos abiertos operan sobre el orden aleatorio. Al desactivarla se restablece el orden normal de la lista conservando el elemento actual.

### Repetición y comportamiento al final del archivo

Elija **Reproductor > Repetir archivo** o presione <kbd>Ctrl</kbd>+<kbd>R</kbd> para repetir el elemento actual. Esta opción de tiempo de ejecución tiene prioridad mientras está habilitada.

La página de Preferencias de audio también controla la acción normal al final de un archivo:

- **Avanzar al siguiente archivo** inicia el elemento siguiente.
- **Repetir el archivo** inicia de nuevo el mismo elemento.
- **No hacer nada** deja la reproducción al final.

### Recordar posiciones

Dos opciones cumplen propósitos diferentes:

- **Recordar la posición del último archivo** restaura el archivo local activo y el tiempo de reproducción la próxima vez que se inicie Luna.
- **Guardar la posición actual de cada archivo** recuerda una posición independiente para cada elemento local y vuelve a ella cuando se revisita ese elemento.

Si ambas están deshabilitadas, los archivos normalmente comienzan desde el principio.

## 7. Bucles A-B

Un bucle A-B repite una parte del elemento actual.

1. Desplácese hasta el comienzo deseado y presione <kbd>[</kbd>. Esto establece el punto A.
2. Desplácese hasta una posición posterior y presione <kbd>]</kbd>. Esto establece el punto B y comienza el bucle de A a B.
3. Presione <kbd>Retroceso</kbd> para borrar el bucle y continuar desde el punto B.

Mientras un bucle está activo, el desplazamiento normal queda limitado al rango seleccionado. <kbd>Inicio</kbd> va al punto A y <kbd>Fin</kbd> va al punto B. El punto B debe ser posterior al punto A.

La selección se aplica al elemento actual y se borra cuando la reproducción pasa a otro elemento.

## 8. Eliminación de silencios y procesamiento de audio

### Activación o desactivación de la eliminación de silencios

Elija **Reproductor > Habilitar el filtro de eliminación de silencios** o presione <kbd>Ctrl</kbd>+<kbd>M</kbd>. Luna guarda de inmediato el estado de activado/desactivado.

La eliminación de silencios acorta las secciones silenciosas mientras se reproducen los medios. No modifica el archivo original. Los resultados dependen en gran medida de la fuente: un umbral agresivo puede tratar el habla o la música suaves como silencio.

### Configuración básica de silencios

Abra **Preferencias > Eliminación de silencios**.

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

La página **Preferencias > Audio > Reproducción** contiene dos filtros adicionales:

- **Habilitar normalización dinámica y limitador** uniforma el volumen cambiante y contiene los picos para reducir la saturación.
- **Reproducir el audio en mono** combina los canales izquierdo y derecho en una única salida mono.

Ninguna de las dos opciones modifica el archivo de origen.

### Espacialización para auriculares

La espacialización para auriculares hace que los canales de audio suenen como altavoces virtuales alrededor de su cabeza. Usa una función de transferencia relacionada con la cabeza, o HRTF: mediciones de cómo el sonido de diferentes direcciones llega a los oídos de quien escucha. Escuche con auriculares para oír el efecto. Esto cambia el sonido durante la reproducción y no altera el archivo multimedia original.

#### Descarga de un conjunto de datos HRTF

**Luna Player no incluye ningún conjunto de datos HRTF.** Descargue por separado un archivo `.sofa` compatible y manténgalo en una carpeta donde seguirá disponible. SOFA es el formato de archivo usado para almacenar las mediciones; elija un archivo que contenga respuestas `SimpleFreeFieldHRIR` para la escucha con auriculares.

SADIE D1 es un punto de partida útil y también se proporciona en los archivos HRTF de Steam Audio. Los siguientes enlaces descargan archivos SOFA directamente:

| Conjunto de datos | Descarga y uso |
| --- | --- |
| SADIE D1 | [Descargar sadie_d1.sofa de Steam Audio](https://raw.githubusercontent.com/ValveSoftware/steam-audio/master/core/data/hrtf/sadie_d1.sofa). Comience aquí si todavía no tiene una HRTF preferida. |
| SADIE H12 | [Descargar sadie_h12.sofa de Steam Audio](https://raw.githubusercontent.com/ValveSoftware/steam-audio/master/core/data/hrtf/sadie_h12.sofa). Un conjunto alternativo de mediciones para comparar con D1. |
| SADIE D2 | [Descargar D2 de la base de datos SOFA](https://sofacoustics.org/data/database/sadie/D2_48K_24bit_256tap_FIR_SOFA.sofa). Otra alternativa si D1 no le da direcciones convincentes. |

Puede encontrar más mediciones en la [base de datos SADIE](https://www.york.ac.uk/sadie-project/database.html) y en la [colección de conjuntos de datos SOFA](https://sofacoustics.org/data/database/). Descargue el archivo `.sofa` en sí en lugar de guardar la página web que lo describe. Si su navegador abre el archivo en lugar de guardarlo, use su comando de guardado o el comando **Guardar enlace como** del enlace y conserve la extensión `.sofa`.

No hay un único conjunto de datos que suene mejor para todos. Compare algunos con la misma grabación conocida y un volumen similar, prestando atención a posiciones claras de izquierda/derecha, adelante/atrás y arriba/abajo. Una descarga más grande o una frecuencia de muestreo de medición más alta no hacen por sí solas que un conjunto de datos se adapte mejor a sus oídos.

#### Configuración de la espacialización

1. Abra **Preferencias > Audio > Espacialización para auriculares**.
2. Seleccione **Habilitar la espacialización para auriculares** para mostrar los demás controles.
3. Elija **Examinar...** y seleccione el archivo `.sofa` descargado.
4. Establezca la ganancia, los ángulos iniciales y los pasos de los atajos según sea necesario, y luego elija **Aceptar**.

La espacialización está desactivada de forma predeterminada. Si desea que esté desactivada al iniciar pero disponible desde el menú Reproductor, seleccione un archivo y configure los valores predeterminados, y luego desmarque **Habilitar la espacialización para auriculares** antes de elegir Aceptar. Los demás campos quedan ocultos mientras la casilla está desmarcada, pero sus valores se conservan.

| Opción | Finalidad |
| --- | --- |
| Ganancia (dB) | Ajuste de salida de -20 a 40 dB, inicialmente 0. Redúzcala si el resultado suena demasiado fuerte o distorsionado. Se aceptan valores decimales. |
| Rotación (grados) | Ángulo horizontal inicial, de -360 a 360, inicialmente 0. Los valores positivos giran a la derecha; los valores negativos giran a la izquierda. |
| Elevación (grados) | Ángulo vertical inicial, de -90 a 90, inicialmente 0. Los valores positivos elevan el sonido; los valores negativos lo bajan. |
| Paso de rotación (grados) | Cambio que hace cada comando de rotación, de 1 a 90 grados. El valor predeterminado es 5. |
| Paso de elevación (grados) | Cambio que hace cada comando de elevación, de 1 a 90 grados. El valor predeterminado es 5. |

La rotación, la elevación y sus pasos usan controles numéricos de grados enteros. La ganancia usa un campo de texto, de modo que puede ingresar un ajuste decimal.

#### Cambio de dirección durante la reproducción

Use **Reproductor > Espacialización para auriculares** o estos atajos predeterminados:

| Comando | Atajo predeterminado | Efecto |
| --- | --- | --- |
| Habilitar la espacialización para auriculares | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>H</kbd> | Activa o desactiva el efecto durante esta sesión. No está disponible hasta que se configura una ruta SOFA en Preferencias. |
| Girar a la izquierda | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Flecha izquierda</kbd> | Resta el paso de rotación. |
| Girar a la derecha | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Flecha derecha</kbd> | Suma el paso de rotación. |
| Subir la elevación | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Flecha arriba</kbd> | Suma el paso de elevación. |
| Bajar la elevación | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Flecha abajo</kbd> | Resta el paso de elevación. |

Los comandos de dirección están disponibles mientras la espacialización está habilitada. La rotación da la vuelta a un círculo completo: un paso predeterminado a la izquierda desde 0 pasa a ser 355 grados. La elevación se detiene en -90 grados, directamente debajo de usted, y en 90 grados, directamente encima de usted. Cerca de esas posiciones verticales, cambiar la rotación puede producir una diferencia audible pequeña. Para oír con más claridad el movimiento alrededor de su cabeza, vuelva a llevar la elevación hacia 0.

Luna anuncia cada cambio. Por ejemplo, el nivel Principiante dice “Rotación 270 grados” y el Avanzado dice “270 grados.” Estos cambios duran la sesión actual y no reemplazan los valores predeterminados de Preferencias. Guardar preferencias no relacionadas conserva el estado habilitado temporal y los ángulos. Cambiar los valores predeterminados de la espacialización aplica la configuración editada; desactivar y volver a activar el efecto desde el menú Reproductor conserva los ángulos de la sesión actual.

Pausar, desplazarse y pasar de un archivo a otro conservan su configuración de espacialización. Habilitar o deshabilitar el efecto puede causar una breve interrupción mientras cambia el procesamiento de audio, y cargar un conjunto de datos grande puede tardar más. Los cambios de dirección no requieren volver a elegir el archivo.

Deje desactivado **Reproducir el audio en mono** para conservar las señales estéreo creadas por el efecto. Desactive la espacialización cuando escuche con altavoces o audio que ya es binaural. Mantenga el archivo SOFA en la ruta seleccionada: Luna no lo copia en su carpeta de configuración ni lo incluye en una copia de seguridad de la configuración. Si se mueve o se elimina, elija su nueva ubicación en Preferencias. Si el efecto no se puede aplicar, Luna informa un error y lo desactiva durante la sesión actual.

### Ecualizador

El ecualizador realza y atenúa rangos de frecuencia individuales. Elija **Reproductor > Ecualizador** y seleccione una entrada de la lista. El cambio surte efecto de inmediato y se recuerda entre sesiones.

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

Elija **Reproductor > Ecualizador > Editar el ajuste preestablecido actual** para abrir el ajuste al que está configurado el ecualizador. Todos los ajustes preestablecidos se pueden editar, incluidos los que suministra Luna.

El cuadro de diálogo contiene un grupo por banda: un filtro shelf de graves, quince bandas de 25 Hz a 16 kHz y un filtro shelf de agudos. Cada grupo contiene tres campos.

- **Frecuencia** es el centro de la banda, o el punto donde gira un filtro shelf.
- **Ganancia** es cuánto se sube o se baja la banda, en decibeles, de -20 a 20.
- **Q** es el ancho de la banda, de 0.1 a 10. Los valores más grandes son más estrechos.

Mientras el ajuste preestablecido que se edita es el que está en uso, cada cambio se aplica a medida que escribe, de modo que puede juzgarlo de oído antes de conservarlo. **Guardar** conserva el resultado; **Cancelar** restablece el ajuste preestablecido tal como estaba.

**Ajuste preestablecido base** completa todas las bandas a partir de otro ajuste preestablecido. Úselo para empezar desde algo cercano a lo que desea. **Restablecer valores predeterminados** devuelve las bandas a la forma en que se suministró el ajuste preestablecido elegido allí.

Al editar un ajuste preestablecido que suministra Luna, solo se registran los valores que usted cambió, de modo que las bandas que deje intactas siguen a Luna y las mejoras posteriores de ese ajuste preestablecido siguen llegándole. Al restablecer, se descartan esos cambios. El nombre de un ajuste preestablecido suministrado no se puede cambiar.

### Administración de ajustes preestablecidos

Elija **Reproductor > Ecualizador > Administrar ajustes preestablecidos** para ver la lista completa.

- **Nuevo** crea un ajuste preestablecido, a partir del ajuste base que elija en el editor.
- **Editar** abre el ajuste preestablecido seleccionado.
- **Eliminar** lo quita después de una confirmación.
- **Cerrar** sale de la lista.

Editar y Eliminar se aplican a sus propios ajustes preestablecidos y están ocultos mientras está seleccionado uno que suministra Luna; use **Editar el ajuste preestablecido actual** para esos. Para copiar un ajuste preestablecido, cree uno nuevo y elija el original como base.

Los ajustes preestablecidos que cree aparecen en el menú Ecualizador junto con los demás y se almacenan en `equalizer.json`, junto al archivo de configuración.

## 9. Administración de archivos locales

Los comandos de cambio de nombre, eliminación, carpeta contenedora y propiedades de Windows solo funcionan con archivos locales. No están disponibles para transmisiones de red ni para medios de YouTube.

### Mostrar el archivo en Windows

- Presione <kbd>Ctrl</kbd>+<kbd>F</kbd> o elija **Archivo > Abrir carpeta contenedora** para abrir el Explorador de archivos con el archivo actual seleccionado.
- Presione <kbd>Alt</kbd>+<kbd>Intro</kbd> o elija **Archivo > Propiedades del archivo...** para abrir el cuadro de diálogo de propiedades de Windows.

### Cambio de nombre de un archivo

Elija **Editar > Cambiar nombre...** o presione <kbd>Mayús</kbd>+<kbd>F2</kbd>. Escriba el nuevo nombre del archivo. Si omite la extensión, Luna conserva la extensión actual. Luna rechaza un nombre vacío o un nombre que ya se use en esa carpeta.

### Eliminación de un archivo

Elija **Editar > Eliminar** o presione <kbd>Mayús</kbd>+<kbd>Supr</kbd>. Confirme el aviso para eliminar de forma permanente el archivo actual del disco y quitarlo de la lista cargada. Luna no mueve el archivo a la Papelera de reciclaje.

Eliminar no es lo mismo que **Cerrar archivo**. Cerrar quita un elemento solo de Luna; eliminar quita el archivo local del disco.

### Copia de un archivo

Presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>C</kbd> para colocar el elemento actual en el portapapeles de archivos de Windows. Péguelo en el Explorador de archivos o en otro programa que acepte archivos copiados.

### Cierre de archivos

- Presione <kbd>Ctrl</kbd>+<kbd>W</kbd> para quitar el elemento actual de Luna sin eliminarlo.
- Presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>W</kbd> para quitar todos los elementos cargados.

## 10. Archivos marcados y operaciones por lotes

Las marcas le permiten reunir archivos locales mientras navega y luego operar sobre ellos en conjunto.

- Presione <kbd>Ctrl</kbd>+<kbd>K</kbd> para marcar o desmarcar el archivo actual.
- Presione <kbd>Ctrl</kbd>+<kbd>A</kbd> para marcar todos los archivos cargados o desmarcarlos todos cuando ya estén todos marcados.
- Presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>K</kbd> para borrar todas las marcas.
- Presione <kbd>K</kbd> para escuchar la cantidad de archivos marcados.

Cuando hay al menos un archivo marcado, el menú **Acciones para archivos marcados** pasa a estar disponible:

- **Copiar a la carpeta...** copia todos los archivos marcados a una carpeta elegida.
- **Mover a la carpeta...** los mueve y quita de Luna las entradas movidas correctamente.
- **Copiar al portapapeles** los coloca en el portapapeles de archivos de Windows.
- **Eliminar** solicita confirmación, elimina de forma permanente los archivos del disco sin usar la Papelera de reciclaje y quita de Luna las entradas procesadas correctamente.

Copiar y mover usan un cuadro de diálogo de progreso cancelable. Luna informa éxito total, éxito parcial, cancelación o error. Si algunos archivos fallan, los archivos procesados correctamente permanecen procesados.

## 11. Marcadores

Los marcadores guardan posiciones de reproducción con nombre para los archivos locales. Se almacenan por archivo y no están disponibles para transmisiones ni videos de YouTube.

### Agregar un marcador

1. Reproduzca o desplácese hasta la posición deseada.
2. Elija **Marcadores > Agregar un marcador nuevo** o presione <kbd>Mayús</kbd>+<kbd>M</kbd>.
3. Acepte el nombre generado basado en el tiempo o escriba un nombre descriptivo.

Los marcadores se ordenan por posición de reproducción. Los primeros diez en ese orden ocupan las ranuras expuestas por los atajos directos.

### Saltar a un marcador numerado

Presione <kbd>Alt</kbd>+<kbd>1</kbd> hasta <kbd>Alt</kbd>+<kbd>9</kbd> para las ranuras de la 1 a la 9. Presione <kbd>Alt</kbd>+<kbd>0</kbd> para la ranura 10. Luna anuncia cuando la ranura solicitada está vacía.

### Administración de marcadores

Elija **Marcadores > Administrar marcadores** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>M</kbd>. El cuadro de diálogo enumera los nombres y las posiciones de los marcadores del archivo actual.

- **Saltar** mueve la reproducción al marcador seleccionado.
- **Editar** cambia su nombre.
- **Eliminar** lo quita después de una confirmación.
- Al activar un marcador en la lista se salta directamente a él.

Use la página de Preferencias Copia de seguridad y restauración para exportar o importar toda la colección de marcadores.

## 12. YouTube

Las funciones de YouTube requieren una conexión a internet. La reproducción, la exploración de canales y las descargas también requieren los componentes de YouTube disponibles en la página de Preferencias de YouTube. La disponibilidad puede cambiar cuando YouTube modifica su servicio.

### Apertura de un enlace de YouTube

Elija **Archivo > Abrir enlace de YouTube...** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Y</kbd>. Ingrese la dirección de un video o de una lista de reproducción. Este comando no acepta direcciones de canales.

Algunas direcciones de YouTube identifican tanto un video como una lista de reproducción. Luna puede preguntar si desea reproducir el video o abrir la lista de reproducción, o seguir el comportamiento fijo seleccionado en las Preferencias de YouTube.

### Búsqueda en YouTube

Elija **Archivo > Buscar en YouTube...** o presione <kbd>Ctrl</kbd>+<kbd>Y</kbd>. Ingrese el texto de búsqueda y luego desplácese por la lista de resultados. De forma predeterminada, Luna ofrece sugerencias de búsqueda mientras escribe. Puede desactivarlas en las Preferencias de YouTube si no desea solicitudes en segundo plano mientras ingresa una consulta.

- Presione <kbd>Intro</kbd> sobre un resultado seleccionado o active **Reproducir** para reproducirlo con video.
- Presione <kbd>Ctrl</kbd>+<kbd>Intro</kbd> o active **Reproducir como audio** para reproducir solo su sonido.
- Active **Descargar** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>D</kbd> para elegir cómo guardarlo.
- Presione <kbd>Ctrl</kbd>+<kbd>D</kbd> para descargarlo de inmediato con los valores predeterminados guardados.
- Presione <kbd>Ctrl</kbd>+<kbd>Q</kbd> para agregarlo a la cola de descargas.
- Presione <kbd>Ctrl</kbd>+<kbd>Espacio</kbd> para agregarlo a Favoritos o quitarlo de allí.
- Presione <kbd>Ctrl</kbd>+<kbd>C</kbd> para copiar el enlace del resultado seleccionado.
- Presione <kbd>Ctrl</kbd>+<kbd>B</kbd> para abrirlo en el navegador predeterminado.
- Presione <kbd>Ctrl</kbd>+<kbd>N</kbd> para abrir el canal de quien lo subió en el navegador predeterminado.
- Presione <kbd>Ctrl</kbd>+<kbd>G</kbd> para explorar ese canal dentro de Luna.
- Abra el menú contextual para las mismas acciones de favoritos, copiar, navegador, canal, descarga y cola.

Cuando la selección llega al final de la página de resultados actual, Luna solicita más resultados automáticamente hasta que YouTube ya no tenga más para devolver.

Las ventanas de resultados de listas de reproducción y canales funcionan de la misma manera. Siguiente avanza por la sesión de resultados de YouTube activa, obteniendo el siguiente video cuando es necesario. Presionar <kbd>Esc</kbd> en la ventana principal después de que comienza la reproducción puede volver a la lista de resultados relacionada cuando esa sesión sigue disponible.

Los resultados de un canal se organizan en pestañas. Una pestaña se carga cuando la abre por primera vez y luego permanece disponible mientras la ventana del canal sigue abierta.

### Calidad de reproducción

Abra **Preferencias > YouTube**.

- **Calidad de video** selecciona una altura de imagen preferida para la reproducción iniciada con <kbd>Intro</kbd>.
- **Calidad de audio** selecciona una tasa de bits preferida para la reproducción solo de audio iniciada con <kbd>Ctrl</kbd>+<kbd>Intro</kbd>.

Estas son opciones de reproducción. La calidad y el formato de descarga tienen sus propios valores predeterminados en la misma página de Preferencias.

### Opciones de video

Mientras un video de YouTube está activo, el menú **Opciones de video** ofrece:

- **Descargar...** o <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>D</kbd>: elija el nombre, el destino, el formato, la calidad y las opciones de procesamiento adicionales.
- **Descarga directa** o <kbd>Ctrl</kbd>+<kbd>D</kbd>: comienza de inmediato con los valores predeterminados de las Preferencias de YouTube.
- **Agregar a la cola de descargas** o <kbd>Ctrl</kbd>+<kbd>Q</kbd>: agrega el video actual con esos valores predeterminados sin iniciar la cola.
- **Descripción del video...** o <kbd>Alt</kbd>+<kbd>D</kbd>: obtiene el título y la descripción de quien lo subió y los muestra en una ventana de texto de solo lectura.
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

Presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Q</kbd> o elija **Herramientas > Cola de descargas...** para abrir la cola. La lista muestra el título de cada video y su estado actual. Un enlace ingresado manualmente se muestra como el título cuando Luna no ha consultado sus metadatos.

- **Agregar...** acepta un enlace de video de YouTube por línea.
- **Iniciar** comienza todos los elementos de la cola y reintenta los elementos fallidos.
- **Detener** cancela la descarga activa seleccionada y la devuelve a la cola para iniciarla más tarde.
- **Detener todo** cancela todas las descargas activas y deja los elementos sin terminar en la cola.
- Presione <kbd>Supr</kbd> para quitar el elemento seleccionado después de una confirmación. Quitar un elemento activo también lo cancela.
- **Cerrar** oculta la ventana de la cola sin detener las descargas activas.

El cuadro de estado y la barra de progreso total aparecen solo mientras la cola contiene elementos. Durante un lote en ejecución, informan el elemento actual, el porcentaje total, la velocidad combinada y el tiempo estimado restante. Un elemento descargado puede permanecer en estado de finalización mientras yt-dlp convierte, combina, incrusta o mueve su salida.

La cola ejecuta hasta el límite de **Descargas simultáneas** de las Preferencias de YouTube. Las filas terminadas permanecen disponibles para su revisión hasta que se cierra la ventana de la cola, y luego se borran. Luna envía una notificación de escritorio cuando todos los elementos visibles de la cola terminan correctamente.

Si cierra Luna mientras la cola todavía tiene elementos sin terminar, Luna pregunta si desea salir y cancelarlos o volver a la aplicación.

### Videos y transmisiones favoritos

Los favoritos guardan enlaces, mientras que los marcadores guardan posiciones dentro de archivos locales. Abra los favoritos con **Archivo > Videos favoritos...** o <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>F</kbd>.

La ventana Favoritos puede abrir, agregar, editar o quitar entradas. Puede seguir usando el reproductor principal mientras está abierta. Agregar o editar una entrada deshabilita temporalmente Favoritos hasta que termine ese cuadro de diálogo, mientras que el reproductor principal sigue siendo utilizable. Cada entrada tiene un nombre, un enlace y un tipo:

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

Use **Ayuda > Actualizaciones > Actualizar componentes de YouTube** para actualizar un yt-dlp instalado. Habilite **Buscar actualizaciones de yt-dlp al iniciar** para una comprobación en segundo plano que solo muestra un aviso cuando hay una actualización disponible.

Algunos videos con restricción de edad, solo para miembros o protegidos por verificación requieren cookies del navegador. Puede elegir un archivo de cookies en formato Netscape o usar **Importar desde Firefox** para que Luna lea las cookies actuales de Firefox en cada solicitud. Estas opciones son mutuamente excluyentes, y el estado de cookies de solo lectura informa qué origen está activo. Trate los archivos de cookies exportados como datos privados de la cuenta.

## 13. Spotify

Elija **Herramientas > Spotify...** o presione <kbd>Alt</kbd>+<kbd>S</kbd> para explorar Spotify. Puede buscar canciones, artistas y listas de reproducción, abrir su biblioteca y reproducir una pista seleccionada. **Explorar Spotify en Luna Player requiere una cuenta Spotify Premium.** También necesita una conexión a internet y la autorización de Spotify.

### Configuración de su aplicación de Spotify

Las funciones de exploración de Spotify requieren el ID de cliente de su propia aplicación de desarrollador. Si no se ha configurado, al abrir Spotify se ofrece **Ir a Preferencias**.

1. Abra el [panel de desarrolladores de Spotify](https://developer.spotify.com/dashboard) e inicie sesión.
2. Cree una aplicación, ingrese su nombre y descripción, y seleccione **Web API**.
3. Agregue `http://127.0.0.1:43891/callback/` a sus **URI de redirección**. Cópiela exactamente, incluidos el puerto y la barra final.
4. Guarde la aplicación y copie su **ID de cliente**.
5. En Luna, abra **Preferencias > Spotify**, pegue el ID de cliente y elija **Aceptar**. No pegue el secreto de cliente.

Spotify establece los requisitos de cuenta y de acceso para las aplicaciones de desarrollador. Sus [requisitos actuales del modo de desarrollo](https://developer.spotify.com/documentation/web-api/concepts/quota-modes) exigen que el propietario de la aplicación tenga Spotify Premium. Si usa una cuenta diferente para iniciar sesión, esa cuenta también debe tener permitido usar la aplicación en el panel. Algunas listas de reproducción y otros contenidos pueden seguir sin estar disponibles para las aplicaciones en modo de desarrollo.

### Inicio de sesión y reproducción de una pista

Abra el explorador de Spotify y elija **Iniciar sesión en Spotify**. Su navegador web predeterminado abre la página de autorización de Spotify. Inicie sesión en la cuenta que desea explorar, apruebe el acceso y vuelva a Luna cuando el navegador confirme que puede hacerlo.

El explorador de Spotify tiene tres pestañas:

- **Inicio** contiene **Sus pistas más escuchadas**, **Reproducidas recientemente** y las mezclas o colecciones de radio disponibles de su biblioteca. No es una copia de todas las recomendaciones que muestra la aplicación propia de Spotify.
- **Búsqueda** le permite ingresar una consulta y elegir **Todo**, **Canciones**, **Artistas** o **Listas de reproducción** en **Filtrar por**. Active **Buscar**, o elija una consulta anterior de **Búsquedas recientes**.
- **Biblioteca** enumera sus listas de reproducción y **Canciones que le gustan**. Seleccione una colección y elija **Abrir selección** para ver sus pistas.

En los resultados de búsqueda, seleccione una canción para reproducirla, o abra un artista o una lista de reproducción para explorar sus pistas. En una lista de pistas, seleccione una canción y elija **Reproducir** o presione <kbd>Intro</kbd>. Se cargan más resultados o pistas a medida que llega al final de una lista. **Atrás** o <kbd>Esc</kbd> vuelve de una ventana de resultados o de pistas al explorador.

La primera reproducción puede abrir un segundo inicio de sesión en el navegador. Esto autoriza la conexión de audio por separado de la conexión de Inicio, Búsqueda y Biblioteca. Complételo y vuelva a Luna; Luna conserva la autorización guardada para sesiones posteriores. No necesita iniciar Spotify de escritorio para reproducir la pista seleccionada.

Elegir una pista lo devuelve al reproductor principal. Anterior y Siguiente se mueven por la colección seleccionada o por los resultados de canciones, cargando más pistas cuando están disponibles. Abra Spotify de nuevo con <kbd>Alt</kbd>+<kbd>S</kbd> para elegir otra cosa. Use **Actualizar** para volver a cargar su biblioteca después de cambiarla en Spotify. **Cerrar sesión** desconecta la cuenta y su autorización de reproducción y detiene una sesión activa de Spotify.

### Calidad de audio y transiciones

En **Preferencias > Spotify > Calidad de audio**, elija **Normal (96 kbps)**, **Alta (160 kbps)** o **Muy alta (320 kbps)**. Muy alta es el valor predeterminado. Cambiar la calidad durante la reproducción vuelve a cargar la pista actual en su posición actual.

Spotify admite las opciones de reproducción sin pausas y de fundido cruzado de Luna en **Preferencias > Audio > Reproducción**. Habilite **Habilitar el fundido cruzado para archivos de audio** para superponer pistas, y habilite **Habilitar el fundido cruzado durante la navegación manual** si también desea superposiciones al elegir Anterior o Siguiente. La pista siguiente debe estar lista a tiempo; una conexión lenta o una pista no disponible pueden impedir una transición continua. Consulte [Fundido cruzado](#fundido-cruzado) para ver la configuración de duración y de curva.

Las colecciones de Spotify pertenecen a Spotify. Explorarlas no las copia en **Mis listas de reproducción**, y el editor local de listas de reproducción de Luna no edita su biblioteca de Spotify.

## 14. Explorador de radio

Elija **Herramientas > Explorador de radio...** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>R</kbd> para encontrar emisoras de radio por internet a través del [directorio público de Radio Browser](https://www.radio-browser.info/). No necesita una cuenta.

### Búsqueda y reproducción de una emisora

Use el árbol **Categorías** para elegir qué explorar y luego pase a la lista **Emisoras**. Sus columnas muestran el nombre, el país, el idioma, el códec y la tasa de bits de la emisora.

- **Emisoras globales** busca en todo el directorio.
- **Favoritos** contiene las emisoras que guardó en Luna.
- **Reproducidas recientemente** contiene su historial de escucha local.
- **Descubrir** contiene las listas **Más escuchadas**, **Tendencias**, **Más votadas**, **Con clics recientes** y **Actualizadas recientemente**. Estas clasificaciones se refieren a la actividad en el directorio público, no solo a su propia escucha.
- **Países** le permite explorar por país y región. **Idiomas**, **Géneros y etiquetas** y **Formatos** ofrecen otras maneras de reducir la lista de emisoras.

Expanda una categoría para cargar sus opciones. Para buscar, ingrese un nombre o un término en **Buscar** y active el botón Buscar o presione <kbd>Intro</kbd> en el campo. Seleccione una categoría de emisoras, como Emisoras globales o un idioma en particular, antes de buscar para reducir sus emisoras. Use **Borrar búsqueda** para quitar la consulta actual. Se cargan entradas adicionales a medida que se acerca al final de una lista.

Seleccione una emisora y elija **Reproducir** o presione <kbd>Intro</kbd> en la lista de emisoras. El explorador se cierra y la emisora se reproduce en la ventana principal. Anterior y Siguiente se mueven por la lista de emisoras que seleccionó, en lugar de por archivos locales no relacionados. Vuelva a abrir el explorador de radio para seleccionar otra categoría u otra emisora.

Presione <kbd>I</kbd> en la ventana principal para escuchar el título de la emisión y <kbd>Mayús</kbd>+<kbd>I</kbd> para el artista cuando la emisora los proporciona. Una emisora en vivo puede no proporcionar duración, desplazamiento ni información completa de la pista. El nombre de la emisora y la información de la canción actual son independientes: una emisora puede seguir transmitiendo aunque no envíe artista ni título.

### Filtrado de emisoras

Elija **Filtros…** para restringir los resultados. En **Aplicar a**, elija **Esta categoría** para una restricción local o **Todas las categorías** para una global. Las restricciones de categoría se combinan con las restricciones globales, de modo que una emisora debe cumplir ambas.

Puede filtrar por un **Código de país** de dos letras, **Región**, **Idioma**, **Etiquetas** separadas por comas, **Códec**, y tasa de bits mínima o máxima. Una tasa de bits de 0 significa que no hay límite en ese extremo del rango. **Solo emisoras que funcionan** oculta las emisoras cuya última comprobación del directorio falló; está habilitada de forma predeterminada. **Solo HTTPS** excluye las transmisiones HTTP normales. Elija **Aplicar** para usar los valores mostrados, **Restablecer** para restaurar los valores predeterminados mostrados del filtro, o **Cancelar** para dejarlos sin cambios.

Si una categoría aparece vacía, borre su búsqueda y compruebe tanto los filtros de la categoría como los filtros de todas las categorías. Una emisora marcada como funcional puede estar temporalmente fuera de línea o no disponible desde su ubicación.

### Favoritos, detalles y acciones de las emisoras

Con una emisora seleccionada, presione <kbd>Ctrl</kbd>+<kbd>Espacio</kbd> para agregarla a Favoritos o quitarla de allí. También puede usar el menú contextual de la lista de emisoras. Estos favoritos se conservan en la biblioteca de radio de Luna, por separado de **Archivo > Videos favoritos...**.

Elija **Detalles de la emisora** o presione <kbd>Mayús</kbd>+<kbd>Intro</kbd> para leer la información del directorio sobre la emisora. El menú contextual también ofrece **Abrir sitio web**, **Copiar URL de la transmisión**, **Copiar nombre de la emisora** y **Votar por la emisora**. El sitio web y la URL de la transmisión son diferentes: el sitio web es la página de inicio de la emisora, mientras que la URL de la transmisión apunta a su audio.

Use **Actualizar** para volver a cargar la vista actual. **Borrar historial…** pide confirmación antes de quitar su historial local de Reproducidas recientemente; no quita los favoritos ni cambia el directorio público.

## 15. Grabación de audio

Luna puede grabar micrófonos y otras entradas, todo lo que se reproduce a través de un dispositivo de salida, o el audio de un programa en particular. Se pueden mezclar varias fuentes en una sola grabación con niveles de volumen independientes.

### Grabación rápida con el micrófono predeterminado

Si no se han creado fuentes en la sesión actual, presione <kbd>F9</kbd> para grabar el dispositivo de entrada predeterminado de Windows usando las Preferencias de grabación guardadas. Presione <kbd>F7</kbd> para pausar o reanudar, y <kbd>F8</kbd> para detener.

Un tono ascendente confirma que la grabación está comenzando. Un tono descendente confirma que se ha detenido. El inicio y la detención correctos no agregan un mensaje hablado sobre esos tonos.

### Apertura de la interfaz de grabación

Elija **Grabación > Abrir la interfaz de grabación...** o presione <kbd>Alt</kbd>+<kbd>R</kbd>. La ventana tiene una lista de fuentes, controles de fuente, configuración de salida y controles de Iniciar/Pausar. Deja utilizable el reproductor principal. Los cuadros de diálogo de fuentes y de salida pertenecen a la ventana de grabación y no bloquean los controles de reproducción de la ventana principal.

Cerrar esta ventana no detiene una grabación activa. La grabación pertenece a la aplicación y continúa hasta que use Detener, <kbd>F8</kbd>, o salga de Luna.

### Agregar fuentes

Active **Agregar** y elija:

- **Dispositivo de entrada...** para un micrófono, una entrada de línea u otro dispositivo de captura de Windows.
- **Salida del sistema...** para todo lo que se reproduce a través de un dispositivo de altavoces o auriculares seleccionado.
- **Programa...** para el audio de un solo programa. Esto requiere Windows 10 versión 2004 o posterior.

Dé a la fuente un nombre descriptivo, elija el dispositivo o el programa y establezca su volumen. Una fuente de dispositivo puede seguir el dispositivo predeterminado actual de Windows en lugar de un dispositivo con nombre.

Para una fuente de programa, **Capturar todo excepto esta aplicación** invierte la selección y captura en cambio el audio de otras aplicaciones. Solo aparecen en la lista los programas con una sesión de audio de Windows; inicie audio en el programa de destino si falta.

Use **Editar...** para cambiar la fuente seleccionada y **Quitar...** para eliminarla de la sesión. El control deslizante de volumen de la fuente cambia su nivel en la mezcla final.

Las fuentes son solo de sesión. Permanecen cuando se cierra y se vuelve a abrir la ventana de grabación durante la misma sesión de Luna, pero no se restauran después de que Luna se cierra porque los dispositivos y los identificadores de proceso pueden dejar de representar las mismas fuentes.

### Formato y destino de la grabación

Elija el formato, la frecuencia de muestreo, la cantidad de canales, la calidad donde corresponda y la carpeta de destino:

- **WAV** no tiene compresión y normalmente produce el archivo más grande.
- **FLAC** no tiene pérdida y normalmente es más pequeño que WAV.
- **MP3** y **AAC** tienen pérdida y usan la tasa de bits seleccionada para equilibrar tamaño y calidad.
- **Mono** suele ser suficiente para un micrófono o para voz.
- **Estéreo** conserva canales izquierdo y derecho separados para música y salida del sistema.

Las frecuencias de muestreo, las cantidades de canales y las tasas de bits disponibles se obtienen de los codificadores instalados con Windows. Por lo tanto, las opciones cambian según el formato seleccionado y pueden variar según el sistema. Luna asigna a cada grabación un nombre con marca de tiempo para evitar reemplazar un archivo anterior.

Los cambios realizados dentro de la interfaz de grabación se aplican a su sesión. Para cambiar los valores predeterminados usados después del próximo inicio o por los atajos directos, use **Preferencias > Grabación**.

### Inicio, pausa y detención

La ventana de grabación requiere al menos una fuente configurada para que **Iniciar** pase a estar disponible. Una vez que comienza la grabación, los controles de fuente y de salida se bloquean para que el formato de grabación y la mezcla se mantengan coherentes.

- El botón **Iniciar** pasa a ser **Detener** durante la grabación.
- El botón **Pausar** pasa a ser **Reanudar** mientras está en pausa.
- <kbd>F9</kbd>, <kbd>F7</kbd> y <kbd>F8</kbd> funcionan desde la ventana principal de Luna.
- Los atajos globales correspondientes funcionan incluso cuando otra aplicación tiene el foco.

Si solo se pueden abrir algunas fuentes, Luna comienza con las fuentes disponibles y enumera las que fallaron. Si no se puede abrir ninguna, la grabación no comienza.

Elija **Grabación > Abrir carpeta de grabaciones** para abrir el destino activo. La carpeta predeterminada es `Documents\Luna Player\Recordings`.

## 16. IPTV

Las fuentes y los canales de IPTV están en **Herramientas > IPTV**. Luna admite listas de reproducción M3U o M3U8 extendidas desde un archivo o una dirección web, cuentas de Xtream Codes y portales Stalker.

### Administración de fuentes

Elija **Herramientas > IPTV > Administrar fuentes de IPTV...** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>I</kbd>. La lista de fuentes ofrece los comandos **Abrir**, **Agregar...**, **Editar...** y **Quitar...**.

Cada fuente tiene un nombre y un tipo. Según ese tipo, Luna solicita una ruta o dirección de lista de reproducción, un servidor Xtream con nombre de usuario y contraseña, o un portal Stalker con dirección MAC y número de serie opcional. También puede indicar una dirección opcional de guía de programación XMLTV. Los canales en vivo de Xtream se pueden abrir como HLS en lugar de como transmisiones de transporte cuando **Abrir los canales en vivo como HLS** está habilitado.

Las credenciales de IPTV guardadas están protegidas para el usuario actual de Windows. No comparta el archivo de datos de fuentes de IPTV porque sigue perteneciendo a su cuenta y a su equipo.

### Exploración de canales

Abra una fuente desde el administrador, o elija **Herramientas > IPTV > Explorar canales...** para volver a abrir la fuente actual. El explorador de canales puede filtrar por TV en vivo, películas o series, por categoría y por el texto escrito en el cuadro Buscar.

Presione <kbd>Intro</kbd> sobre una fila seleccionada o active **Reproducir** para abrirla. Cuando hay información de la guía de programación disponible, presione <kbd>Ctrl</kbd>+<kbd>I</kbd> para escuchar qué se está emitiendo ahora y qué sigue. Presione <kbd>F1</kbd> en el explorador de canales para escuchar un resumen de sus controles.

## 17. Convertidor de medios

Elija **Herramientas > Convertidor de medios...** para convertir uno o más archivos multimedia a un formato de audio. Agregue archivos individuales o agregue una carpeta; al agregar una carpeta se incluyen los medios compatibles de sus subcarpetas. Las rutas duplicadas se ignoran.

El convertidor escribe AAC, AC-3, AIFF, FLAC, M4A, MKA, MP3, OGA, OGG, Opus, WAV y WMA. Elige un formato inicial razonable a partir del primer archivo de origen, pero usted puede cambiarlo. Según el formato de salida, también puede elegir una frecuencia de muestreo, canales mono o estéreo, tasa de bits, y el modo y el perfil de tasa de bits de Opus.

Establezca **Máximo de conversiones simultáneas** para controlar cuántos archivos se procesan a la vez. De forma predeterminada, los archivos convertidos se escriben junto a sus orígenes. Desmarque **Conservar las ubicaciones originales de los archivos** para enviar todos los resultados a una única carpeta de destino.

**Eliminar los archivos originales después de la conversión** quita cada origen solo después de que ese archivo se convierte correctamente. Use esta opción con cuidado; los originales eliminados no se envían a la Papelera de reciclaje.

La ventana de progreso informa el archivo actual, su porcentaje, la cantidad de archivos restantes, el porcentaje total y el tiempo estimado. Puede cancelar el trabajo restante. Cuando termina la conversión, Luna informa éxito total o enumera los archivos que fallaron y el motivo.

## 18. Temporizador de sueño

Elija **Herramientas > Temporizador de sueño > Establecer temporizador...** o presione <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>S</kbd>. Un temporizador puede activarse después de una duración elegida o al final de la pista actual. Elija si pausa la reproducción o detiene y descarga el elemento actual.

Para una duración, elija 15, 30, 45, 60, 90 o 120 minutos, o ingrese horas y minutos personalizados. Un temporizador de fin de pista requiere un elemento abierto e impide el avance automático normal cuando ese elemento termina.

Habilite **Reducir gradualmente el volumen antes de activarse** para bajar el volumen durante los últimos 1 a 120 segundos. Luna restablece el volumen original después de que el temporizador se activa o se cancela; el valor reducido no se guarda como su volumen preferido.

- Presione <kbd>Ctrl</kbd>+<kbd>T</kbd> o elija **Tiempo restante** para escuchar el tiempo restante o que el temporizador está esperando el final de la pista.
- Elija **Cancelar temporizador** para desactivarlo. Abrir la ventana del temporizador mientras hay uno activo también ofrece **Desactivar temporizador**.
- Luna anuncia una advertencia de un minuto para los temporizadores de duración.

Las últimas opciones del temporizador se recuerdan, pero una cuenta regresiva activa no sobrevive al cierre de Luna.

## 19. Referencia de Preferencias

Abra las Preferencias con **Archivo > Preferencias...** o <kbd>Ctrl</kbd>+<kbd>P</kbd>. Seleccione una página en el árbol de categorías. Elija Aceptar para guardar y aplicar los ajustes que cambió, o Cancelar para descartar los cambios no aceptados. Los ajustes sin cambios no se vuelven a aplicar, de modo que guardar una preferencia no relacionada no restablece ajustes temporales de reproducción como la espacialización para auriculares.

Presione <kbd>F1</kbd> mientras una opción tiene el foco para escuchar una explicación detallada de esa opción.

### General

| Opción | Finalidad |
| --- | --- |
| Idioma | Usa el idioma de visualización de Windows o una traducción incluida. Reinicie Luna después de cambiarlo. |
| Tema | Sigue a Windows o fuerza una apariencia clara u oscura. El alto contraste siempre sigue a Windows. Reinicie Luna después de cambiarlo. |
| Iniciar maximizada | Abre la ventana principal maximizada en el próximo inicio. Está desactivada de forma predeterminada, lo que conserva el tamaño normal de la ventana. |
| Recordar la posición del último archivo | Restaura el último archivo local activo y su posición en el próximo inicio. |
| Leer el nombre del archivo al navegar | Anuncia el elemento al que se llega con Anterior o Siguiente. |
| Buscar actualizaciones de la aplicación al iniciar | Comprueba en silencio después del inicio y solo muestra un aviso cuando hay una versión más nueva lista. |
| Guardar la configuración al cerrar | Guarda los cambios de la sesión, como el volumen y la velocidad, cuando Luna se cierra. Los cambios de Preferencias aceptados explícitamente se siguen guardando de inmediato. |
| Abrir los archivos en la ventana existente cuando Luna ya está en ejecución | Envía los archivos abiertos desde Windows a la ventana actual de Luna. Desmárquela para abrir cada solicitud en una ventana independiente. |
| Deshabilitar los controles multimedia del sistema | Quita a Luna de la superposición multimedia de Windows e ignora las teclas multimedia de hardware. |
| Mostrar la pista en reproducción en el título de la ventana | Muestra el elemento actual y si está en pausa en la barra de título de Luna. Está desactivada de forma predeterminada para evitar anuncios repetidos del lector de pantalla. |
| Interrumpir la línea actual cuando aparece un nuevo subtítulo | Permite que un nuevo subtítulo hablado corte el anterior para mantener el ritmo de la imagen. |
| Nivel de detalle | Elige los anuncios completos del nivel Principiante o los anuncios más breves del nivel Avanzado. |
| Qué desea abrir con los archivos? | Controla si un archivo abierto trae consigo ningún archivo vecino, su carpeta, o su carpeta y subcarpetas. |
| Registrar/Anular el registro de extensiones de archivo | Agrega o quita los registros de medios compatibles de Luna para el usuario actual de Windows. |

### Copia de seguridad y restauración

| Comando | Finalidad |
| --- | --- |
| Exportar configuración | Guarda las preferencias aplicadas actualmente en un archivo JSON sin mover la copia de trabajo. |
| Importar configuración | Carga un archivo JSON de configuración válido y reemplaza los valores mostrados en Preferencias. |
| Exportar marcadores | Guarda todos los marcadores en un archivo JSON. |
| Importar marcadores | Reemplaza la colección de marcadores actual por una colección exportada válida. |
| Restablecer configuración | Restaura todas las preferencias y los atajos a sus valores predeterminados después de una confirmación. |
| Abrir la carpeta de configuración del usuario | Abre el directorio de configuración de Luna en el Explorador de archivos. |

La importación reemplaza la colección correspondiente; no combina entradas. Guarde los archivos exportados en un lugar separado de la carpeta de configuración de Luna si están destinados a ser copias de seguridad.

### Audio

| Opción | Finalidad |
| --- | --- |
| Valor de desplazamiento personalizado | Cantidad de segundos usada por la cantidad de desplazamiento Personalizada; se aceptan decimales positivos como 2.5. |
| Paso de velocidad | Cantidad que suma o resta cada comando de velocidad. |
| Paso de tono | Cambio de tono por pulsación, de 0.001 a 12 semitonos. |
| Paso de volumen | Cambio de volumen por pulsación, de 1 a 20 puntos porcentuales. |
| Paso de balance | Movimiento izquierda/derecha por pulsación, de 1 a 100 puntos porcentuales. |
| Qué sucede cuando termina un archivo? | Avanza, repite o se detiene al final. |
| Volver al principio con varios archivos | Conecta el final y el comienzo de una lista de varios elementos. |
| Guardar la posición actual de cada archivo | Mantiene una posición de reanudación independiente para cada elemento local. |
| Habilitar normalización dinámica y limitador | Uniforma el volumen y contiene los picos. |
| Reproducir el audio en mono | Mezcla los canales izquierdo y derecho en mono. |

#### Valores predeterminados de la espacialización para auriculares

Esta página selecciona el conjunto de datos SOFA y el estado habilitado, la ganancia, la rotación, la elevación y los pasos de dirección predeterminados. Luna no incluye un conjunto de datos. Consulte [Espacialización para auriculares](#espacializacion-para-auriculares) en la sección de procesamiento de audio para ver las descargas directas, las instrucciones de configuración y los atajos de reproducción.

#### Fundidos y reproducción sin pausas

En **Audio > Reproducción**, las opciones de fundido de volumen controlan el comienzo y el final de los archivos, la pausa y la reanudación, y el desplazamiento. **Intervalo de fundido de volumen (milisegundos)** establece la duración normal del fundido; 0 desactiva estos fundidos. **Habilitar el fundido de volumen durante el desplazamiento** usa el **Intervalo de fundido de desplazamiento (milisegundos)** independiente, de 1 a 10000. El fundido baja el sonido alrededor de una operación; no superpone dos pistas.

**Modo de reproducción sin pausas** controla si Luna mantiene abierta la salida de audio entre pistas. **Desactivado** usa cambios de archivo normales. **Archivos compatibles** conserva el formato de audio original y une las pistas cuando sus formatos son compatibles. **Siempre que sea posible** puede adaptar las pistas posteriores al formato de salida de la primera pista para que la reproducción pueda continuar a través de esa salida. Elija Archivos compatibles si desea conservar el formato de cada pista. La reproducción en línea también requiere que la transmisión siguiente esté lista a tiempo. Consulte [Spotify](#13-spotify) para la reproducción desde una colección de Spotify.

#### Fundido cruzado

En **Audio > Reproducción**, seleccione **Habilitar el fundido cruzado para archivos de audio** para superponer el final de un archivo de audio con el comienzo del siguiente. El fundido cruzado está desactivado de forma predeterminada y excluye los archivos que contienen video. La imagen de portada de un álbum no cuenta como video.

Establezca **Duración del fundido cruzado (milisegundos)** de 1 a 30000; el valor predeterminado es 3000, o tres segundos. Elija **Potencia constante** para una sonoridad percibida uniforme durante la superposición, o **Lineal** para una tasa constante de cambio de ganancia. La potencia constante puede producir picos más altos.

Seleccione **Habilitar el fundido cruzado durante la navegación manual** para usar superposiciones cuando usted pasa de un archivo de audio a otro, incluidos los comandos Anterior, Siguiente y de número de archivo. Esta opción está desactivada de forma predeterminada. La navegación en pausa usa un cambio de archivo normal. Las mismas opciones de fundido cruzado también se aplican a las pistas de Spotify.

El fundido cruzado funciona independientemente de la opción de reproducción sin pausas y tiene prioridad sobre los fundidos normales de inicio y fin de archivo durante una superposición. Las selecciones activas, los bucles A-B, el modo de repetir archivo y los temporizadores de sueño de fin de pista impiden las transiciones automáticas. El audio en línea usa una superposición solo cuando la transmisión siguiente está lista; los archivos cortos o las transmisiones tardías pueden acortarla. Las duraciones más largas requieren más búfer de audio decodificado.

### Eliminación de silencios

Consulte [Eliminación de silencios y procesamiento de audio](#8-eliminacion-de-silencios-y-procesamiento-de-audio) para ver los controles básicos y avanzados.

### YouTube

| Opción | Finalidad |
| --- | --- |
| Calidad de video | Elige la altura de imagen preferida para la reproducción de video. |
| Calidad de audio | Elige la tasa de bits preferida para la reproducción solo de audio. |
| Cantidad de resultados de búsqueda | Establece el objetivo inicial de 5 a 100; se cargan más resultados al llegar al final. |
| Mostrar sugerencias de búsqueda al escribir | Obtiene y muestra posibles finalizaciones para una consulta de YouTube. |
| Comportamiento del enlace de video+lista de reproducción | Pregunta cada vez, siempre reproduce el video o siempre abre la lista de reproducción. |
| Carpeta de descargas | Establece el destino base, o usa `Downloads\Luna Player` cuando se deja vacía. |
| Ordenar las descargas por tipo | Coloca los archivos en las subcarpetas `videos` y `audios`. |
| Tipo de descarga predeterminado | Elige video o audio para las descargas directas y la selección inicial del cuadro de diálogo Descargar. |
| Calidad y formato de descarga de video | Establece el límite de altura y el contenedor MP4 o MKV usados de forma predeterminada. |
| Calidad y formato de descarga de audio | Establece la tasa de bits y el formato MP3, M4A, Opus, FLAC o WAV usados de forma predeterminada. |
| Fragmentos simultáneos | Establece cuántas partes de un archivo obtiene yt-dlp a la vez, de 1 a 16. |
| Descargas simultáneas | Establece cuántos elementos de la cola pueden ejecutarse a la vez, de 1 a 16. |
| Incrustar metadatos, miniatura, capítulos o subtítulos | Agrega la información seleccionada a los archivos descargados donde el formato de salida lo admita. |
| Eliminar segmentos de SponsorBlock | Corta de las descargas las secciones compatibles de patrocinio, introducción y similares. |
| Archivo de cookies / Importar desde Firefox | Proporciona cookies de cuenta para los videos que las requieren. Solo puede haber un origen de cookies activo. |
| Canal de actualización de yt-dlp | Elige Stable, Nightly o Master. |
| Buscar actualizaciones de yt-dlp al iniciar | Realiza una comprobación silenciosa del componente después del inicio. |
| Descargar componentes de YouTube | Instala o actualiza yt-dlp y las herramientas de apoyo. |

### Spotify

Aquí establece el **ID de cliente** de su aplicación de desarrollador y la **Calidad de audio** preferida. La página incluye el URI de redirección exacto que debe registrar y las instrucciones para obtener el ID de cliente. Consulte [Spotify](#13-spotify) para la configuración de la cuenta, el inicio de sesión, la exploración y la reproducción.

### Discord Rich Presence

Rich Presence permite que las personas que ven su perfil de Discord vean lo que está reproduciendo en Luna. Compartir está desactivado de forma predeterminada. Usted elige si incluir detalles de los medios, imágenes y enlaces.

#### Habilitación del uso compartido de la actividad

1. Abra **Discord de escritorio** e inicie sesión. Vaya a **Configuración de usuario > Privacidad de la actividad** y active **Compartir con otras personas las actividades detectadas**.
2. En Luna, abra **Preferencias > Discord**, seleccione **Habilitar Discord Rich Presence** y elija **Aceptar**.

Discord debe estar en ejecución en el mismo equipo. Su versión de navegador no puede recibir la actividad de Luna. Desactivar la opción borra la actividad sin interrumpir la reproducción.

#### Qué aparece en su perfil

La línea de actividad identifica el origen, por ejemplo **Escuchando música en Luna**, **Escuchando YouTube en Luna**, **Escuchando radio en Luna** o **Escuchando Spotify en Luna**. Las tarjetas de escucha usan **Luna Player** como encabezado. Debajo, Luna muestra el título del medio y un crédito de artista independiente cuando esa información está disponible. La música local usa el título almacenado en el archivo; si no tiene título, Luna usa el nombre del archivo sin su extensión. La radio muestra el nombre de la emisora y el artista actual de la emisión, cuando la emisora lo proporciona.

Con el modo audiolibro habilitado, los archivos que contienen capítulos aparecen como **Escuchando un audiolibro en Luna**. Los archivos sin capítulos usan la actividad de música normal. La línea de título de un audiolibro muestra el título del libro y la línea de abajo muestra el capítulo actual en lugar de un crédito de artista. El título del libro proviene de los metadatos de su álbum o título, y se usa el nombre del archivo cuando no hay ninguno de los dos.

Pausar cambia el encabezado a **[En pausa]: Luna Player**. El título y el artista o el capítulo permanecen en sus propias líneas, sin otra etiqueta de pausa después de ellos. La frase del origen sigue siendo la misma mientras está en pausa.

YouTube usa la miniatura del video. Spotify usa la portada del álbum, y la radio usa la imagen de la emisora, cuando están disponibles. Los archivos locales y los libros usan el icono de la aplicación de Luna. Las etiquetas de las imágenes contienen el nombre del artista o del canal sin un prefijo "Por" añadido ni la versión de la aplicación. Las tarjetas de escucha muestran un crédito independiente **Por artista** debajo del título cuando hay un artista disponible. El video de YouTube usa **Viendo YouTube en Luna**, mientras que la reproducción solo de audio usa **Escuchando YouTube en Luna**. La reproducción de video conserva su miniatura y el botón **Ver en YouTube**. La tarjeta Viendo de YouTube usa **Luna Player** como encabezado, que cambia a **[En pausa]: Luna Player** cuando está en pausa. El título del video aparece debajo del encabezado. La etiqueta de la imagen contiene el nombre simple del artista o del canal cuando se conoce. Discord puede acortar los títulos largos.

Para los medios con una duración conocida, Luna proporciona ambas marcas de tiempo de progreso usando el mismo tiempo transcurrido y total del medio que el reproductor, sin dividir ninguno por la velocidad de reproducción. La tarjeta de escucha de Discord muestra el tiempo transcurrido y el total, incluso cuando el modo audiolibro está habilitado. Los medios en pausa permanecen visibles con una etiqueta **En pausa** y sin temporizador en marcha. Reanudar, desplazarse, cambiar la velocidad o pasar a otro capítulo actualiza el temporizador. Las transmisiones en vivo muestran el tiempo transcurrido cuando no hay una duración conocida.

#### Controles de uso compartido

| Opción | Finalidad |
| --- | --- |
| Habilitar Discord Rich Presence | Activa o desactiva el uso compartido. |
| Compartir títulos, artistas e imágenes de los medios | Incluye títulos, créditos de artista, capítulos de libros e imágenes en línea. Desmárquela para mostrar un estado de reproducción genérico y ocultar los enlaces de origen. |
| Mostrar botones de enlace | Agrega **Descargar Luna Player** y, cuando se comparten los detalles de los medios, un botón de origen para YouTube, Spotify o una transmisión de radio pública. |
| Mostrar la actividad cuando está inactivo | Muestra **Inactivo** cuando la reproducción está detenida o no hay ningún medio cargado. Desmárquela para quitar la actividad mientras está inactivo. Pausar no cuenta como inactivo. |

Para la radio, **Ver en línea** abre la propia transmisión en el navegador de quien la ve. Algunos navegadores pueden descargar o rechazar una transmisión que no pueden reproducir. Luna omite las direcciones privadas, los enlaces que contienen credenciales y los enlaces con parámetros de consulta no reconocidos. Las rutas de archivos locales nunca se usan como botones. Desmarcar **Mostrar botones de enlace** quita ambos botones sin ocultar la imagen.

#### Si falta la actividad

Compruebe que estén habilitados tanto el uso compartido de actividad de Discord como la opción Rich Presence de Luna. Luna muestrea la reproducción una vez por segundo y envía los cambios de estado sin un retraso fijo de cuatro segundos. Los cambios rápidos se combinan cuando se alcanza el límite de envío de Discord. Luna se vuelve a conectar cuando Discord se inicia o se reinicia; espere unos 15 segundos. Si Discord no está disponible, la reproducción continúa con normalidad.

Discord puede ocultar sus propios botones de actividad cuando usted ve su perfil. Otra persona que vea el perfil puede ver los botones.

### Grabación

Esta página establece los valores predeterminados usados por los atajos de grabación directa y alimenta la interfaz de grabación al comienzo de cada sesión de Luna. Contiene el formato de audio, la frecuencia de muestreo, los canales, la calidad de audio para los formatos comprimidos y la carpeta de grabaciones.

### Atajos de teclado

Esta página enumera todas las acciones que se pueden ejecutar mientras la ventana principal de Luna tiene el foco. Cada acción puede tener un atajo principal; algunas acciones también tienen una ranura secundaria predefinida.

1. Seleccione una acción.
2. Active **Editar atajo principal** o, donde esté disponible, **Editar atajo secundario**.
3. Presione la combinación deseada. Presione <kbd>Esc</kbd> para cancelar la captura.
4. Presione Aceptar en Preferencias para aplicar los cambios.

Inmediatamente después de la lista de atajos, **Deshabilitar los atajos secundarios** desactiva todas las asignaciones de la columna Atajo secundario. Las teclas conservan entonces su comportamiento normal de la interfaz; por ejemplo, Tab y Mayús+Tab pueden mover el foco en lugar de cambiar de pista. Los atajos principales y los globales no se ven afectados. Sus asignaciones secundarias se conservan y vuelven a estar activas cuando desmarca la casilla y elige Aceptar. Re Pág y Av Pág son los atajos principales de Anterior y Siguiente.

Luna rechaza una combinación ya asignada a otra acción del mismo conjunto de atajos. Los atajos locales pueden usar teclas imprimibles, teclas de navegación, teclas de función de F1 a F24 y los modificadores Ctrl, Mayús o Alt. Use **Restablecer valores predeterminados** para restaurar toda la página de atajos locales después de una confirmación.

### Atajos globales

Los atajos globales funcionan mientras otra aplicación tiene el foco, ya sea que Luna esté minimizada o simplemente detrás de otra ventana después de Alt+Tab. No se ejecutan mientras Luna o uno de sus cuadros de diálogo tiene el foco; use allí los atajos locales. Los comandos globales se limitan a acciones de reproducción, navegación, volumen y grabación que resultan útiles fuera de Luna.

Seleccione una acción, active **Editar atajo** y presione la combinación deseada. Los atajos globales también pueden usar la tecla de Windows. Luna transmite estas combinaciones a Windows, de modo que un atajo del sistema operativo con las mismas teclas puede seguir realizando su propia acción. Elija una combinación diferente si esto causa un efecto secundario no deseado.

Los atajos globales son independientes de los atajos locales. Cambiar uno no cambia el otro.

## 20. Actualizaciones de la aplicación

### Comprobación manual

Elija **Ayuda > Actualizaciones > Buscar actualizaciones de la aplicación**. Aparece un cuadro de diálogo de progreso cancelable mientras Luna consulta la información de la versión.

Si hay una versión más nueva lista, Luna muestra las versiones instalada y disponible y una lista desplazable de cambios. Elija **Actualizar** para descargarla o **Más tarde** para dejar sin cambios la versión actual.

Luna verifica que el instalador o el archivo portátil real exista antes de ofrecer la actualización. Si la información de la versión se publicó antes de que su paquete terminara de subirse, una comprobación manual le pide que lo intente de nuevo más tarde en lugar de presentar una descarga que fallará.

### Comprobaciones al iniciar

Habilite **Buscar actualizaciones de la aplicación al iniciar** para una comprobación silenciosa en segundo plano. No muestra ninguna ventana de progreso ni mensaje de que no hay actualizaciones; solo aparece un aviso cuando hay un paquete compatible más nuevo disponible.

### Instalación de una actualización descargada

La ventana de descarga muestra el tamaño total, el tamaño descargado, el porcentaje y un botón Cancelar. Deja utilizable el reproductor principal, de modo que puede pausar, detener o cambiar la reproducción mientras espera. Después de una descarga correcta, Luna inicia su pequeño asistente de actualización y se cierra:

- Una copia instalada inicia el instalador de la actualización. Su progreso de instalación permanece visible y Luna se abre cuando termina la instalación.
- Una copia portátil reemplaza los archivos extraídos de la aplicación y abre el reproductor actualizado.

No inicie otro proceso de Luna mientras se aplica la actualización.

## 21. Ayuda e información de versiones

- **Ayuda > Guía del usuario** o <kbd>F1</kbd> abre la guía instalada que coincide con el idioma de la interfaz de Luna. Si no hay instalada una guía en el idioma regional o base correspondiente, Luna abre la guía en inglés.
- **Ayuda > Acerca de** muestra una descripción, la versión, la información de derechos de autor y un enlace al sitio web del proyecto.
- **Ayuda > Notas de la versión** abre la página de versiones de GitHub para la versión instalada en su navegador predeterminado.

La guía del usuario se instala como HTML local y no requiere una conexión a internet. Las notas de la versión y el sitio web del proyecto sí la requieren.

## 22. Configuración y datos del usuario

Luna almacena los datos del usuario en `%APPDATA%\Luna Player`:

| Archivo | Contenido |
| --- | --- |
| `settings.json` | Preferencias y personalizaciones de atajos |
| `bookmarks.json` | Marcadores con nombre de archivos locales |
| `positions.json` | Posiciones de reproducción por archivo |
| `equalizer.json` | Ajustes preestablecidos creados por el usuario y cambios en los ajustes preestablecidos suministrados del ecualizador |
| `favorites.json` | Enlaces favoritos de YouTube y de transmisiones |
| `iptv-sources.json` | Fuentes de IPTV guardadas, con los campos de cuenta protegidos para el usuario actual de Windows |
| `recents.json` | Archivos, carpetas y listas de reproducción abiertos recientemente |
| `Playlists/` | Listas de reproducción JSON con nombre, incluidas las descripciones y las referencias de carpetas |
| `radio.json` | Emisoras de radio favoritas e historial local de escucha de radio |
| `spotify-preview-searches.txt` | Consultas de búsqueda recientes de Spotify |
| `spotify-token.dat` | Autorización de exploración de Spotify, protegida para el usuario actual de Windows |
| `spotify-playback-credentials.dat` | Autorización de reproducción de Spotify, protegida para el usuario actual de Windows |

Use **Preferencias > Copia de seguridad y restauración > Abrir la carpeta de configuración del usuario** para abrir la carpeta exacta en lugar de escribir la ruta.

No edite estos archivos mientras Luna se está ejecutando. Use los controles de exportación e importación de la configuración y los marcadores siempre que sea posible.

Si `settings.json` no se puede leer o contiene valores no válidos, Luna se inicia con los valores predeterminados, muestra el motivo y protege el archivo existente para que no se sobrescriba. Importe una copia de seguridad de configuración válida o use **Restablecer configuración** para reemplazarlo deliberadamente. Los marcadores, favoritos, ajustes preestablecidos del ecualizador, fuentes de IPTV y recientes no válidos también se informan en lugar de reemplazarse silenciosamente.

## 23. Referencia completa de atajos predeterminados

Estos son los valores de fábrica. Es posible que sus asignaciones actuales difieran después de la personalización. La lista autorizada para su instalación está en **Preferencias > Atajos de teclado** y **Atajos globales**.

### Archivos, información y Preferencias

| Acción | Atajo principal | Atajo secundario |
| --- | --- | --- |
| Abrir archivos | <kbd>Ctrl</kbd>+<kbd>O</kbd> | — |
| Abrir un disco DVD | Ninguno | — |
| Agregar archivos a la lista actual | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>F</kbd> | — |
| Agregar una carpeta a la lista actual | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>D</kbd> | — |
| Abrir un enlace de transmisión de red | <kbd>Ctrl</kbd>+<kbd>L</kbd> | — |
| Abrir todos los archivos de una carpeta | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>O</kbd> | — |
| Mostrar el archivo actual en el Explorador de archivos | <kbd>Ctrl</kbd>+<kbd>F</kbd> | — |
| Propiedades del archivo de Windows | <kbd>Alt</kbd>+<kbd>Intro</kbd> | — |
| Cuadro de diálogo Archivos abiertos | <kbd>F2</kbd> | — |
| Cerrar el archivo actual | <kbd>Ctrl</kbd>+<kbd>W</kbd> | — |
| Cerrar todos los archivos | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>W</kbd> | — |
| Preferencias | <kbd>Ctrl</kbd>+<kbd>P</kbd> | — |
| Leer la información del archivo actual | <kbd>F</kbd> | — |
| Leer el título del medio actual | <kbd>I</kbd> | — |
| Salir de Luna | Ninguno | — |

### Navegación por la lista de reproducción y modos

| Acción | Atajo principal | Atajo secundario |
| --- | --- | --- |
| Archivo anterior | <kbd>Re Pág</kbd> | <kbd>Mayús</kbd>+<kbd>Tab</kbd> |
| Archivo siguiente | <kbd>Av Pág</kbd> | <kbd>Tab</kbd> |
| Primer archivo | <kbd>Ctrl</kbd>+<kbd>Inicio</kbd> | — |
| Ir al número de archivo | <kbd>Ctrl</kbd>+<kbd>G</kbd> | — |
| Último archivo | <kbd>Ctrl</kbd>+<kbd>Fin</kbd> | — |
| Activar o desactivar la reproducción aleatoria | <kbd>Ctrl</kbd>+<kbd>Z</kbd> | — |
| Activar o desactivar la repetición del archivo | <kbd>Ctrl</kbd>+<kbd>R</kbd> | — |

### Reproducción y desplazamiento

| Acción | Atajo principal | Atajo secundario |
| --- | --- | --- |
| Reproducir o pausar | <kbd>Espacio</kbd> | <kbd>Intro</kbd> |
| Retroceder un paso | <kbd>Flecha izquierda</kbd> | — |
| Avanzar un paso | <kbd>Flecha derecha</kbd> | — |
| Retroceder dos pasos | <kbd>Mayús</kbd>+<kbd>Flecha izquierda</kbd> | — |
| Avanzar dos pasos | <kbd>Mayús</kbd>+<kbd>Flecha derecha</kbd> | — |
| Principio | <kbd>Inicio</kbd> | — |
| Final | <kbd>Fin</kbd> | — |
| Ir al tiempo | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>G</kbd> | — |
| Elegir tarjeta de sonido | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>A</kbd> | — |
| Entrar en pantalla completa o salir de ella | <kbd>F11</kbd> | — |
| Abrir la lista de pistas de audio | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>T</kbd> | — |
| Capítulo anterior | <kbd>Ctrl</kbd>+<kbd>Re Pág</kbd> | — |
| Capítulo siguiente | <kbd>Ctrl</kbd>+<kbd>Av Pág</kbd> | — |
| Leer el capítulo actual | <kbd>C</kbd> | — |
| Modo audiolibro | Ninguno | — |
| Detener al final del capítulo actual | Ninguno | — |
| Pista de audio anterior | <kbd>Mayús</kbd>+<kbd>Re Pág</kbd> | — |
| Pista de audio siguiente | <kbd>Mayús</kbd>+<kbd>Av Pág</kbd> | — |
| Activar o desactivar la lectura de subtítulos | <kbd>Ctrl</kbd>+<kbd>U</kbd> | — |
| Cargar subtítulo desde un archivo | <kbd>Ctrl</kbd>+<kbd>J</kbd> | — |
| Cargar subtítulo desde una dirección web | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>J</kbd> | — |
| Recordar el subtítulo durante esta sesión | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>U</kbd> | — |

### Selección de la cantidad de desplazamiento

| Acción | Atajo |
| --- | --- |
| Seleccionar paso de 1 segundo | <kbd>Mayús</kbd>+<kbd>1</kbd> |
| Seleccionar paso de 5 segundos | <kbd>Mayús</kbd>+<kbd>2</kbd> |
| Seleccionar paso de 10 segundos | <kbd>Mayús</kbd>+<kbd>3</kbd> |
| Seleccionar paso de 20 segundos | <kbd>Mayús</kbd>+<kbd>4</kbd> |
| Seleccionar paso de 30 segundos | <kbd>Mayús</kbd>+<kbd>5</kbd> |
| Seleccionar paso de 1 minuto | <kbd>Mayús</kbd>+<kbd>6</kbd> |
| Seleccionar paso de 2 minutos | <kbd>Mayús</kbd>+<kbd>7</kbd> |
| Seleccionar paso de 3 minutos | <kbd>Mayús</kbd>+<kbd>8</kbd> |
| Seleccionar paso de 5 minutos | <kbd>Mayús</kbd>+<kbd>9</kbd> |
| Seleccionar paso de 10 minutos | <kbd>Mayús</kbd>+<kbd>0</kbd> |
| Seleccionar paso personalizado | <kbd>Mayús</kbd>+<kbd>-</kbd> |

### Saltos por porcentaje

| Posición | Atajo | Posición | Atajo |
| --- | --- | --- | --- |
| 10% | <kbd>Ctrl</kbd>+<kbd>1</kbd> | 15% | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>1</kbd> |
| 20% | <kbd>Ctrl</kbd>+<kbd>2</kbd> | 25% | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>2</kbd> |
| 30% | <kbd>Ctrl</kbd>+<kbd>3</kbd> | 35% | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>3</kbd> |
| 40% | <kbd>Ctrl</kbd>+<kbd>4</kbd> | 45% | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>4</kbd> |
| 50% | <kbd>Ctrl</kbd>+<kbd>5</kbd> | 55% | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>5</kbd> |
| 60% | <kbd>Ctrl</kbd>+<kbd>6</kbd> | 65% | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>6</kbd> |
| 70% | <kbd>Ctrl</kbd>+<kbd>7</kbd> | 75% | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>7</kbd> |
| 80% | <kbd>Ctrl</kbd>+<kbd>8</kbd> | 85% | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>8</kbd> |
| 90% | <kbd>Ctrl</kbd>+<kbd>9</kbd> | 95% | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>9</kbd> |
| 100% | <kbd>Ctrl</kbd>+<kbd>0</kbd> | 100% alternativo | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>0</kbd> |

### Volumen y estado hablado

| Acción | Atajo |
| --- | --- |
| Subir el volumen | <kbd>Flecha arriba</kbd> |
| Bajar el volumen | <kbd>Flecha abajo</kbd> |
| Establecer el volumen al máximo | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Flecha arriba</kbd> |
| Establecer el volumen al mínimo | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Flecha abajo</kbd> |
| Leer el volumen | <kbd>V</kbd> |
| Leer el tiempo transcurrido | <kbd>E</kbd> |
| Leer el tiempo restante | <kbd>R</kbd> |
| Leer la duración total | <kbd>T</kbd> |
| Leer el porcentaje de posición | <kbd>P</kbd> |
| Leer la velocidad de reproducción | <kbd>S</kbd> |
| Cambiar entre el nivel de detalle Principiante/Avanzado | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>V</kbd> |

### Velocidad, tono, balance, bucles y filtros

| Acción | Atajo |
| --- | --- |
| Aumentar la velocidad de reproducción | <kbd>Ctrl</kbd>+<kbd>Flecha arriba</kbd> |
| Disminuir la velocidad de reproducción | <kbd>Ctrl</kbd>+<kbd>Flecha abajo</kbd> |
| Restablecer la velocidad de reproducción | <kbd>Alt</kbd>+<kbd>Y</kbd> |
| Subir el tono | <kbd>Mayús</kbd>+<kbd>Flecha arriba</kbd> |
| Bajar el tono | <kbd>Mayús</kbd>+<kbd>Flecha abajo</kbd> |
| Restablecer el tono | <kbd>Alt</kbd>+<kbd>Mayús</kbd>+<kbd>P</kbd> |
| Leer el tono | <kbd>Mayús</kbd>+<kbd>P</kbd> |
| Balance a la izquierda | <kbd>Ctrl</kbd>+<kbd>Flecha izquierda</kbd> |
| Balance a la derecha | <kbd>Ctrl</kbd>+<kbd>Flecha derecha</kbd> |
| Leer el balance | <kbd>B</kbd> |
| Activar o desactivar la espacialización para auriculares | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>H</kbd> |
| Girar la espacialización para auriculares a la izquierda | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Flecha izquierda</kbd> |
| Girar la espacialización para auriculares a la derecha | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Flecha derecha</kbd> |
| Subir la elevación de la espacialización para auriculares | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Flecha arriba</kbd> |
| Bajar la elevación de la espacialización para auriculares | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Flecha abajo</kbd> |
| Activar o desactivar la eliminación de silencios | <kbd>Ctrl</kbd>+<kbd>M</kbd> |
| Establecer el inicio del bucle A-B | <kbd>[</kbd> |
| Establecer el final del bucle A-B | <kbd>]</kbd> |
| Borrar el bucle A-B | <kbd>Retroceso</kbd> |

### Edición y archivos marcados

| Acción | Atajo |
| --- | --- |
| Cambiar el nombre del archivo actual | <kbd>Mayús</kbd>+<kbd>F2</kbd> |
| Eliminar el archivo actual | <kbd>Mayús</kbd>+<kbd>Supr</kbd> |
| Copiar el archivo actual al portapapeles | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>C</kbd> |
| Pegar/abrir un archivo o carpeta desde el portapapeles | <kbd>Ctrl</kbd>+<kbd>V</kbd> |
| Marcar o desmarcar el archivo actual | <kbd>Ctrl</kbd>+<kbd>K</kbd> |
| Marcar o desmarcar todos los archivos | <kbd>Ctrl</kbd>+<kbd>A</kbd> |
| Borrar todas las marcas | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>K</kbd> |
| Leer la cantidad de archivos marcados | <kbd>K</kbd> |
| Copiar los archivos marcados a una carpeta | Ninguno |
| Mover los archivos marcados a una carpeta | Ninguno |
| Copiar los archivos marcados al portapapeles | Ninguno |
| Eliminar los archivos marcados | Ninguno |

### Marcadores

| Acción | Atajo |
| --- | --- |
| Agregar marcador | <kbd>Mayús</kbd>+<kbd>M</kbd> |
| Administrar marcadores | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>M</kbd> |
| Saltar a las ranuras de marcador 1–9 | <kbd>Alt</kbd>+<kbd>1</kbd> hasta <kbd>Alt</kbd>+<kbd>9</kbd> |
| Saltar a la ranura de marcador 10 | <kbd>Alt</kbd>+<kbd>0</kbd> |

### Servicios en línea, herramientas, grabación, actualizaciones y ayuda

| Acción | Atajo |
| --- | --- |
| Abrir enlace de YouTube | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Y</kbd> |
| Buscar en YouTube | <kbd>Ctrl</kbd>+<kbd>Y</kbd> |
| Videos favoritos | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>F</kbd> |
| Abrir la cola de descargas | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>Q</kbd> |
| Agregar el video actual de YouTube a la cola | <kbd>Ctrl</kbd>+<kbd>Q</kbd> |
| Descargar directamente el video actual de YouTube | <kbd>Ctrl</kbd>+<kbd>D</kbd> |
| Elegir las opciones de descarga del video actual de YouTube | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>D</kbd> |
| Mostrar la descripción del video actual | <kbd>Alt</kbd>+<kbd>D</kbd> |
| Copiar el enlace del video actual | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>C</kbd> |
| Abrir el video actual en el navegador | Ninguno |
| Abrir el canal del video actual en el navegador | Ninguno |
| Explorar el canal del video actual en Luna | Ninguno |
| Abrir el explorador de radio | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>R</kbd> |
| Abrir el explorador de Spotify | <kbd>Alt</kbd>+<kbd>S</kbd> |
| Preferencias de Spotify | Ninguno |
| Abrir el convertidor de medios | Ninguno |
| Administrar fuentes de IPTV | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>I</kbd> |
| Explorar canales de IPTV | Ninguno |
| Establecer el temporizador de sueño | <kbd>Ctrl</kbd>+<kbd>Mayús</kbd>+<kbd>S</kbd> |
| Cancelar el temporizador de sueño | Ninguno |
| Leer el tiempo restante del temporizador de sueño | <kbd>Ctrl</kbd>+<kbd>T</kbd> |
| Abrir la interfaz de grabación | <kbd>Alt</kbd>+<kbd>R</kbd> |
| Iniciar la grabación | <kbd>F9</kbd> |
| Pausar o reanudar la grabación | <kbd>F7</kbd> |
| Detener la grabación | <kbd>F8</kbd> |
| Abrir la carpeta de grabaciones | Ninguno |
| Abrir la guía del usuario | <kbd>F1</kbd> |
| Acerca de Luna | Ninguno |
| Abrir las notas de la versión | Ninguno |
| Buscar actualizaciones de la aplicación | Ninguno |
| Actualizar componentes de YouTube | Ninguno |

### Atajos globales predeterminados

Estos comandos funcionan mientras otra aplicación tiene el foco, incluso cuando Luna está minimizada. Se ignoran mientras Luna o uno de sus cuadros de diálogo tiene el foco.

| Acción | Atajo global |
| --- | --- |
| Reproducir o pausar | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Espacio</kbd> |
| Retroceder un paso | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Flecha izquierda</kbd> |
| Avanzar un paso | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Flecha derecha</kbd> |
| Subir el volumen | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Flecha arriba</kbd> |
| Bajar el volumen | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Flecha abajo</kbd> |
| Archivo anterior | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Re Pág</kbd> |
| Archivo siguiente | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Av Pág</kbd> |
| Iniciar la grabación | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>F9</kbd> |
| Pausar o reanudar la grabación | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>F7</kbd> |
| Detener la grabación | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>F8</kbd> |

## 24. Formatos compatibles

Luna filtra la selección de archivos y carpetas usando las siguientes extensiones. La reproducción la gestiona mpv, por lo que otros formatos admitidos por ese motor también pueden reproducirse cuando se abren directamente.

**Audio:** AAC, AC-3, AIFF, ALAC, APE, AU, DTS, E-AC-3, FLAC, M4A, M4B, MKA, MP1, MP2, MP3, MPC, OGA, OGG, OGM, Opus, TAK, TrueHD (`.thd`), TTA, WAV, WMA y WavPack.

**Video:** 3G2, 3GP, AVI, FLV, IVF, M2TS, M4V, MJ2, MKV, MOV, MP4, MPEG, MPG, MXF, OGV, RMVB, TS, WebM, WMV y Y4M.

**Listas de reproducción:** M3U y M3U8, incluidas las transmisiones HLS, y listas de reproducción JSON locales de Luna con descripciones y referencias de carpetas.

**Discos:** DVD-Video y DVD-Audio mediante **Archivo > Abrir DVD...**, la reproducción automática de DVD o la acción de reproducir DVD del Explorador. Los menús de disco, los discos Blu-ray y los CD de audio no son compatibles con esta función de apertura de discos.

## 25. Solución de problemas

### Un archivo o una carpeta abre menos elementos de los esperados

Revise **Preferencias > General > Qué desea abrir con los archivos?** El valor predeterminado abre solo el archivo seleccionado. Los comandos de carpeta incluyen solo las extensiones reconocidas como medios. Las carpetas inaccesibles y las carpetas del sistema se omiten durante los análisis recursivos.

### Una transmisión en vivo no tiene duración ni porcentaje

Las transmisiones en vivo a menudo no publican una duración fija. Ir al tiempo, los saltos por porcentaje, el tiempo restante y el desplazamiento hasta el final requieren una duración conocida y pueden no estar disponibles.

### Un video de YouTube no se abre

Pruebe estos pasos en orden:

1. Confirme que la dirección se abre en un navegador y que nombra un video o una lista de reproducción en lugar de un canal.
2. Elija **Ayuda > Actualizaciones > Actualizar componentes de YouTube**.
3. Si los componentes no se han instalado, elija **Preferencias > YouTube > Descargar componentes de YouTube**.
4. Pruebe primero el canal de actualización Stable; use Nightly o Master solo si Stable no puede manejar un cambio reciente del servicio.
5. Para un video con restricción de edad, solo para miembros o protegido por verificación, configure un archivo de cookies o las cookies de Firefox en las Preferencias de YouTube.

### Una descarga de YouTube termina de descargar pero sigue trabajando

Después de que yt-dlp recibe todos los datos multimedia, puede que todavía necesite combinar video y audio, convertir el resultado, incrustar información, procesar capítulos o mover el archivo terminado. El título y el texto de la ventana de progreso nombran este trabajo. No cancele solo porque el porcentaje haya llegado al 100%.

### Una descarga continúa después de que se cierra su ventana de progreso

Elegir **Minimizar a la cola** transfiere deliberadamente el progreso visible a **Herramientas > Cola de descargas...**. Cerrar la ventana de la cola también deja el trabajo activo en ejecución. Detenga el elemento desde la cola si ya no lo desea, o cierre Luna y confirme que las descargas sin terminar deben cancelarse.

### IPTV no tiene información de programación

La información de programación aparece solo cuando el proveedor la suministra o la fuente tiene una dirección de EPG XMLTV que coincide. Los identificadores de canal de la guía deben coincidir con los identificadores de la fuente. La reproducción puede seguir funcionando cuando no hay guía de programación disponible.

### Spotify no inicia sesión o no carga una colección

Compruebe el ID de cliente en **Preferencias > Spotify** y el URI de redirección en su aplicación de desarrollador. El URI debe ser exactamente `http://127.0.0.1:43891/callback/`, incluida la barra final. Siga las dos autorizaciones del navegador si Luna las solicita: la exploración y la reproducción de audio inician sesión por separado. Si una autorización ha caducado, inicie sesión de nuevo.

Si Spotify rechaza una lista de reproducción o una colección, compruebe que su cuenta puede acceder a ella y tiene permitido usar su aplicación de desarrollador. Las aplicaciones en modo de desarrollo no pueden acceder a todos los tipos de contenido de Spotify. Si Luna informa un límite de solicitudes, espere el tiempo indicado antes de reintentar; actualizar repetidamente no elimina el límite. Una colección que no se puede cargar porque Spotify la restringe no puede ponerse a disposición cambiando la calidad de audio de Luna.

### Una emisora de radio no se reproduce o no tiene información de pista

Pruebe **Actualizar** en el explorador de radio y seleccione otra emisora. La última comprobación correcta del directorio público no garantiza que una transmisión esté disponible ahora o en su región. Si no aparece ninguna emisora, borre la búsqueda e inspeccione tanto los filtros de categoría como los globales. **Solo emisoras que funcionan** y **Solo HTTPS** pueden excluir emisoras que esperaba encontrar.

No todas las emisoras envían títulos de canciones o artistas. La información de emisión ausente o sin cambios no significa necesariamente que la reproducción se haya detenido. Use **Detalles de la emisora** para inspeccionar la entrada del directorio, o **Abrir sitio web** en el menú contextual de la emisora para consultar el sitio de la propia emisora.

### La espacialización para auriculares no puede abrir un conjunto de datos

Descargue un conjunto de datos `.sofa` desde los enlaces de [Espacialización para auriculares](#espacializacion-para-auriculares) y luego seleccione el archivo en sí en Preferencias. Una página HTML guardada, un archivo comprimido o un archivo de audio común no es un conjunto de datos HRTF. Mantenga el archivo seleccionado en su lugar; las copias de seguridad de la configuración no lo contienen. Si no se puede abrir un conjunto de datos válido desde una ruta que contiene caracteres que no son ASCII, pruebe con una carpeta y un nombre de archivo que usen letras inglesas simples y selecciónelo de nuevo.

Para comparar direcciones, escuche con auriculares, desactive **Reproducir el audio en mono** y comience con una elevación cercana a 0 grados. Reduzca la ganancia si el sonido se distorsiona. Si las señales de adelante/atrás o de altura no son convincentes, pruebe otro conjunto de datos en lugar de suponer que un archivo más grande sonará mejor para usted.

### La grabación no encuentra un programa

La captura de programas requiere Windows 10 versión 2004 o posterior. El programa también debe tener una sesión de audio de Windows activa. Inicie la reproducción en ese programa, vuelva a abrir el cuadro de diálogo de fuentes y elíjalo en la lista actualizada.

### La grabación comienza sin una de sus fuentes

Luna puede continuar cuando se abre al menos una fuente. Lea la advertencia que enumera las fuentes que fallaron y luego compruebe si sus dispositivos están conectados, habilitados y disponibles. Detenga la grabación antes de editar la lista de fuentes.

### Un atajo global no funciona

Los atajos globales están activos solo mientras otra aplicación tiene el foco. Cambie a otra aplicación con Alt+Tab o minimice Luna, y luego use el atajo. Mientras Luna o uno de sus cuadros de diálogo tiene el foco, use en cambio los atajos locales. Compruebe la asignación en **Preferencias > Atajos globales**. Si la misma combinación también realiza una acción de Windows, asigne otras teclas; Luna no suprime el tratamiento que el sistema operativo da a estas combinaciones.

### La configuración no se guarda después de un error de inicio

Luna protege un `settings.json` no válido o ilegible en lugar de sobrescribirlo. Abra Preferencias e importe una copia de seguridad de configuración válida o elija **Copia de seguridad y restauración > Restablecer configuración**. Si necesita el original para un diagnóstico, cópielo primero desde la carpeta de configuración del usuario.

### Los marcadores o los favoritos informan datos no válidos

Un archivo de almacenamiento no válido se conserva en lugar de reemplazarse silenciosamente. Abra la carpeta de configuración del usuario, haga una copia del archivo JSON afectado y restaure una copia de seguridad válida conocida. Los marcadores se pueden restaurar a través de Preferencias. Los favoritos requieren actualmente restaurar el propio archivo `favorites.json` mientras Luna está cerrado.

### La guía del usuario no se abre

La guía instalada debería estar en `docs\<código-de-idioma>\user-guide.html` junto al ejecutable. Vuelva a extraer el archivo portátil completo o repare/reinstale la aplicación si falta la carpeta `docs`. Luna recurre del idioma regional a su idioma base y luego al inglés.

## 26. Editor y licencias

Luna Player es publicado por **Diamond Star**.

Copyright © 2026 Diamond Star.

Luna Player es software libre licenciado bajo la Licencia Pública General de GNU, versión 3 o (a su elección) cualquier versión posterior (GPL-3.0-or-later). El enlace de mpv traducido en `src/Mpv.cs` está licenciado bajo la Licencia Pública General Reducida de GNU, versión 2.1 o posterior.

Luna Player también usa componentes de terceros con licencias independientes, incluidos mpv, FFmpeg, wxWidgets, Prism, NAudio y PyYt. Cada componente se mantiene bajo su propia licencia. Consulte `NOTICE.txt`, instalado junto con Luna Player, para ver las declaraciones de derechos de autor, las versiones de los componentes, los nombres de las licencias y las ubicaciones del código fuente. Los textos de licencia pertinentes se instalan en la carpeta `licenses`.

El resumen de licencias de esta guía se ofrece por conveniencia. `LICENSE.txt`, `NOTICE.txt` y los archivos de la carpeta `licenses` contienen los términos y las atribuciones oficiales. En el repositorio del código fuente, los archivos correspondientes de nivel superior se llaman `LICENSE` y `NOTICE`.

## 27. Contacto y soporte

Luna Player es desarrollado y publicado por Diamond Star. Puede usar los siguientes canales:

- **Correo electrónico:** [ramymaherali55@gmail.com](mailto:ramymaherali55@gmail.com)
- **Telegram:** [Contactar a Diamond Star en Telegram](https://t.me/diamondStar35)
- **Informes de errores:** [Problemas de Luna Player en GitHub](https://github.com/diamondStar35/luna_player/issues)
- **Código fuente y versiones:** [Luna Player en GitHub](https://github.com/diamondStar35/luna_player)

Para un informe de errores, incluya la versión de Luna que se muestra en **Ayuda > Acerca de**, su versión de Windows, lo que esperaba, lo que ocurrió y los pasos más breves que reproducen el problema. Incluya el texto exacto de cualquier mensaje de error. Elimine los nombres de archivo privados, las direcciones web, los datos de cuentas y otra información personal de los registros o las capturas de pantalla antes de compartirlos públicamente.
