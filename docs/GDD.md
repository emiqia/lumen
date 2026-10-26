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
| Atenuar (sigilo) | Shift | LT / LB |
| Destello | Espacio | A / RT |
| Pausa | Esc | Start |
| Idioma (título) | L | — |

## Mecánicas

### Combustible (= vida = tiempo)
- Máximo 100. Se consume a **1.5/s** con luz plena y a **0.55/s** atenuado.
- El radio de luz escala con el combustible (3.2 → 7.5 unidades). Con poco combustible ves menos y la llama chisporrotea.
- Si llega a 0, te extinguís.
- Se transfiere entre pisos (+25 de bonus, mínimo 50).

### Atenuar (Shift)
- Radio de luz fijo de 2.2, movimiento al 70%, consumo bajo.
- Los Acechadores te detectan desde mucho más cerca y las Polillas pierden el interés.
- No quemás a las sombras mientras estás atenuado.

### Destello (Espacio)
- Cuesta **18** de combustible, enfriamiento de 1.1 s, radio de 5.5.
- Mata a Acechadores y Polillas en línea de visión. Aturde al Devorador (hacen falta 3 destellos para matarlo).

### Braseros
- Se encienden al tocarlos. Hay que encender todos para abrir la salida.
- Encendidos son **zonas seguras**: recargan 22/s, los Acechadores huyen y se queman.
- Atraen a las Polillas, así que encender uno es también una forma de distraerlas.

### Brasas
- Pickups flotantes: +12 de combustible y +10 de puntaje. Se acercan solas al jugador (imán).

### Presión
- Cada 40 s en un piso, la oscuridad hace nacer un Acechador nuevo lejos del jugador. Así se evita quedarse acampando en un brasero.

## Enemigos

| Enemigo | Desde piso | Comportamiento | Contra |
|---|---|---|---|
| **Acechador** | 1 | Deambula; si te ve (rango = 1.5 + 1.15 × tu radio de luz, con línea de visión) te persigue. Pierde el rastro tras 3 s sin verte. | Atenuar para que no te vea, tu luz plena lo quema de cerca, destello |
| **Polilla** | 2 | Atraída por la luz a distancia (sin necesidad de línea de visión). Te orbita y después se lanza en picada. Roba 10 de combustible y muere. | Atenuar, encender braseros para distraerlas, destello |
| **Devorador** | 3 | Lento, gigante e inmune a la luz. Va hacia los braseros encendidos y los apaga, lo que vuelve a sellar la salida. | 3 destellos, o ser rápido |

Los cuerpos de las sombras se dibujan con material *lit*: **solo se ven donde llega la luz**. Sus ojos son *unlit* y brillan en la oscuridad. Así ves ojos acechando en lo negro antes de ver el cuerpo.

## Progresión (por piso)

`FloorSettings.ForFloor(n)` concentra toda la curva de dificultad:
- Mapa: 40×30 → 76×56 tiles. Salas: 5+n (máx. 13).
- Braseros: 2 + (n+1)/2 (máx. 7). Brasas: 14−n (mín. 6).
- Acechadores: 1+n (máx. 9). Polillas: n−1 desde el piso 2. Devoradores: 1 desde el piso 3, 2 desde el piso 6.

## Puntaje

Brasa 10 · Brasero 50 · Acechador 25 · Polilla 15 · Devorador 150 · Piso completado 100×n + 4 por segundo bajo 75 s.

## Dirección de arte

- Vista 3/4: los muros muestran cara de ladrillo hacia el sur. Los ShadowCasters excluyen la mitad inferior de esa cara, así que la luz "sube" por la pared.
- El nivel entero se hornea en **una textura + normal map**. Light2D usa los normales para sombrear ladrillos y biseles de piedra.
- Post-procesado: bloom (el fuego "quema"), viñeta que se tiñe de rojo con poco combustible, film grain y contraste.
- Paleta fría y desaturada para la mazmorra, y cálida para todo lo que es fuego.
- Audio 100% sintetizado: drone ambiental, latido que acelera con poco combustible y efectos generados con osciladores y ruido.

## Accesibilidad

- Toggle de temblor de pantalla (Pausa → S).
- Español / inglés.
- Soporte de gamepad.
- Fuente pixel de alto contraste con sombra.
