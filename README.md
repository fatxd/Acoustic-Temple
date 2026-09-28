# Acoustic Temple

`Assets/Scenes/Main Scene.unity` es la escena de inicio con el nivel 3D y todos los sistemas de juego: menús, preguntas, voz, música y efectos. En la Hierarchy, `Systems` reúne `Core` (estado y menú), `Gameplay` (preguntas, vidas y tiempo), `Voice` (reconocimiento y TTS) y `Audio` (volumen, música y efectos). `Bach.unity` queda guardada como respaldo independiente y no se carga durante la partida. `Veron.unity` conserva el nivel original.

## Código propio

| Carpeta | Responsabilidad |
| --- | --- |
| `Assets/Scripts/Bach/Core/` | Estado y transiciones generales de la partida |
| `Assets/Scripts/Bach/Gameplay/` | Preguntas, vidas y temporizador |
| `Assets/Scripts/Bach/Audio/` | Música adaptativa, efectos y volumen |
| `Assets/Scripts/Bach/Voice/` | Comandos de voz, TTS y menús hablados |
| `Assets/Scripts/Bach/UI/` | Menú principal, pausa y pantalla de volumen |
| `Assets/Scripts/MainScene/` | Avance automático, triggers, apertura de puertas y conexión de la UI editable de Main Scene |

Los scripts conservan sus `.meta` y GUID originales, por lo que las referencias de la escena siguen apuntando a los mismos componentes. Los paquetes y assets de terceros permanecen en sus carpetas existentes.

## Flujo de una ronda

1. En Main Scene, «empezar» inicia el temporizador y el personaje avanza solo desde `Spawn_Player`.
2. Un trigger lo detiene antes de cada puerta y recién entonces se lee esa pregunta. Los tres triggers de puerta y el de meta son objetos editables bajo `NIVEL_TEMPLO/Triggers`, con su posición y volumen configurados en el Inspector. Un acierto reproduce la campanita, abre las dos hojas de la puerta hacia los costados y reanuda el avance. La puerta permanece visible; si falta alguna hoja, se libera su colisión para permitir el paso. Un error descuenta una vida, reproduce el golpe, lo hace retroceder brevemente y repite las opciones.
3. Tras la tercera puerta, el personaje recorre el tramo final. Al llegar a `Meta`, pasa a `Victory`, dice «Has ganado» y luego reproduce las trompetas.
4. Al agotar vidas o tiempo, pasa a `GameOver`, habla el mensaje de derrota y luego reproduce el efecto largo.

`TTSManager.IsSpeaking` mantiene desactivado el reconocimiento de comandos y reduce la música mientras la voz habla. Los proyectos editables de Reaper, generadores y detalles de exportación están en [AudioProduction](AudioProduction/README.md).

## Menús del juego

El menú principal ofrece **Empezar**, **Volumen** y **Salir** con botones y comandos de voz. Durante la partida, «pausa», la tecla Escape o el botón **Pausa** detienen el tiempo y el audio de Unity. En pausa se puede **Continuar**, **Volver al menú**, cambiar el **Volumen** o **Salir**. En la pantalla de volumen, «volver» regresa al menú del que se abrió. Se puede ajustar el nivel del 1 al 10 con el control o diciendo el número.

La UI de `Main Scene` está guardada como objetos editables bajo `UI` en la Hierarchy. `GameMenuUI` conecta sus controles con `Systems/Core`; no crea pantallas durante Play. [Guía de UI](UI_HANDOFF.md) explica la estructura para Arancibia.

## Prueba rápida

Abrí `Main Scene.unity` en Unity y pulsá **Play**. Iniciá desde **Empezar** o decí «empezar». El personaje debe detenerse ante la primera puerta antes de que hable la pregunta. Contestá «uno», «dos», «tres» o «cuatro» y probá «pausa» durante el recorrido. `Bach.unity` sigue disponible como respaldo, fuera de Build Settings.
