using UnityEngine;

namespace PathfindingDemo
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private LayerMask tileLayerMask = -1;

        [Header("Unit Prefabs")]
        [SerializeField] private GameObject playerUnitPrefab;
        [SerializeField] private GameObject enemyUnitPrefab;

        private Camera playerCamera;
        private TileComponent hoveredTileComponent;
        private GridGenerator gridGenerator;
        private PathVisualizer pathVisualizer;
        private UnitComponent playerUnit;

        private void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
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
            if (Input.GetMouseButtonDown(0) && hoveredTileComponent != null)
            {
                hoveredTileComponent.CycleTileType();
            }

            if (Input.GetMouseButtonDown(1) && hoveredTileComponent != null)
            {
                ShowPathToTile(hoveredTileComponent.TileData);
            }
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
                unitComponent = unitObject.AddComponent<UnitComponent>();
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

        private void ShowPathToTile(TileData targetTile)
        {
            if (playerUnit == null || playerUnit.CurrentTile == null)
            {
                Debug.LogWarning("PlayerController: No player unit found for pathfinding");
                pathVisualizer?.HidePath();
                return;
            }

            if (targetTile == null)
            {
                pathVisualizer?.HidePath();
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
                return;
            }

            // Show path with range validation
            pathVisualizer?.ShowPath(path, maxRange, pathType);

            // Log path information
            bool inRange = PathfindingService.IsPathInRange(path, maxRange);
            Debug.Log($"Path found: {pathType} path with {path.Count} tiles " +
                     $"(Range: {maxRange}, In range: {inRange})");
        }
    }
}