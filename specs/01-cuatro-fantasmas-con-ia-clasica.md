# SPEC 01 — Cuatro fantasmas con IA clásica

> **Status:** Approved
> **Depends on:** None
> **Date:** 2026-08-13
> **Objective:** Incorporar cuatro fantasmas letales con salida por puntos y comportamientos clásicos diferenciados.

## Scope

**In:**

- Reemplazar los dos fantasmas actuales por rojo, rosa, cian y naranja.
- Mostrar los cuatro fantasmas con los colores clásicos en `wwwroot/js/pacman.js`.
- Mantener los cuatro fantasmas dentro de la casa al inicio en dos filas de dos.
- Liberar rojo, rosa, cian y naranja al obtener respectivamente 0, 100, 300 y 600 puntos durante la vida actual.
- Hacer que cada fantasma liberado siga una ruta fija hasta la puerta rosa antes de aplicar su IA.
- Hacer letales las colisiones con fantasmas liberados.
- Reiniciar las posiciones, el contador de puntos de la vida y la secuencia de liberación al perder una vida.
- Verificar la implementación mediante `dotnet build open-pacman.csproj`.

**Out of scope (for future specs):**

- Modo vulnerable, poderes y comer fantasmas.
- Sonidos, animaciones adicionales y nombres o indicadores visuales de la IA.
- Niveles de dificultad, cambios de velocidad y cambios del laberinto.
- Pruebas automatizadas.

## Data model

`Game/PacmanGame.cs` ampliará el estado de cada fantasma para conservar su tipo, su estado de liberación, su ruta de salida y el tiempo de su ciclo de IA.

```csharp
private sealed class Actor(double x, double y, string direction, double speed, string kind)
{
    public double X = x, Y = y;
    public string Direction = direction;
    public string? NextDirection;
    public double Speed = speed;
    public string Kind = kind;
    public bool Released;
    public bool LeavingHouse;
    public DateTime AiCycleStartedAt;
}
```

El juego añadirá un contador de puntos obtenidos desde la última reaparición para evaluar los umbrales sin alterar el marcador acumulado.

```csharp
private int dotsEatenThisLife;
private static readonly (int X, int Y, string Kind, int ReleaseScore)[] GhostStarts;
```

Las posiciones iniciales serán rojo `(13,14)`, rosa `(14,14)`, cian `(13,15)` y naranja `(14,15)`.

## Implementation plan

1. Actualizar `GhostStarts` y el estado de `Actor` en `Game/PacmanGame.cs` para representar los cuatro colores, sus posiciones, sus umbrales y sus estados de salida sin modificar el contrato `GameFrame`.
2. Añadir el contador de puntos de la vida actual y liberar en orden a los fantasmas cuando alcance los umbrales `0`, `100`, `300` y `600`.
3. Implementar una ruta fija desde cada posición interior hasta la puerta rosa para cada fantasma liberado, permitiendo que los fantasmas atraviesen la puerta y manteniendo a Pac-Man bloqueado.
4. Sustituir la decisión genérica de fantasmas por objetivos por tipo: rojo persigue la posición actual de Pac-Man, rosa apunta cuatro casillas delante, cian alterna cada siete segundos reales entre Pac-Man y la esquina abajo-derecha, y naranja persigue a ocho o más casillas Manhattan y se retira a la esquina abajo-izquierda cuando está más cerca.
5. Aplicar las esquinas clásicas como destinos de retirada: rojo arriba-derecha, rosa arriba-izquierda, cian abajo-derecha y naranja abajo-izquierda.
6. Limitar la detección de colisión a los fantasmas liberados y reiniciar posiciones, contador de puntos de vida, estados de liberación y ciclos de IA después de perder una vida.
7. Actualizar `wwwroot/js/pacman.js` para renderizar los cuatro fantasmas, en orden, con rojo, rosa, cian y naranja.

## Acceptance criteria

- [ ] `dotnet build open-pacman.csproj` finaliza correctamente.
- [ ] `PacmanGame` crea exactamente cuatro fantasmas con los tipos rojo, rosa, cian y naranja.
- [ ] Los fantasmas comienzan en `(13,14)`, `(14,14)`, `(13,15)` y `(14,15)` respectivamente.
- [ ] El rojo queda liberado al comenzar una vida y los otros fantasmas permanecen dentro hasta alcanzar sus umbrales de puntos de la vida actual.
- [ ] Rosa, cian y naranja se liberan al alcanzar 100, 300 y 600 puntos de la vida actual respectivamente.
- [ ] Un fantasma liberado sale por una ruta fija y atraviesa la puerta antes de usar su IA.
- [ ] Pac-Man no puede atravesar la puerta rosa.
- [ ] El rojo elige rutas que reducen la distancia a la posición actual de Pac-Man en los cruces disponibles.
- [ ] El rosa elige rutas hacia un objetivo situado cuatro casillas delante de Pac-Man en su dirección actual.
- [ ] El cian alterna cada siete segundos reales entre perseguir a Pac-Man y dirigirse a la esquina abajo-derecha desde su liberación.
- [ ] El naranja persigue a Pac-Man a una distancia Manhattan de ocho o más casillas y se dirige a la esquina abajo-izquierda a una distancia menor de ocho casillas.
- [ ] El renderizador asigna los colores rojo, rosa, cian y naranja a los cuatro fantasmas en ese orden.
- [ ] Una colisión con cualquier fantasma liberado reduce una vida.
- [ ] Una colisión con un fantasma dentro de la casa no reduce una vida.
- [ ] Tras perder una vida, se reinician las posiciones, los ciclos de IA y los puntos de liberación de la vida actual, mientras el marcador total se conserva.

## Decisions

- **Yes:** Cuatro patrones clásicos simplificados. Diferencian a los fantasmas sin ampliar el alcance a todas las reglas originales de Pac-Man.
- **Yes:** Rojo persigue agresivamente la posición actual de Pac-Man. Cumple el requisito de un perseguidor directo.
- **Yes:** Rosa apunta cuatro casillas por delante de Pac-Man. Mantiene una emboscada predecible y verificable.
- **Yes:** Cian alterna por tiempo real cada siete segundos. Evita que la fluctuación de fotogramas cambie su comportamiento.
- **Yes:** Naranja cambia por distancia Manhattan de ocho casillas. Produce un patrón de persecución y retirada distinguible.
- **Yes:** Umbrales por puntos de la vida actual `0/100/300/600`. Reinicia la secuencia después de perder una vida sin borrar el marcador.
- **Yes:** Los fantasmas salen por una ruta visible a través de la puerta. Evita que aparezcan abruptamente fuera de la casa.
- **No:** Salida cada 1.5 segundos. Se descartó a favor de la liberación por puntos.
- **No:** Modo vulnerable y poderes. Requieren reglas de puntuación, temporizadores y estados adicionales que pertenecen a otra especificación.
- **No:** Pruebas automatizadas. La verificación acordada para esta especificación es exclusivamente la compilación del proyecto.

## Risks

| Risk | Mitigation |
| --- | --- |
| Las posiciones interiores o la ruta fija chocan con muros del laberinto. | Validar las coordenadas contra `MazeRows` y usar únicamente casillas transitables y la puerta para fantasmas. |
| El reloj de siete segundos no se reinicia correctamente tras perder una vida. | Reinicializar `AiCycleStartedAt` al restablecer las posiciones. |
| Un fantasma encerrado activa una colisión. | Incluir el estado `Released` en la condición de detección de colisiones. |

## What is **not** in this spec

- Modo vulnerable, poderes o fantasmas comibles.
- Sonidos, dificultad, niveles o cambios de laberinto.
- Pruebas automatizadas o cambios a la interfaz fuera del renderizado de los cuatro fantasmas.

Each excluded item belongs in its own future spec.
