using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PathfindingDemo
{
    [RequireComponent(typeof(UnitManager))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private LayerMask tileLayerMask = -1;
        [SerializeField] private float paintDelay = 0.2f;

        // Path visualization events
        public static event Action<List<TileData>, PathType, int> OnPathCalculated;
        public static event Action<List<TileData>, int, int> OnMoveToAttackPathCalculated;
        public static event Action OnPathHidden;

        private Camera playerCamera;
        private TileComponent hoveredTileComponent;
        private GridGenerator gridGenerator;
        private UnitManager unitManager;
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

            // Initialize unit manager
            unitManager = GetComponent<UnitManager>();
            unitManager.Initialize(gridGenerator);

            // Ensure path visualizer exists
            var pathVisualizer = FindFirstObjectByType<PathVisualizer>();
            if (pathVisualizer == null)
            {
                Debug.LogError("PlayerController: PathVisualizer not found in scene. Please add PathVisualizer component to a GameObject.");
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

            // Update path visualization if we have an active path
            RefreshCurrentPath();
        }

        private void StopPainting()
        {
            isPainting = false;
            lastPaintedTile = null;
        }

        private void SpawnUnits()
        {
            unitManager.SpawnUnits();
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
            var playerUnit = unitManager.PlayerUnit;
            if (playerUnit == null || playerUnit.CurrentTile == null)
            {
                Debug.LogWarning("PlayerController: No player unit found for pathfinding");
                OnPathHidden?.Invoke();
                return;
            }

            // Block path drawing while unit is moving
            if (playerUnit.IsMoving)
            {
                OnPathHidden?.Invoke();
                lastShownPath = null;
                return;
            }

            if (targetTile == null)
            {
                OnPathHidden?.Invoke();
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
                OnPathHidden?.Invoke();
                lastShownPath = null;
                return;
            }

            // Store the path for potential movement
            lastShownPath = new List<TileData>(path);

            // Show path with appropriate visualization using events
            if (pathType == PathType.Attack)
            {
                // Check if this is a move-to-attack scenario (enemy out of direct attack range)
                bool directAttackInRange = PathfindingService.IsPathInRange(path, playerUnit.AttackRange);

                if (!directAttackInRange)
                {
                    // Show move-to-attack path with 3 segments
                    OnMoveToAttackPathCalculated?.Invoke(path, playerUnit.MoveRange, playerUnit.AttackRange);
                }
                else
                {
                    // Standard attack path visualization
                    OnPathCalculated?.Invoke(path, pathType, maxRange);
                }
            }
            else
            {
                // Standard movement path visualization
                OnPathCalculated?.Invoke(path, pathType, maxRange);
            }

            // Log path information
            bool inRange = PathfindingService.IsPathInRange(path, maxRange);
            Debug.Log($"Path found: {pathType} path with {path.Count} tiles " +
                     $"(Range: {maxRange}, In range: {inRange})");
        }

        private void TryMovePlayerToTile(TileData targetTile)
        {
            var playerUnit = unitManager.PlayerUnit;
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

            // Check if this is an attack action
            if (targetTile.IsOccupied() && targetTile.OccupiedBy is UnitComponent targetUnit && targetUnit.Type == UnitType.Enemy)
            {
                TryAttackEnemy(targetTile, targetUnit);
                return;
            }

            // Regular movement logic
            var movementPath = PathfindingService.GetInRangePath(lastShownPath, playerUnit.MoveRange);

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
                OnPathHidden?.Invoke();
            }
            else
            {
                Debug.LogWarning("PlayerController: Failed to start movement");
            }
        }

        private void TryAttackEnemy(TileData targetTile, UnitComponent enemyUnit)
        {
            var playerUnit = unitManager.PlayerUnit;

            // Check if we can attack directly from current position
            bool canAttackDirectly = PathfindingService.IsPathInRange(lastShownPath, playerUnit.AttackRange);

            if (canAttackDirectly)
            {
                // Direct attack - enemy is in range
                Debug.Log($"PlayerController: Attacking enemy at {targetTile.Position}");
                unitManager.KillEnemy(enemyUnit);

                // Clear the stored path since attack is complete
                lastShownPath = null;
                lastClickedTile = null;
                OnPathHidden?.Invoke();
            }
            else
            {
                // Check if this is a move-to-attack scenario (we have a movement path that can get us in attack range)
                var movementPath = PathfindingService.GetInRangePath(lastShownPath, playerUnit.MoveRange);

                if (movementPath.Count >= 2)
                {
                    // We can move - check if after movement we can attack
                    var endPosition = movementPath[movementPath.Count - 1];
                    int distanceToEnemyAfterMove = Mathf.Abs(endPosition.Position.x - targetTile.Position.x) +
                                                  Mathf.Abs(endPosition.Position.y - targetTile.Position.y);

                    if (distanceToEnemyAfterMove <= playerUnit.AttackRange)
                    {
                        // Move then attack
                        Debug.Log($"PlayerController: Moving to position for attack");

                        if (playerUnit.StartMovement(movementPath))
                        {
                            StartCoroutine(WaitForMovementThenAttack(enemyUnit));

                            // Clear UI state
                            lastShownPath = null;
                            lastClickedTile = null;
                            OnPathHidden?.Invoke();
                        }
                        else
                        {
                            Debug.LogWarning("PlayerController: Failed to start movement");
                        }
                    }
                    else
                    {
                        // Just move closer
                        Debug.Log($"PlayerController: Moving closer to enemy");

                        if (playerUnit.StartMovement(movementPath))
                        {
                            // Clear UI state
                            lastShownPath = null;
                            lastClickedTile = null;
                            OnPathHidden?.Invoke();
                        }
                    }
                }
                else
                {
                    Debug.Log("PlayerController: Cannot move or attack - no valid path");
                }
            }
        }


        private System.Collections.IEnumerator WaitForMovementThenAttack(UnitComponent enemyUnit)
        {
            var playerUnit = unitManager.PlayerUnit;
            // Wait for movement to complete
            while (playerUnit.IsMoving)
            {
                yield return null;
            }

            // Check if enemy still exists and attack
            if (enemyUnit != null)
            {
                Debug.Log($"PlayerController: Movement complete, attacking enemy");
                unitManager.KillEnemy(enemyUnit);
            }
        }

        private void RefreshCurrentPath()
        {
            // Only refresh if we have an active target tile to show path to
            if (lastClickedTile != null)
            {
                ShowPathToTile(lastClickedTile);
            }
        }
    }
}