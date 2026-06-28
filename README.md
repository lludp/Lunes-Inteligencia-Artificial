# Lunes — Inteligencia Artificial (TPO)

Juego de sigilo/terror en primera-tercera persona donde el jugador debe explorar un
nivel, recolectar objetos/documentos y escapar, evitando a varios enemigos con IA.

- **Género:** sigilo / survival.
- **Objetivo:** completar los objetivos del nivel (llaves, documentos, salida) sin ser
  alcanzado por los enemigos.
- **Escena principal:** `Assets/Scenes/GameScene.unity`.

## Controles básicos
- **WASD / Mouse:** mover y mirar.
- **E / Click:** interactuar (objetos, llaves, documentos).
- **Esc:** pausa.

---

## Arquitectura general de IA

El proyecto separa los tres niveles de IA pedidos por la consigna:

| Nivel | Qué resuelve | Implementación |
|-------|--------------|----------------|
| **Decisión** | Qué hacer (patrullar, perseguir, huir, atacar) | FSM + Line of Sight (`LineOfSight.cs`) |
| **Pathfinding** | Por dónde ir en el mapa esquivando obstáculos | **A\*** propio sobre una grilla (`Pathfinding/`) |
| **Steering** | Cómo desplazarse localmente entre puntos | `SteeringBehaviours.cs` (Seek, Flee, Arrive, Pursue, Evade, Wander) |

### Sistema de Pathfinding (A\*)
Carpeta `Assets/Scritps/Pathfinding/`:
- `PathNode.cs` — nodo del grafo como **GameObject real** en la escena (objeto en la
  jerarquía, con gizmo y su lista de vecinos conectados).
- `PathfindingGrid.cs` — genera/administra el grafo: siembra nodos sobre el NavMesh de la
  casa, los conecta entre vecinos cercanos (sin atravesar paredes) y expone `FindPath()`,
  que corre A\* sobre esos nodos y devuelve waypoints.
- `AStarPathfinder.cs` — algoritmo **A\*** (g + h) implementado a mano.
- `PathPriorityQueue.cs` — cola de prioridad (min-heap) que usa A\*.

El A\* decide la ruta sobre el grafo de nodos; el agente luego recorre los waypoints con
steering. (No se usa el pathfinding interno del NavMesh para navegar: el `NavMeshAgent` se
emplea solo como "mover" para mantener al agente sobre el piso navegable.)

### Steering Behaviors
`SteeringBehaviours.cs` implementa **Seek, Flee, Arrive, Pursue, Evade y Wander**
(más de los 3 pedidos). El Stalker usa Seek/Arrive para seguir el camino A\* y Pursue
para perseguir al jugador en línea directa.

---

## Enemigos / agentes (qué sistemas usa cada uno)

| Agente | Decisión | Pathfinding | Steering | Comportamientos |
|--------|----------|-------------|----------|-----------------|
| **Stalker** (`StalkerController.cs`) | **FSM** (Patrol/Chase/Search/Attack) + LoS | **A\*** propio (grilla) | **Seek, Arrive, Pursue** | Patrulla, persigue rodeando obstáculos, busca en la última posición conocida, ataca |
| **Coward** (`CowardController.cs`) | FSM (Patrol/Flee/Scared) + LoS | NavMesh + waypoints | Flee | Patrulla, huye al ver al jugador, se esconde asustado |
| **Guard** (`GuardController.cs`) | Reactivo + LoS | NavMesh | — | Quieto vigilando; persigue y se detiene al encarar al jugador |
| **PuppetMaster** (`PuppetMaster.cs`) | Selección por puntaje | Salto entre "muñecos" | LookAt | Cambia de cuerpo según distancia/visión del jugador y ataca |

> Los tres+ agentes actúan de forma diferenciada (objetivos, movimiento y reacción al
> jugador distintos), cumpliendo el requisito de ≥3 agentes con comportamientos diversos.

---

## Grafo de nodos (ya configurado en `GameScene`)
En la escena existe el GameObject **`PathfindingGrid`** con ~**1500 nodos `PathNode`** como
hijos (objetos `Node_i`), generados automáticamente sobre **todo el NavMesh horneado de la
casa, en sus dos plantas** (primer piso ≈ y 26.5 y planta baja ≈ y 17) y conectados entre
vecinos (incluso a través de escaleras). El Stalker los toma vía `PathfindingGrid.Instance`.

Con **`Use Nav Mesh Bounds`** activado, el generador siembra sobre los límites del NavMesh
y barre en altura (`Vertical Step`) para captar todos los pisos; así no quedan zonas sin
nodos. `Node Spacing` controla la densidad y `Max Nodes` es un tope de seguridad.

Gizmos en la Scene view: cada nodo = esfera verde, líneas celestes = conexiones entre
vecinos, amarillo = último camino A\* calculado.

**Para regenerar/ajustar:** seleccionar `PathfindingGrid`, ajustar `Node Spacing`
(más chico = más nodos) o `Vertical Step`, y en el menú contextual del componente usar
**`Generate Nodes`** (o **`Clear Nodes`**). Regenerar también tras re-hornear el NavMesh.
