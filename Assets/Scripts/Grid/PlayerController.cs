using UnityEngine;
using System.Collections.Generic;

namespace PathfindingDemo
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private LayerMask tileLayerMask = -1;
        [SerializeField] private float paintDelay = 0.2f;

        [Header("Unit Prefabs")]
        [SerializeField] private GameObject playerUnitPrefab;
        [SerializeField] private GameObject enemyUnitPrefab;

        private Camera playerCamera;
        private TileComponent hoveredTileComponent;
        private GridGenerator gridGenerator;
        private PathVisualizer pathVisualizer;
        private UnitComponent playerUnit;
        private CameraController cameraController;

        private bool isPainting;
        private TileComponent lastPaintedTile;
        private float lastPaintTime;

        private TileData lastClickedTile;
        private List<TileData> lastShownPath;

        private void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }

            // Add camera controller if not already present
            if (playerCamera != null)
            {
                cameraController = playerCamera.GetComponent<CameraController>();
                if (cameraController == null)
                {
                    cameraController = playerCamera.gameObject.AddComponent<CameraController>();
                }
            }

            gridGenerator = FindFirstObjectByType<GridGenerator>();

            // Create or find path visualizer
            pathVisualizer = FindFirstObjectByType<PathVisualizer>();
            if (pathVisualizer == null)
            {
                var visualizerObject = new GameObject("PathVisualizer");
                pathVisualizer = visualizerObject.AddComponent<PathVisualizer>();
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Wait a frame for grid to be generated, then spawn units
            Invoke(nameof(SpawnUnits), 0.1f);
        }

        private void Update()
        {
            UpdateHoveredTile();
            HandleInput();
        }

        private void UpdateHoveredTile()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, tileLayerMask))
            {
                var tileComponent = hit.collider.GetComponent<TileComponent>();
                if (tileComponent != null)
                {
                    hoveredTileComponent = tileComponent;
                }
                else
                {
                    hoveredTileComponent = null;
                }
            }
            else
            {
                hoveredTileComponent = null;
            }
        }

        private void HandleInput()
        {
            HandleTilePainting();

            if (Input.GetMouseButtonDown(1) && hoveredTileComponent != null)
            {
                HandleRightClick(hoveredTileComponent.TileData);
            }
        }

        private void HandleTilePainting()
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartPainting();
            }
            else if (Input.GetMouseButton(0) && isPainting)
            {
                ContinuePainting();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                StopPainting();
            }
        }

        private void StartPainting()
        {
            if (hoveredTileComponent == null) return;

            isPainting = true;
            PaintCurrentTile();
        }

        private void ContinuePainting()
        {
            if (hoveredTileComponent == null) return;

            bool movedToNewTile = hoveredTileComponent != lastPaintedTile;
            bool enoughTimeElapsed = Time.time - lastPaintTime >= paintDelay;

            if (movedToNewTile || enoughTimeElapsed)
            {
                PaintCurrentTile();
            }
        }

        private void PaintCurrentTile()
        {
            if (hoveredTileComponent == null) return;

            hoveredTileComponent.CycleTileType();
            lastPaintedTile = hoveredTileComponent;
            lastPaintTime = Time.time;
        }

        private void StopPainting()
        {
            isPainting = false;
            lastPaintedTile = null;
        }

        private void SpawnUnits()
        {
            if (gridGenerator?.Grid == null)
            {
                Debug.LogError("PlayerController: Grid not available for unit spawning");
                return;
            }

            SpawnUnit(playerUnitPrefab, UnitType.Player);
            SpawnUnit(enemyUnitPrefab, UnitType.Enemy);
        }

        private void SpawnUnit(GameObject unitPrefab, UnitType unitType)
        {
            if (unitPrefab == null)
            {
                Debug.LogWarning($"PlayerController: No prefab assigned for {unitType} unit");
                return;
            }

            var randomTile = GetRandomTraversableTile();
            if (randomTile == null)
            {
                Debug.LogError($"PlayerController: No available tiles for {unitType} unit");
                return;
            }

            var unitObject = Instantiate(unitPrefab);
            var unitComponent = unitObject.GetComponent<UnitComponent>();

            if (unitComponent == null)
            {
                Debug.LogError($"PlayerController: Unit prefab {unitPrefab.name} must have UnitComponent attached");
                return;
            }

            // // Set unit type through reflection or make the field public
            // var unitTypeField = typeof(UnitComponent).GetField("unitType",
            //     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // unitTypeField?.SetValue(unitComponent, unitType);

            unitComponent.SetTile(randomTile);

            // Store reference to player unit
            if (unitType == UnitType.Player)
            {
                playerUnit = unitComponent;
            }
        }

        private TileData GetRandomTraversableTile()
        {
            var grid = gridGenerator.Grid;
            var availableTiles = new System.Collections.Generic.List<TileData>();

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var tile = grid.GetTile(x, y);
                    if (tile != null && tile.CanBeOccupied())
                    {
                        availableTiles.Add(tile);
                    }
                }
            }

            if (availableTiles.Count == 0)
                return null;

            return availableTiles[Random.Range(0, availableTiles.Count)];
        }

        private void HandleRightClick(TileData targetTile)
        {
            if (targetTile == lastClickedTile && lastShownPath != null && lastShownPath.Count > 0)
            {
                // Same tile clicked twice - attempt movement
                TryMovePlayerToTile(targetTile);
            }
            else
            {
                // First click or different tile - show path
                ShowPathToTile(targetTile);
                lastClickedTile = targetTile;
            }
        }

        private void ShowPathToTile(TileData targetTile)
        {
            if (playerUnit == null || playerUnit.CurrentTile == null)
            {
                Debug.LogWarning("PlayerController: No player unit found for pathfinding");
                pathVisualizer?.HidePath();
                return;
            }

            // Block path drawing while unit is moving
            if (playerUnit.IsMoving)
            {
                pathVisualizer?.HidePath();
                lastShownPath = null;
                return;
            }

            if (targetTile == null)
            {
                pathVisualizer?.HidePath();
                lastShownPath = null;
                return;
            }

            var startTile = playerUnit.CurrentTile;

            // Determine path type: attack if tile has enemy, movement otherwise
            var pathType = PathType.Movement;
            int maxRange = playerUnit.MoveRange;

            if (targetTile.IsOccupied() && targetTile.OccupiedBy is UnitComponent targetUnit)
            {
                if (targetUnit.Type == UnitType.Enemy)
                {
                    pathType = PathType.Attack;
                    maxRange = playerUnit.AttackRange;
                }
            }

            // Calculate path using new pathfinding service
            var path = PathfindingService.FindPath(gridGenerator.Grid, startTile, targetTile, pathType);

            if (path.Count == 0)
            {
                Debug.Log($"No path found from {startTile.Position} to {targetTile.Position}");
                pathVisualizer?.HidePath();
                lastShownPath = null;
                return;
            }

            // Store the path for potential movement
            lastShownPath = new List<TileData>(path);

            // Show path with range validation
            pathVisualizer?.ShowPath(path, maxRange, pathType);

            // Log path information
            bool inRange = PathfindingService.IsPathInRange(path, maxRange);
            Debug.Log($"Path found: {pathType} path with {path.Count} tiles " +
                     $"(Range: {maxRange}, In range: {inRange})");
        }

        private void TryMovePlayerToTile(TileData targetTile)
        {
            if (playerUnit == null || lastShownPath == null || lastShownPath.Count == 0)
            {
                Debug.LogWarning("PlayerController: Cannot move - no valid path available");
                return;
            }

            if (playerUnit.IsMoving)
            {
                Debug.Log("PlayerController: Cannot move - unit is already moving");
                return;
            }

            // Check if this is a movement path (not attack)
            var pathType = PathType.Movement;
            int maxRange = playerUnit.MoveRange;

            if (targetTile.IsOccupied() && targetTile.OccupiedBy is UnitComponent targetUnit)
            {
                if (targetUnit.Type == UnitType.Enemy)
                {
                    Debug.Log("PlayerController: Cannot move to attack target - use attack action instead");
                    return;
                }
            }

            // Get only the in-range portion of the path (allows partial movement)
            var movementPath = PathfindingService.GetInRangePath(lastShownPath, maxRange);

            // Check if we have any valid movement
            if (movementPath.Count < 2)
            {
                Debug.Log("PlayerController: Cannot move - no valid movement path available");
                return;
            }

            // Start movement
            if (playerUnit.StartMovement(movementPath))
            {
                bool isPartialMovement = movementPath.Count < lastShownPath.Count;
                string movementType = isPartialMovement ? "partial" : "full";
                Debug.Log($"PlayerController: Starting {movementType} movement along path with {movementPath.Count} tiles");

                // Clear the stored path since we're now moving
                lastShownPath = null;
                lastClickedTile = null;

                // Hide the path visualization during movement
                pathVisualizer?.HidePath();
            }
            else
            {
                Debug.LogWarning("PlayerController: Failed to start movement");
            }
        }
    }
}