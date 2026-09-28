# Audio de Bach

Los proyectos de Reaper y sus generadores viven fuera de `Assets`. Unity usa los WAV exportados en `Assets/Resources` y no necesita que Reaper esté abierto.

| Carpeta | Contenido |
| --- | --- |
| `Music/` | Proyecto `Templo_Adaptativo.RPP`, generador y previsualizaciones de la música por capas |
| `SFX/` | Proyecto `Bach_Efectos.RPP` y generador de los cuatro efectos independientes |
| `Assets/Resources/AdaptiveMusic/` | Siete stems sincronizados que reproduce `AdaptiveMusicManager` |
| `Assets/Resources/GameSFX/` | Golpe de vida, campanita, derrota y trompetas que reproduce `GameSfxManager` |

## Música

Los siete stems activos duran 20 segundos, empiezan en el compás 1 y forman un bucle de ocho compases a 96 BPM. El drone, el pad y un tambor discreto sostienen el inicio; las campanas de fondo quedan bajas para distinguir la campanita de respuesta. `TTSManager.IsSpeaking` baja la música mientras habla la voz.

El ritmo entra por niveles, en el siguiente compás: un acierto o 120 segundos restantes incorporan el pulso; dos aciertos, 60 segundos restantes o una sola vida incorporan la percusión rápida; a los 25 segundos el ritmo llega a su máximo. Perder una vida silencia la percusión durante un pulso y aumenta inmediatamente la textura disonante. El peligro queda separado del avance, así que un error no reduce el nivel rítmico de forma permanente. Las capas salen con un fundido cuando termina la ronda, sin un cierre musical nuevo.

El proyecto de Reaper también conserva `08_life_lost.wav` y `09_question_passed.wav` como bocetos anteriores. Esos dos archivos ya no se reproducen en el juego: sus funciones las cumplen los cuatro WAV de `GameSFX`.
`Music/preview_eventos.wav` también muestra esos bocetos y sirve sólo como referencia de producción.
`Music/preview_escalada.wav` comprime la progresión de capas en 20 segundos para escuchar las entradas sin jugar una ronda completa.

## Efectos

Los efectos son WAV estéreo de 48 kHz y 24 bits. La campanita suena en cada respuesta correcta, incluida la última. Las trompetas esperan a que termine la frase de victoria. La derrota espera a que termine «Has perdido»; un error que aún deja vidas reproduce sólo el golpe breve.

## Edición y exportación

Abrí el `.RPP` correspondiente en Reaper. Los archivos fuente apuntan a los WAV que Unity importa; si reexportás, conservá sus nombres y la duración de los stems de música.

Desde la raíz del proyecto, `python AudioProduction/Music/build_music.py` recrea la música y sus previsualizaciones; `python AudioProduction/SFX/build_effects.py` recrea los cuatro efectos. Ambos requieren NumPy. Al regenerar música también se recrean los dos bocetos antiguos, que siguen sin usarse en runtime.
