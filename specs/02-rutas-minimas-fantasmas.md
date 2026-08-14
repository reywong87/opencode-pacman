# SPEC 02 — Rutas mínimas para fantasmas

> **Status:** Implemented
> **Depends on:** SPEC 01
> **Date:** 2026-08-14
> **Objective:** Corregir la navegación de los cuatro fantasmas para que elijan rutas mínimas hacia sus objetivos y usen los túneles laterales solo cuando acorten el recorrido.

## Scope

**In:**

- Sustituir la decisión basada en distancia Manhattan de `DecideGhost` en `Game/PacmanGame.cs`.
- Calcular rutas mínimas sobre la cuadrícula transitable para los cuatro fantasmas liberados.
- Modelar los extremos izquierdo y derecho de la fila de túnel `14` como conexiones entre sí.
- Evitar que un fantasma invierta su dirección actual, salvo cuando sea su único movimiento válido.
- Normalizar los objetivos que queden fuera del tablero al borde válido más cercano.
- Resolver empates de ruta con la prioridad `up`, `left`, `down`, `right`.
- Verificar el escenario de Pac-Man en la esquina superior derecha y la compilación del proyecto.

**Out of scope (for future specs):**

- Cambiar los objetivos, umbrales de liberación o ciclos de IA definidos en SPEC 01.
- Cambiar velocidades, colisiones, renderizado, sonidos o el diseño del laberinto.
- Añadir pruebas automatizadas.

## Data model

Esta funcionalidad no introduce estado persistente ni cambia el contrato `GameFrame` o `ActorFrame`.

`Game/PacmanGame.cs` añadirá una prioridad fija de decisión y helpers de navegación para evaluar la distancia mínima desde cada movimiento candidato hasta el objetivo normalizado.

```csharp
private static readonly string[] GhostDecisionDirections =
[
    "up", "left", "down", "right"
];
```

Los helpers se llamarán `NormalizeGhostTarget`, `GhostNeighbors` y `ShortestGhostPathDistance`.

## Implementation plan

1. Añadir `GhostDecisionDirections`, `NormalizeGhostTarget` y `GhostNeighbors` en `Game/PacmanGame.cs` para representar prioridades, objetivos válidos y conexiones del túnel sin alterar el movimiento actual.
2. Añadir `ShortestGhostPathDistance` para obtener la distancia de ruta mínima entre dos casillas transitables del laberinto, incluyendo el salto entre los extremos del túnel de la fila `14`.
3. Actualizar `DecideGhost` para descartar la dirección opuesta excepto ante bloqueo, normalizar el objetivo y escoger el movimiento con la menor distancia de ruta usando la prioridad fijada en caso de empate.
4. Ejecutar el juego y reproducir el caso con Pac-Man en la esquina superior derecha para comprobar que los cuatro fantasmas abandonan cualquier recorrido repetido del túnel y continúan hacia sus objetivos.
5. Ejecutar `dotnet build open-pacman.csproj`.

## Acceptance criteria

- [ ] `dotnet build open-pacman.csproj` finaliza correctamente.
- [ ] Los cuatro fantasmas liberados seleccionan movimientos usando distancia de ruta mínima en vez de distancia Manhattan directa.
- [ ] La salida izquierda y la salida derecha de la fila `14` se consideran conectadas durante la navegación de fantasmas.
- [ ] Un fantasma usa el túnel lateral solo cuando produce una ruta más corta hacia su objetivo.
- [ ] Un fantasma no invierte su dirección en un cruce salvo que no tenga otra dirección transitable.
- [ ] Los objetivos de IA fuera del tablero se limitan al borde válido más cercano antes de calcular la ruta.
- [ ] Dos rutas de igual longitud se resuelven con prioridad `up`, `left`, `down`, `right`.
- [ ] Con Pac-Man en la esquina superior derecha, los cuatro fantasmas dejan de recorrer repetidamente el túnel lateral y vuelven a dirigirse hacia sus objetivos.

## Decisions

- **Yes:** Búsqueda de ruta mínima sobre la cuadrícula. Representa las conexiones reales del laberinto y elimina el bucle causado por Manhattan.
- **Yes:** Túneles laterales como conexión válida. Los fantasmas los conservan cuando realmente acortan la ruta.
- **Yes:** Sin inversión de dirección salvo bloqueo. Mantiene la regla de movimiento acordada.
- **Yes:** Objetivos exteriores limitados al borde. Mantiene el patrón del rosa sin buscar posiciones inválidas.
- **Yes:** Prioridad `up`, `left`, `down`, `right` para empates. Produce decisiones reproducibles.
- **No:** Excepción específica para la esquina superior derecha. Corregiría solo el síntoma y dejaría fallos equivalentes.
- **No:** Cambiar la IA individual de los fantasmas. Sus objetivos ya fueron definidos en SPEC 01.

## Risks

| Risk | Mitigation |
| --- | --- |
| La búsqueda se ejecuta en cada cruce de cada fantasma. | Limitarla a la cuadrícula fija de 28 por 31 y solo a fantasmas alineados. |
| Un objetivo normalizado cae sobre un muro. | Usar la casilla transitable válida más cercana al objetivo limitado. |
| Una ruta inexistente deja al fantasma sin elección. | Conservar el movimiento válido con mejor prioridad cuando no exista ruta al objetivo. |

## What is **not** in this spec

- Cambios a los patrones de persecución y retirada de SPEC 01.
- Cambios visuales o de audio.
- Pruebas automatizadas.
- Cambios al laberinto o a las reglas de túneles de Pac-Man.

Each excluded item belongs in its own future spec.
