# SPEC 03 — Power Pellets y modo vulnerable

> **Status:** Implemented
> **Depends on:** SPEC 01, SPEC 02
> **Date:** 2026-08-14
> **Objective:** Incorporar cuatro Power Pellets clásicos que activan seis segundos de vulnerabilidad y permiten comer fantasmas antes de que regresen a la casa.

## Scope

**In:**

- Añadir cuatro Power Pellets en las casillas `(1,3)`, `(26,3)`, `(1,23)` y `(26,23)` del laberinto de 28 por 31.
- Representar los Power Pellets como un valor de cuadrícula propio, distinto de los puntos normales, muros y puerta.
- Dibujar cada Power Pellet como un círculo fijo mayor que un punto normal y con el mismo color de los puntos.
- Otorgar `50` puntos y eliminar el Power Pellet al consumirlo.
- Contar los Power Pellets sin consumir como coleccionables necesarios para ganar el nivel.
- Activar seis segundos reales de vulnerabilidad al consumir un Power Pellet.
- Invertir una vez la dirección de cada fantasma liberado al activarse la vulnerabilidad.
- Hacer que los fantasmas vulnerables usen rutas mínimas para alejarse de Pac-Man en los cruces.
- Mostrar en azul a los fantasmas vulnerables y permitir que Pac-Man los coma.
- Otorgar `200`, `400`, `800` y `1600` puntos por los fantasmas comidos consecutivamente durante la misma activación.
- Hacer que un fantasma comido muestre solo ojos, no colisione y vuelva a la casa antes de salir de nuevo sin requerir su umbral de liberación.
- Aplicar el tiempo vulnerable restante a un fantasma que salga de la casa durante una activación.
- Reiniciar el temporizador vulnerable y la secuencia de puntuación al consumir otro Power Pellet.
- Cancelar la vulnerabilidad y reiniciar la secuencia de puntuación al perder una vida, sin restaurar los Power Pellets ya consumidos.
- Actualizar el contrato de valores de cuadrícula en `AGENTS.md`.
- Verificar la compilación y el flujo manual de consumo, vulnerabilidad, fantasma comido, regreso y final de nivel.

**Out of scope (for future specs):**

- Parpadeo visual del final de la vulnerabilidad.
- Variaciones de duración o velocidad por nivel.
- Sonidos, animaciones de puntuación flotante o pantallas de transición.
- Nuevos niveles, reinicio automático del laberinto o persistencia de puntuaciones.
- Pruebas automatizadas.

## Data model

`Game/PacmanGame.cs` ampliará el contrato de cuadrícula con el valor `4` para un Power Pellet y mantendrá sus posiciones fuera de `MazeRows` mediante una lista explícita.

```csharp
private const int PowerPelletScore = 50;
private const double FrightenedDurationSeconds = 6;
private static readonly (int X, int Y)[] PowerPelletPositions =
[
    (1, 3), (26, 3), (1, 23), (26, 23)
];

private int collectiblesRemaining;
private int frightenedGhostsEaten;
private DateTime? frightenedUntil;
```

El estado de cada fantasma añadirá `ReturningHome` para distinguir su regreso no letal a la casa de su salida normal.

```csharp
public bool ReturningHome;
```

`ActorFrame` incorporará el estado de vulnerabilidad y de regreso para que `wwwroot/js/pacman.js` pueda renderizar azul o solo ojos sin deducir reglas de juego en JavaScript.

```csharp
public sealed record ActorFrame(
    double X,
    double Y,
    string Direction,
    bool Frightened,
    bool ReturningHome);
```

`AGENTS.md` documentará el valor `4` como Power Pellet en el contrato de cuadrícula compartido entre C# y JavaScript.

## Implementation plan

1. Actualizar `AGENTS.md` y la inicialización de la cuadrícula en `Game/PacmanGame.cs` para documentar y colocar el valor `4` en las cuatro coordenadas acordadas, conservando las dimensiones y los valores existentes.
2. Sustituir el contador de puntos restantes por `collectiblesRemaining` en `Game/PacmanGame.cs` para incluir puntos normales y Power Pellets en la condición de victoria, sin alterar la puntuación de los puntos normales.
3. Actualizar `MovePacman` para consumir un Power Pellet, sumar `50` puntos, iniciar o reiniciar `frightenedUntil` a seis segundos desde el consumo y reiniciar `frightenedGhostsEaten`.
4. Añadir el estado vulnerable en `Game/PacmanGame.cs`: invertir una vez los fantasmas liberados que no regresan, aplicar la vulnerabilidad a los que salgan durante el tiempo restante y hacer que la elección en los cruces maximice la distancia de ruta mínima a Pac-Man con la prioridad de empate de SPEC 02.
5. Actualizar las colisiones y el estado `ReturningHome` para que un fantasma vulnerable otorgue la secuencia `200/400/800/1600`, ignore colisiones durante su regreso, llegue a la casa y vuelva a realizar su ruta de salida sin umbral de puntos.
6. Restablecer al perder una vida el temporizador vulnerable, la cadena de fantasmas comidos y los estados de regreso, manteniendo intacta la cuadrícula con sus coleccionables ya consumidos.
7. Ampliar `ActorFrame` y actualizar `wwwroot/js/pacman.js` para dibujar los Power Pellets, los fantasmas vulnerables en azul y los fantasmas que regresan como ojos, conservando el renderizado de los cuatro colores normales.
8. Ejecutar el juego para comprobar las cuatro posiciones, los `50` puntos, los seis segundos de vulnerabilidad, la huida, la puntuación acumulativa, el regreso visible y la victoria solo después de consumir todos los coleccionables.
9. Ejecutar `dotnet build open-pacman.csproj`.

## Acceptance criteria

- [ ] `dotnet build open-pacman.csproj` finaliza correctamente.
- [ ] La cuadrícula contiene exactamente cuatro celdas de valor `4` al iniciar una partida en `(1,3)`, `(26,3)`, `(1,23)` y `(26,23)`.
- [ ] `AGENTS.md` define `4` como Power Pellet dentro del contrato de cuadrícula.
- [ ] Cada Power Pellet se dibuja como un círculo fijo mayor que un punto normal y con el color de los puntos.
- [ ] Consumir un Power Pellet suma exactamente `50` puntos y elimina su celda de la cuadrícula.
- [ ] El nivel no pasa a `won` mientras quede un punto normal o un Power Pellet.
- [ ] Consumir un Power Pellet activa o reinicia un temporizador de vulnerabilidad de seis segundos reales.
- [ ] Los fantasmas liberados que no regresan invierten una vez su dirección al comenzar una activación de vulnerabilidad.
- [ ] Un fantasma vulnerable elige en un cruce una ruta que aumenta su distancia mínima a Pac-Man, usando la prioridad `up`, `left`, `down`, `right` en caso de empate.
- [ ] Un fantasma que abandona la casa durante una activación se muestra vulnerable y huye hasta que expire el tiempo restante.
- [ ] Los fantasmas vulnerables se dibujan en azul y no reducen una vida al colisionar con Pac-Man.
- [ ] Comer fantasmas vulnerables durante una activación suma en orden `200`, `400`, `800` y `1600` puntos.
- [ ] Un segundo Power Pellet durante la vulnerabilidad reinicia el temporizador a seis segundos y hace que el siguiente fantasma comido valga `200` puntos.
- [ ] Un fantasma comido se muestra solo como ojos, no puede matar ni ser comido y vuelve a la casa antes de salir de nuevo sin esperar puntos adicionales.
- [ ] Al expirar la vulnerabilidad, los fantasmas que no regresan recuperan su aspecto y colisión letales normales.
- [ ] Al perder una vida se cancelan la vulnerabilidad y la secuencia de puntuación, mientras los Power Pellets consumidos permanecen ausentes.

## Decisions

- **Yes:** Cuatro Power Pellets en `(1,3)`, `(26,3)`, `(1,23)` y `(26,23)`. Corresponden a las posiciones clásicas adaptadas a la cuadrícula actual.
- **Yes:** Valor de cuadrícula `4`. Permite conservar una representación explícita y renderizable sin confundir los Power Pellets con puntos normales.
- **Yes:** `50` puntos por Power Pellet. Mantiene la puntuación clásica.
- **Yes:** Seis segundos reales de vulnerabilidad azul fija. Define una duración verificable sin añadir el parpadeo de fin de estado.
- **Yes:** Huida mediante la mayor distancia de ruta mínima. Reutiliza la navegación determinista de SPEC 02 en lugar de inventar una segunda estrategia de movimiento.
- **Yes:** Inversión única al activarse. Hace visible el cambio de modo y conserva el comportamiento acordado para la vulnerabilidad.
- **Yes:** Cadena `200/400/800/1600` por activación. Replica la progresión clásica y se reinicia con cada nuevo Power Pellet.
- **Yes:** Fantasmas comidos como ojos sin colisión hasta volver a la casa. Evita que un fantasma en tránsito afecte el juego y hace visible su recuperación.
- **Yes:** Los fantasmas que se liberan durante la activación heredan el tiempo vulnerable restante. El Power Pellet afecta al conjunto de fantasmas activos del nivel.
- **Yes:** La vulnerabilidad y su cadena se cancelan al perder una vida. El reinicio de posiciones no conserva estados temporales.
- **No:** Parpadeo durante los últimos segundos. Se descarta para mantener el alcance visual limitado al azul fijo.
- **No:** Duración o velocidad por nivel. Requieren un sistema de niveles fuera del alcance actual.
- **No:** Sonidos y animaciones de puntuación. Son mejoras audiovisuales independientes de las reglas de juego.
- **No:** Pruebas automatizadas. La verificación acordada combina ejecución manual y compilación del proyecto.

## Risks

| Risk | Mitigation |
| --- | --- |
| La ampliación de `ActorFrame` deja C# y JavaScript con formas JSON incompatibles. | Cambiar la creación del frame y el renderizador en el mismo paso y comprobar la consola al ejecutar el juego. |
| Un fantasma que regresa sigue recibiendo reglas de vulnerabilidad o colisión. | Priorizar `ReturningHome` sobre los estados vulnerable y letal en movimiento, colisión y renderizado. |
| El temporizador caduca durante una actualización y produce un estado visual distinto al de las reglas. | Evaluar `frightenedUntil` una sola vez por actualización y usar ese estado para movimiento, colisiones y frame. |
| La huida elige el túnel de forma incoherente. | Reutilizar la distancia de ruta mínima y las conexiones del túnel definidas en SPEC 02. |

## What is **not** in this spec

- Parpadeo de final de vulnerabilidad, sonidos o animaciones de puntuación.
- Velocidades o duraciones variables por nivel.
- Nuevos niveles, persistencia de puntuaciones o reinicio automático del laberinto.
- Pruebas automatizadas.

Each excluded item belongs in its own future spec.
