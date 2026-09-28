# UI de Main Scene

En `Assets/Scenes/Main Scene.unity`, la Hierarchy separa `NIVEL_TEMPLO` (recorrido 3D), `Systems` (lógica, voz y audio) y `UI` (interfaz). Bajo `Systems` están `Core`, `Gameplay`, `Voice` y `Audio`; bajo `NIVEL_TEMPLO/Triggers` están los tres puntos de pregunta y el de meta. `Bach.unity` queda como respaldo y no se carga.

## Paneles editables

| Objeto bajo `UI` | Cuándo aparece | Controles |
| --- | --- | --- |
| `MainMenuPanel` | Menú principal | Empezar, Volumen, Salir |
| `PausePanel` | Partida pausada | Continuar, Volver al menú, Volumen, Salir |
| `VolumePanel` | Ajuste de volumen | Control de 1 a 10, menos, más, Volver |
| `ResultPanel` | Victoria o derrota | Volver al menú, Salir; el título cambia según el resultado |
| `HUD` | Durante la partida | Pausa |

`UI` tiene un Canvas de pantalla completa y el componente `GameMenuUI`. Sus referencias están asignadas en el Inspector. Los eventos de los botones se conectan por código en `GameMenuUI.Awake`, por eso `On Click()` aparece vacío en cada botón: no agregues la misma acción allí o se ejecutará dos veces. `EventSystem` está bajo `UI`.

El componente `BachMenuController` de `Systems/Core` conserva su nombre histórico, pero es una instancia de Main Scene: no abre ni carga la escena `Bach.unity`.

Podés cambiar colores, tipografías, imágenes, tamaños, posiciones y textos visuales. Conservá los componentes `Button`, `Slider` y `Text` que figuran en `GameMenuUI`, o reasigná sus reemplazos en el Inspector. La lógica de estados, comandos de voz, preguntas, volumen y pausa está en `Systems` y en los scripts; no depende del diseño visual.

Para previsualizar un panel inactivo en Edit Mode, activalo temporalmente en la Hierarchy y desactivá el panel que esté visible. `GameMenuUI` decidirá qué panel mostrar al entrar en Play.
