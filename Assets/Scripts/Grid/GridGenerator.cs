using UnityEngine;

namespace PathfindingDemo
{
    public class GridGenerator : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float tileSize = 1f;

        [Header("Tile Prefab")]
        [SerializeField] private GameObject tilePrefab;

        [Header("Materials")]
        [SerializeField] private Material traversableMaterial;
        [SerializeField] private Material obstacleMaterial;
        [SerializeField] private Material coverMaterial;

        private Grid grid;
        private Transform tilesParent;

        public Grid Grid => grid;

        private void Awake()
        {
            CreateTilesParent();
        }

        private void Start()
        {
            if (tilePrefab == null)
            {
                Debug.LogError("GridGenerator: Tile prefab is not assigned!");
                return;
            }

            GenerateGrid();
        }

        private void CreateTilesParent()
        {
            var tilesParentObject = new GameObject("Tiles");
            tilesParentObject.transform.SetParent(transform);
            tilesParent = tilesParentObject.transform;
        }

        public void GenerateGrid()
        {
            if (tilePrefab == null)
            {
                Debug.LogError("GridGenerator: Cannot generate grid without tile prefab!");
                return;
            }

            ClearExistingTiles();

            grid = new Grid(gridWidth, gridHeight, tileSize);

            SpawnTileObjects();
            ApplyMaterials();
        }

        private void ClearExistingTiles()
        {
            if (tilesParent != null)
            {
                for (int i = tilesParent.childCount - 1; i >= 0; i--)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(tilesParent.GetChild(i).gameObject);
                    }
                    else
                    {
                        DestroyImmediate(tilesParent.GetChild(i).gameObject);
                    }
                }
            }
        }

        private void SpawnTileObjects()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var tile = grid.GetTile(x, y);
                    var worldPosition = grid.GetWorldPosition(new Vector2Int(x, y));

                    var tileObject = Instantiate(tilePrefab, worldPosition, Quaternion.identity, tilesParent);
                    tileObject.name = $"Tile_{x}_{y}";

                    tile.TileObject = tileObject;

                    // Add a TileComponent for easy tile reference
                    var tileComponent = tileObject.GetComponent<TileComponent>();
                    if (tileComponent == null)
                    {
                        tileComponent = tileObject.AddComponent<TileComponent>();
                    }
                    tileComponent.Initialize(tile);
                }
            }
        }

        private void ApplyMaterials()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var tile = grid.GetTile(x, y);
                    ApplyMaterialToTile(tile);
                }
            }
        }

        private void ApplyMaterialToTile(TileData tile)
        {
            if (tile?.TileObject == null) return;

            var renderer = tile.TileObject.GetComponent<Renderer>();
            if (renderer == null) return;

            Material materialToApply = tile.Type switch
            {
                TileType.Traversable => traversableMaterial,
                TileType.Obstacle => obstacleMaterial,
                TileType.Cover => coverMaterial,
                _ => traversableMaterial
            };

            if (materialToApply != null)
            {
                renderer.material = materialToApply;
            }
        }

        public void SetTileType(int x, int y, TileType type)
        {
            if (grid == null) return;

            grid.SetTileType(x, y, type);
            var tile = grid.GetTile(x, y);
            ApplyMaterialToTile(tile);
        }

        public void SetTileType(Vector2Int position, TileType type)
        {
            SetTileType(position.x, position.y, type);
        }

        public void ResizeGrid(int newWidth, int newHeight)
        {
            gridWidth = newWidth;
            gridHeight = newHeight;
            GenerateGrid();
        }

        public TileData GetTileAtWorldPosition(Vector3 worldPosition)
        {
            return grid?.GetTileAtWorldPosition(worldPosition);
        }

        // Editor helpers
        private void OnValidate()
        {
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
            tileSize = Mathf.Max(0.1f, tileSize);
        }

        private void OnDrawGizmos()
        {
            if (grid == null) return;

            Gizmos.color = Color.white;
            for (int x = 0; x <= gridWidth; x++)
            {
                Vector3 start = new Vector3(x * tileSize, 0f, 0f);
                Vector3 end = new Vector3(x * tileSize, 0f, gridHeight * tileSize);
                Gizmos.DrawLine(start, end);
            }

            for (int y = 0; y <= gridHeight; y++)
            {
                Vector3 start = new Vector3(0f, 0f, y * tileSize);
                Vector3 end = new Vector3(gridWidth * tileSize, 0f, y * tileSize);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}