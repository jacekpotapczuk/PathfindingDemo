# Pathfinding Demo

This project is a simple pathfinding demo.  
It demonstrates A\* pathfinding on a grid with traversable tiles, obstacles, and cover tiles, as well as player/enemy units with movement and attack ranges.

The main goal is to showcase a clean pathfinding implementation, unit movement with animations, and interactive map editing.

---

## Features

- **Grid-based map**
  - Tiles can be:
    - **Traversable (0)**
    - **Obstacle (1)**
    - **Cover (2)**
  - Each move has equal cost (`1`).

- **Units**
  - Player and enemy can only be placed on **traversable tiles**.
  - Player has **MoveRange** and **AttackRange** parameters.
  - Player:
    - Moves only if path length ≤ `MoveRange`.
    - Attacks only if path length ≤ `AttackRange`.
    - Can attack through traversable **and cover tiles**.

- **Pathfinding**
  - Select a tile to see the optimal path from player:
    - If tile is empty → show movement path.
    - If tile has enemy → show attack path.
    - If path is longer than allowed range → full path is shown, with **segments colored by turns**.

- **Extra Credit**
  - Player movement with animation.
  - **Click to move** → player runs along computed path.
  - **Click to attack** → player moves to attack position, then destroys enemy.
  - If enemy is out of attack range → path to closest attackable tile is shown.

- **Other features**
  - Adjustable grid size (in `GridGenerator`).
  - MoveRange and AttackRange can be set in **unit prefabs** and **runtime UI**.
  - Tile painting mode to add/remove obstacles or covers.
  - Utility button to spawn additional enemies.
  - Audio for footsteps (animation events + starter content sounds).

---

## Pathfinding Algorithm Choice

I considered a couple of algorithms:

- **BFS (Breadth-First Search)**
  - Simple, low overhead.
  - Works well for uniform cost grids.
  - However, it cannot easily handle variable costs if needed later.

- **JPS (Jump Point Search)**
  - More efficient on large, open grids.
  - Overkill if grid isn't enormous (and that would bring other problems like each tile having it's own Game Object).

- A\* (chosen for simple movement paths)
  - Standard for pathfinding.
  - Efficient for standard-sized grids in games.
  - Outperforms BFS in average cases with heuristic guidance.
  - Used for basic movement pathfinding and single-target paths.

- **Bidirectional BFS** (chosen for movement + attack scenarios)
  - Pretty late I realized how bad A* is for searching move+attack paths. So I switched to Bidirectional BFS (over x10 performance improvement) 
  - Optimized algorithm for finding best attack positions.
  - One BFS expands from enemy position to find all valid attack tiles.
  - Another BFS expands from player position until it intersects with attack tiles.
  - Provides automatic early termination when searches meet.

**Adjustments made:**
- A\* implementation on orthogonal grid for movement pathfinding.
- Bidirectional BFS for complex movement + attack position finding.
- Path visualization extended to support multi-turn moves → paths beyond `MoveRange`/`AttackRange` are segmented and color-coded to indicate turn requirements.

---

## Limitations

- The requirement to show a path with "in-range" and "out-of-range" segments wasn’t implemented exactly as described.
  - Instead, I implemented a **multi-move + attack path system**:
    - Paths are drawn fully.
    - Coloring alternates based on turns needed.
    - This visually communicates when the unit would be able to attack.

---

## Architecture & Implementation Notes

- **Simple architecture**
  - Used `FindObjectOfType<T>()` for references.
  - Direct `Input.GetKey` for controls (to save development time).
  - Could be extended with event-driven systems or dependency injection for scalability.

- **Runtime editing**
  - Grid size adjustable in `GridGenerator` (runtime resizing was annoying, could be added later).
  - Tile painting mode to adjust obstacles/covers.

- **Unit setup**
  - Player parameters (MoveRange, AttackRange) configurable in prefab or runtime UI.
  - Enemies can be spawned via utility button.

- **Visuals & Audio**
  - Default Unity character model.
  - Movement animations integrated.
  - Footstep sounds triggered by animation events.
  
- **Parts of the code were written with Claude Code**
---

## Future Improvements

- Cleaner architecture with event-driven references instead of `FindObjectOfType`.
- Proper runtime grid resizing.
- Smarter controls (e.g., input system, camera improvements).
