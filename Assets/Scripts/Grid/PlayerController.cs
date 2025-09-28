using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PathfindingDemo
{
    /// <summary>
    /// Handles player input for grid interaction, unit movement, and pathfinding visualization.
    /// </summary>
    [RequireComponent(typeof(UnitManager))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private LayerMask tileLayerMask = -1;
        [SerializeField] private float paintDelay = 0.2f;

        // Path visualization events
        public static event Action<List<TileData>, PathType, int> OnPathCalculated;
        public static event Action<List<TileData>, int, int> OnMoveToAttackPathCalculated;
        public static event Action<List<(List<TileData>, int)>, List<TileData>> OnMultiTurnPathCalculated;
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

        // Parameter monitoring for automatic path refresh
        private int lastMoveRange = -1;
        private int lastAttackRange = -1;

        private void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = FindFirstObjectByType<Camera>();

            if (playerCamera != null)
            {
                cameraController = playerCamera.GetComponent<CameraController>();
                if (cameraController == null)
                    cameraController = playerCamera.gameObject.AddComponent<CameraController>();
            }

            gridGenerator = FindFirstObjectByType<GridGenerator>();
            unitManager = GetComponent<UnitManager>();
            unitManager.Initialize(gridGenerator);

            var pathVisualizer = FindFirstObjectByType<PathVisualizer>();
            if (pathVisualizer == null)
                Debug.LogError("PlayerController: PathVisualizer not found in scene. Please add PathVisualizer component to a GameObject.");

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Invoke(nameof(SpawnUnits), 0.1f);
        }

        private void Update()
        {
            UpdateHoveredTile();
            HandleInput();
            MonitorParameterChanges();
        }

        private void MonitorParameterChanges()
        {
            var playerUnit = unitManager?.PlayerUnit;
            if (playerUnit == null) return;

            if (lastMoveRange != playerUnit.MoveRange)
            {
                lastMoveRange = playerUnit.MoveRange;
                RefreshCurrentPath();
            }

            if (lastAttackRange != playerUnit.AttackRange)
            {
                lastAttackRange = playerUnit.AttackRange;
                RefreshCurrentPath();
            }
        }

        private void UpdateHoveredTile()
        {
            if (playerCamera == null) return;

            var ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, tileLayerMask))
            {
                var tileComponent = hit.collider.GetComponent<TileComponent>();
                hoveredTileComponent = tileComponent;
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

            var movedToNewTile = hoveredTileComponent != lastPaintedTile;
            var enoughTimeElapsed = Time.time - lastPaintTime >= paintDelay;

            if (movedToNewTile || enoughTimeElapsed)
                PaintCurrentTile();
        }

        private void PaintCurrentTile()
        {
            if (hoveredTileComponent == null) return;

            hoveredTileComponent.CycleTileType();
            lastPaintedTile = hoveredTileComponent;
            lastPaintTime = Time.time;
            RefreshCurrentPath();
        }

        private void StopPainting()
        {
            isPainting = false;
            lastPaintedTile = null;
        }

        private void SpawnUnits()
        {
            unitManager.SpawnInitialUnits();
            var playerUnit = unitManager?.PlayerUnit;
            if (playerUnit != null)
            {
                lastMoveRange = playerUnit.MoveRange;
                lastAttackRange = playerUnit.AttackRange;
            }
        }


        private void HandleRightClick(TileData targetTile)
        {
            if (targetTile == lastClickedTile && lastShownPath != null && lastShownPath.Count > 0)
                TryMovePlayerToTile(targetTile);
            else
            {
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

            // Check if target is an enemy for attack logic
            if (targetTile.IsOccupied() && targetTile.OccupiedBy is UnitComponent targetUnit && targetUnit.Type == UnitType.Enemy)
            {
                ShowAttackPath(startTile, targetTile, playerUnit);
            }
            else
            {
                ShowMovementPath(startTile, targetTile, playerUnit);
            }
        }

        private void ShowAttackPath(TileData startTile, TileData targetTile, UnitComponent playerUnit)
        {
            // Check if target is within direct attack range
            var directAttackPath = PathfindingService.FindPath(gridGenerator.Grid, startTile, targetTile, PathType.Attack);
            bool directAttackInRange = directAttackPath.Count > 0 && PathfindingService.IsPathInRange(directAttackPath, playerUnit.AttackRange);

            if (directAttackInRange)
            {
                lastShownPath = new List<TileData>(directAttackPath);
                OnPathCalculated?.Invoke(directAttackPath, PathType.Attack, playerUnit.AttackRange);
                return;
            }

            // Find best attack position
            var bestAttackPosition = PathfindingService.FindBestAttackPosition(gridGenerator.Grid, startTile, targetTile, playerUnit.AttackRange);

            if (bestAttackPosition == null)
            {
                OnPathHidden?.Invoke();
                lastShownPath = null;
                return;
            }

            // Calculate multi-turn movement to attack position
            var movementTurns = PathfindingService.FindMultiTurnMovementPath(gridGenerator.Grid, startTile, bestAttackPosition, playerUnit.MoveRange);

            if (movementTurns.Count == 0)
            {
                OnPathHidden?.Invoke();
                lastShownPath = null;
                return;
            }

            // Calculate attack segment from final position
            var finalPosition = movementTurns[movementTurns.Count - 1].segment[movementTurns[movementTurns.Count - 1].segment.Count - 1];
            var attackSegment = PathfindingService.FindPath(gridGenerator.Grid, finalPosition, targetTile, PathType.Attack);

            // Build combined path from pure movement segments
            var combinedPath = new List<TileData>();
            foreach (var (segment, _) in movementTurns)
            {
                combinedPath.AddRange(segment);
            }

            // Build execution path by adding current tile at the start for movement validation
            var executionPath = new List<TileData> { startTile };
            executionPath.AddRange(combinedPath);
            lastShownPath = executionPath;

            OnMultiTurnPathCalculated?.Invoke(movementTurns, attackSegment);
        }

        private void ShowMovementPath(TileData startTile, TileData targetTile, UnitComponent playerUnit)
        {
            // Calculate multi-turn movement path
            var movementTurns = PathfindingService.FindMultiTurnMovementPath(gridGenerator.Grid, startTile, targetTile, playerUnit.MoveRange);

            if (movementTurns.Count == 0)
            {
                OnPathHidden?.Invoke();
                lastShownPath = null;
                return;
            }

            // Build combined path from pure movement segments
            var combinedPath = new List<TileData>();
            foreach (var (segment, _) in movementTurns)
            {
                combinedPath.AddRange(segment);
            }

            // Build execution path by adding current tile at the start for movement validation
            var executionPath = new List<TileData> { startTile };
            executionPath.AddRange(combinedPath);
            lastShownPath = executionPath;

            OnMultiTurnPathCalculated?.Invoke(movementTurns, null);
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

            if (movementPath.Count < 2)
                return;

            if (playerUnit.StartMovement(movementPath))
            {
                lastShownPath = null;
                lastClickedTile = null;
                OnPathHidden?.Invoke();
            }
        }

        private void TryAttackEnemy(TileData targetTile, UnitComponent enemyUnit)
        {
            var playerUnit = unitManager.PlayerUnit;
            var currentPosition = playerUnit.CurrentTile;

            var directAttackPath = PathfindingService.FindPath(gridGenerator.Grid, currentPosition, targetTile, PathType.Attack);
            var canAttackDirectly = directAttackPath.Count > 0 && PathfindingService.IsPathInRange(directAttackPath, playerUnit.AttackRange);

            if (canAttackDirectly)
            {
                unitManager.KillEnemy(enemyUnit);
                lastShownPath = null;
                lastClickedTile = null;
                OnPathHidden?.Invoke();
            }
            else
            {
                var movementPath = PathfindingService.GetInRangePath(lastShownPath, playerUnit.MoveRange);
                if (movementPath.Count >= 2)
                {
                    var endPosition = movementPath[movementPath.Count - 1];
                    var attackPathAfterMove = PathfindingService.FindPath(gridGenerator.Grid, endPosition, targetTile, PathType.Attack);
                    var canAttackAfterMove = attackPathAfterMove.Count > 0 && PathfindingService.IsPathInRange(attackPathAfterMove, playerUnit.AttackRange);

                    if (canAttackAfterMove)
                    {
                        if (playerUnit.StartMovement(movementPath))
                        {
                            StartCoroutine(WaitForMovementThenAttack(enemyUnit, targetTile));
                            lastShownPath = null;
                            lastClickedTile = null;
                            OnPathHidden?.Invoke();
                        }
                    }
                    else
                    {
                        if (playerUnit.StartMovement(movementPath))
                        {
                            lastShownPath = null;
                            lastClickedTile = null;
                            OnPathHidden?.Invoke();
                        }
                    }
                }
            }
        }


        private System.Collections.IEnumerator WaitForMovementThenAttack(UnitComponent enemyUnit, TileData targetTile)
        {
            var playerUnit = unitManager.PlayerUnit;
            while (playerUnit.IsMoving)
                yield return null;

            if (enemyUnit != null && targetTile != null)
            {
                var currentPosition = playerUnit.CurrentTile;
                var attackPath = PathfindingService.FindPath(gridGenerator.Grid, currentPosition, targetTile, PathType.Attack);
                var canAttack = attackPath.Count > 0 && PathfindingService.IsPathInRange(attackPath, playerUnit.AttackRange);

                if (canAttack)
                    unitManager.KillEnemy(enemyUnit);
            }
        }

        private void RefreshCurrentPath()
        {
            if (lastClickedTile != null)
                ShowPathToTile(lastClickedTile);
        }
    }
}