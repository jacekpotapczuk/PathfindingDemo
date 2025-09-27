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

        private void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }

            gridGenerator = FindFirstObjectByType<GridGenerator>();
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
    }
}