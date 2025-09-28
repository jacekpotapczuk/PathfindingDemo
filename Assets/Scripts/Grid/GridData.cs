using UnityEngine;

namespace PathfindingDemo
{
    /// <summary>
    /// Manages grid data storage and provides grid-based coordinate operations.
    /// </summary>
    [System.Serializable]
    public class GridData
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private float tileSize;

        private TileData[,] tiles;

        public int Width => width;
        public int Height => height;
        public float TileSize => tileSize;

        public GridData(int width, int height, float tileSize = 1f)
        {
            this.width = width;
            this.height = height;
            this.tileSize = tileSize;
            InitializeTiles();
        }

        private void InitializeTiles()
        {
            tiles = new TileData[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    tiles[x, y] = new TileData(new Vector2Int(x, y));
                }
            }
        }

        public TileData GetTile(int x, int y)
        {
            if (IsValidPosition(x, y))
            {
                return tiles[x, y];
            }
            return null;
        }

        public TileData GetTile(Vector2Int position)
        {
            return GetTile(position.x, position.y);
        }

        public void SetTileType(int x, int y, TileType type)
        {
            var tile = GetTile(x, y);
            if (tile != null)
                tile.Type = type;
        }

        public void SetTileType(Vector2Int position, TileType type)
        {
            SetTileType(position.x, position.y, type);
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public bool IsValidPosition(Vector2Int position)
        {
            return IsValidPosition(position.x, position.y);
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x * tileSize, 0f, y * tileSize);
        }

        public Vector3 GetWorldPosition(Vector2Int position)
        {
            return GetWorldPosition(position.x, position.y);
        }

        public Vector2Int GetGridPosition(Vector3 worldPosition)
        {
            var x = Mathf.RoundToInt(worldPosition.x / tileSize);
            var z = Mathf.RoundToInt(worldPosition.z / tileSize);
            return new Vector2Int(x, z);
        }

        public TileData[,] GetAllTiles()
        {
            return tiles;
        }

        public void ResizeGrid(int newWidth, int newHeight)
        {
            var oldTiles = tiles;
            var oldWidth = width;
            var oldHeight = height;

            width = newWidth;
            height = newHeight;
            InitializeTiles();

            for (var x = 0; x < Mathf.Min(oldWidth, newWidth); x++)
            {
                for (var y = 0; y < Mathf.Min(oldHeight, newHeight); y++)
                {
                    if (oldTiles[x, y] != null)
                        tiles[x, y].Type = oldTiles[x, y].Type;
                }
            }
        }
    }
}