# Guía del proyecto

## Escena y responsabilidades

`Assets/Scenes/Main Scene.unity` es la única escena activa del build. Las escenas de respaldo, recuperación y demos se retiraron de Assets.

| Ubicación | Responsabilidad |
| --- | --- |
| `Assets/Scripts/Core` | Estados de partida y acciones compartidas por botones, teclado y voz |
| `Assets/Scripts/Gameplay` | Preguntas, vidas y tiempo |
| `Assets/Scripts/Voice` | Lectura y reconocimiento de comandos |
| `Assets/Scripts/Audio` | Música adaptativa, volumen y efectos |
| `Assets/Scripts/UI` | Presentación de preguntas, vidas y degradados de Arancibia |
| `Assets/Scripts/MainScene` | Recorrido, puertas y triggers |
| `Assets/Scripts/MainScene/UI` | Conexión de los menús editables con el estado del juego |
| `Assets/Scripts/Camera` | Seguimiento y plano final de cámara |
| `Assets/Scripts/Player` | Movimiento y animaciones del personaje |
| `AudioProduction` | Fuentes y generadores de audio, fuera del runtime de Unity |

La estructura existente y los GUID se conservan. Los paquetes de terceros permanecen separados del código propio. Al mover un asset, mover también su `.meta`, preferiblemente desde Unity.

## Reglas de integración

- Modificar la lógica de menús en `MenuController`: clic, Escape y voz deben usar las mismas acciones.
- Conectar botones una sola vez y retirar únicamente los listeners propios al destruir el componente.
- Cambiar vidas en `LivesManager`; `LivesUI` se actualiza por su evento y al mostrar el HUD.
- Mantener `IsSpeaking` para reconocimiento y respuestas. `IsBlockingTraversal` distingue las preguntas de los anuncios de menú, para que Continuar no espere el anuncio de pausa.
- Leer pregunta y opciones por segmentos: el resaltado cambia al iniciar cada segmento y la respuesta se habilita al finalizar la lectura.
- Victoria: detener al jugador, iniciar plano bajo de cámara, esperar voz y transición, reproducir trompetas y mostrar resultado al terminar el audio.
- Mantener tamaños, sprites y referencias editables en la escena; evitar crear pantallas completas por código.
- Mantener el diseño visual del equipo: fuentes, sprites y degradados existentes, con márgenes y escalas uniformes.

## Presentación

El Canvas toma 1920 × 1080 como referencia. Vidas y Pausa se anclan a las esquinas superiores con margen. El reloj se ancla arriba al centro y las respuestas forman dos filas debajo de la pregunta. El feedback tiene entrada y salida suaves que se detienen con la pausa.

Los degradados calculan sus límites una sola vez por actualización de malla. El reloj reconstruye su texto cuando cambia el segundo visible; el pulso de peligro se actualiza mientras el temporizador corre.

## Verificación antes de exportar

Abrir Main Scene y probar en Game View con Full HD:

1. Iniciar por botón y por voz; verificar escudos completos y 03:00.
2. Probar pausa/continuar con clic→clic, clic→voz, voz→clic y Escape. Repetir durante el recorrido y durante una pregunta.
3. Comprobar que el resaltado sigue pregunta y cuatro opciones y que no se aceptan respuestas durante la lectura.
4. Fallar y acertar: revisar feedback, escudos, retroceso, animación y apertura de puerta.
5. Pausar durante el feedback y continuar; abrir otra pregunta antes de que termine el aviso anterior.
6. Agotar vidas y tiempo en partidas separadas; verificar resultado y efectos.
7. Ganar: verificar cámara baja, trompetas y cartel posterior. Volver al menú y jugar otra ronda completa.
8. Cambiar volumen, volver, iniciar otra ronda y comprobar que reloj, colores y vidas se reinician.
9. Repetir en el ejecutable de Windows con micrófono y voz SAPI disponibles. Revisar Console y Player.log.

Una compilación C# correcta no reemplaza estas pruebas visuales, de audio y entrada en Unity y en el build.
