# LUMEN — Game Design Document

**Género:** sigilo top-down / roguelite de partidas cortas
**Motor:** Unity 6 (URP 2D, Light2D + ShadowCaster2D)
**Plataformas:** Windows / Linux (escritorio)
**Arte:** pixel art 100% procedural (generado por código al iniciar, sin assets importados)
**Duración de una partida:** 3–15 minutos

## Pitch

Sos una pequeña llama viva en una mazmorra sin luz. **Tu luz es tu vida, tu radio de visión y tu reloj**: se consume con el tiempo. Ver más te hace más visible para las criaturas de la oscuridad. Encendé todos los braseros de cada piso para abrir la salida y bajá lo más profundo que puedas.

## Pilares

1. **La luz es un recurso con doble filo.** Ver más = te ven más.
2. **Sombras reales, consecuencias reales.** Los muros proyectan sombras en tiempo real. La IA usa exactamente la misma línea de visión que el render: si una sombra te cubre en pantalla, el enemigo tampoco te ve.
3. **"Una más".** Partidas cortas, reinicio instantáneo (R) y récord persistente.

## Loop principal

```
Explorar en penumbra → Encontrar brasero → Encenderlo (zona segura + recarga)
      ↑                                                   ↓
Bajar de piso ← Salida se abre ← Todos los braseros encendidos
```

## Controles

| Acción | Teclado | Gamepad |
|---|---|---|
| Mover | WASD / Flechas | Stick izq. / D-pad |
| Correr (gasta luz) | Shift | RT |
| Atenuar (sigilo) | Ctrl | LT |
| Destello | Espacio | A |
| Usar señuelo | E | Y |
| Pausa / menú | Esc | Start |

Los menús se navegan con flechas / D-pad y Enter / A. Brillo, temblor, modo daltónico, cronómetro, idioma, volumen de música y efectos y modo de pantalla están en **Opciones** (desde el título o la pausa).

## Estructura de una partida

1. **Título:** el logo y un menú (Jugar / Guía / Opciones / Salir), con una partida del bot de fondo.
2. **Dificultad:** Vela, Antorcha, Hoguera o Ceniza (ver abajo), con el récord de cada una.
3. **El libro:** antes de la primera partida se abre una guía de 10 páginas ilustradas que se pasan con las flechas. Se puede releer desde "Guía".
4. **Pisos:** cada uno con nombre propio ("El Umbral", "Criptas del Polvo", "La Garganta" y después nombres generados por bioma).
5. **Bendiciones:** al bajar elegís 1 de 3 mejoras que se acumulan toda la run.

## Mecánicas

### Combustible (= vida = tiempo)
- Máximo 100. Se consume a **1.5/s** con luz plena y a **0.4/s** atenuado.
- El radio de luz escala con el combustible (3.2 → 7.5 unidades). Con poco combustible ves menos y la llama chisporrotea.
- Si llega a 0, te extinguís.
- Se transfiere entre pisos (+15 de bonus, mínimo 35). Llegar con combustible conviene.
- Las reglas viven en `FuelModel` (C# puro, con tests).

### Atenuar (Shift)
- Radio de luz fijo de 2.2, movimiento a 3.1 (contra 4.2), consumo casi cuatro veces menor.
- Los Acechadores te detectan desde mucho más cerca y las Polillas pierden el interés.
- No quemás a las sombras mientras estás atenuado.

### Destello (Espacio)
- Cuesta **22** de combustible y tiene un enfriamiento de **2.2 s**.
- **Mata dentro de 3.2** unidades y **aturde 2 s entre 3.2 y 5.5**, siempre con línea de visión. Premia arriesgarse a acercarse, no tirarlo de lejos.
- El Devorador necesita 3 destellos. Mientras está aturdido no cuentan, así que son tres enfrentamientos separados.
- Dos anillos de partículas marcan el radio letal y el de aturdimiento.

### Braseros
- Se encienden al tocarlos. Hay que encender todos para abrir la salida.
- Encendidos son **zonas seguras**: recargan 16/s, los Acechadores huyen y se queman.
- Cada uno tiene una **reserva de 60** de combustible. Cuando se agota sigue encendido (cuenta para la salida), pero arde bajo, ya no recarga y su zona segura se achica a 1.8. Acampar ya no es infinito.
- Apagados, las brasas del cuenco laten en rojo para que se reconozcan como objetivo. Una flecha en el borde de la pantalla apunta al brasero apagado más cercano (o a la salida, si está abierta).
- Atraen a las Polillas, así que encender uno es también una forma de distraerlas.

### Correr (Shift)
- Velocidad x1.6, pero la llama arde **3 veces más rápido** y brilla un 20% más, así que los Acechadores te ven de más lejos.
- Es la herramienta del speedrun (hay un cronómetro opcional en Opciones). No se puede correr atenuado.

### Brasas
- Pickups flotantes: +12 de combustible y +10 de puntaje. Se acercan solas al jugador (imán).
- Nunca aparecen a menos de 6 casillas de un brasero, porque ahí se desperdiciarían.

### Objetos
- **Farol:** absorbe el próximo golpe. Se lleva uno.
- **Aceite:** el próximo destello es gratis. Hasta 2.
- **Señuelo (E):** se tira hacia donde mirás. Brilla 7 s: las Polillas lo prefieren a todo y los Acechadores cercanos van a investigarlo. Hasta 2.
- **Mapa:** marca en el borde de la pantalla todos los braseros apagados del piso.
- Hay 1 objeto por piso, hasta 3 en pisos profundos.

### Bendiciones
Al completar un piso se ofrecen 3 de 11 (determinísticas por semilla). Ejemplos: Mecha larga (+20 de luz máxima), Destello barato (-25%), Sombra amiga (atenuar gasta 30% menos), Pulmones de fragua (correr gasta 35% menos) y Segunda chispa (renacés una vez, no se repite).

### Presión
- Cada 40 s en un piso, la oscuridad hace nacer un Acechador nuevo lejos del jugador, **que sale ya buscando tu posición**.
- Desde el piso 7 el intervalo baja 2 s por piso, con un mínimo de 22 s.

## Enemigos

| Enemigo | Desde piso | Comportamiento | Contra |
|---|---|---|---|
| **Acechador** | 1 | Deambula; si te ve (rango = 1.5 + 1.15 × tu radio de luz, con línea de visión) te persigue a 3.6, más rápido que vos atenuado. Pierde el rastro tras 3 s sin verte. | Atenuar antes de que te vea, tu luz plena lo quema de cerca (1.8 s), destello |
| **Polilla** | 2 | Atraída por la luz a distancia (sin necesidad de línea de visión). Te orbita a 3 unidades y después se lanza en picada. Roba 14 de combustible, se aleja 2.5 s y vuelve. | Atenuar, encender braseros para distraerlas, destello |
| **Devorador** | 3 | Lento, gigante e inmune a la luz. Va hacia los braseros encendidos y los apaga, lo que vuelve a sellar la salida. Después de cada destello queda **5 s sin poder ser herido** y te carga enfurecido (más lento que vos). Si te golpea, se calma. | 3 destellos, uno por vez. Las **3 marcas** sobre su cabeza muestran cuántos faltan: oscuras y parpadeando = inmune; naranjas = pegale ahora |

Los cuerpos de las sombras se dibujan con material *lit*: **solo se ven donde llega la luz**. Sus ojos son *unlit* y brillan en la oscuridad. Así ves ojos acechando en lo negro antes de ver el cuerpo.

## Progresión (por piso)

`FloorSettings.ForFloor(n)` concentra toda la curva de dificultad. El tamaño del mapa topa temprano para que los pisos sigan siendo navegables. La presión sigue subiendo después:
- Mapa: 40×30 → 68×50 tiles (tope en el piso 7). Salas: 5+n (máx. 12).
- Braseros: 2 + (n+1)/2 (máx. 7). Brasas: 13−n (mín. 4).
- Acechadores: 1+n (máx. 14). Polillas: n−1 desde el piso 2 (máx. 8). Devoradores: 1 + (n−3)/4 desde el piso 3 (1, 2, 3, 4… en los pisos 3, 7, 11, 15).
- Velocidad de enemigos: +3% por piso desde el 9 (tope ×1.3).
- Cada piso tiene su propia semilla, derivada de la de la run, así que las runs completas se pueden reproducir.

## Puntaje

Brasa 10 · Brasero 50 · Acechador 25 · Polilla 15 · Devorador 150 · Piso completado 100×n + 4 por segundo bajo el par (30 + 6 × min(n, 9) s).

El puntaje lo calcula `ScoreService` a partir de eventos: el gameplay nunca toca el puntaje directamente.

## Biomas

Cada 3 pisos cambia el lugar, con su paleta, su luz ambiente y su peligro. El ciclo se repite más difícil:

| Pisos | Bioma | Peligro |
|---|---|---|
| 1–3 | **Criptas** | Rejas que se abren y cierran (7 s abiertas, 4 s cerradas, y avisan 1 s antes). Cerradas bloquean el paso, la luz y la visión. Desde el piso 2. |
| 4–6 | **Cavernas inundadas** | Charcos de agua negra. Caminar es gratis, pero correr por el agua drena 9/s. |
| 7–9 | **Raíces** | Pasillos con ráfagas periódicas (avisadas por hojas) que achican la luz al 45% y te empujan. |

## Dificultades

| | Vela | Antorcha | Hoguera | Ceniza |
|---|---|---|---|---|
| Drenaje de luz | x0.75 | x1 | x1.15 | x1.3 |
| Velocidad de enemigos | x0.85 | x1 | x1.1 | x1.2 |
| Daño recibido | x0.7 | x1 | x1.25 | x1.5 (el Devorador mata de un golpe) |
| Reserva de braseros | x1.3 | x1 | x0.8 | x0.55 |
| Vista de los Acechadores | x0.85 | x1 | x1.1 | x1.25 |

Los récords se guardan por dificultad.

## Tutorial contextual

Cada consejo aparece la primera vez que su situación ocurre en la run, no por reloj. Los urgentes se adelantan en la cola:
- **Moverse:** al empezar.
- **Encender braseros:** cuando te moviste un poco.
- **Recargar:** al encender el primer brasero.
- **Atenuar:** cuando un Acechador te ve, o cuando hay uno cerca.
- **Destello:** cuando hay una sombra a menos de 4.5.
- **Brasero seco:** cuando uno se queda sin reserva.
- **Devorador:** la primera vez que hay uno cerca ("3 destellos, uno por vez").
- **Correr:** si pasaron 20 s y nunca corriste.
- **Señuelo:** cuando juntás uno.

Si no viste un consejo en el piso 1, puede salir en el piso 2.

## Dirección de arte

- Vista 3/4: los muros muestran cara de ladrillo hacia el sur. Los ShadowCasters excluyen la mitad inferior de esa cara, así que la luz "sube" por la pared.
- El nivel entero se hornea en **una textura + normal map**. Light2D usa los normales para sombrear ladrillos y biseles de piedra.
- Post-procesado: bloom (el fuego "quema"), viñeta que se tiñe de rojo con poco combustible, film grain sutil y contraste.
- Luz ambiente **azul fría** (intensidad 0.14) contra el fuego **cálido**. Fuera de la luz ves un valor medio azulado y legible, no negro puro, así que las sombras de los muros se leen.
- Los ojos de las sombras tienen un halo unlit para que la viñeta no los borre. Las polillas se dibujan pálidas y tenues para verlas venir.
- Las decoraciones van un escalón más oscuras que los objetos de juego. La calavera tiene dientes y mandíbula para no confundirse con un enemigo, y las velas tienen llama.
- Audio 100% sintetizado: drone ambiental, latido que acelera con poco combustible y efectos generados con osciladores y ruido.

## Accesibilidad

- Todo en el menú de Opciones: temblor de pantalla, brillo (80% a 250%), volumen de música y de efectos por separado, ventana o pantalla completa y cronómetro.
- **Modo daltónico:** los ojos rojo/verde pasan a magenta/cian. Además, siempre: el Acechador tiene ojos-ranura y la flecha de salida es un rombo, así que nunca se depende solo del color.
- Español / inglés.
- Soporte de gamepad.
- Fuente pixel de alto contraste con sombra.
