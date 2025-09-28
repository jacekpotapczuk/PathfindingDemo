using System.Collections.Generic;
using UnityEngine;

namespace PathfindingDemo
{
    /// <summary>
    /// Core grid system that manages tile connections and pathfinding logic.
    /// </summary>
    public class Grid
    {
        private GridData gridData;
        private readonly Vector2Int[] orthogonalDirections = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        public GridData Data => gridData;
        public int Width => gridData.Width;
        public int Height => gridData.Height;
        public float TileSize => gridData.TileSize;

        public Grid(int width, int height, float tileSize = 1f)
        {
            gridData = new GridData(width, height, tileSize);
            GenerateNeighborConnections();
        }

        public Grid(GridData data)
        {
            gridData = data;
            GenerateNeighborConnections();
        }

        public TileData GetTile(int x, int y)
        {
            return gridData.GetTile(x, y);
        }

        public TileData GetTile(Vector2Int position)
        {
            return gridData.GetTile(position);
        }

        public void SetTileType(int x, int y, TileType type)
        {
            gridData.SetTileType(x, y, type);
        }

        public void SetTileType(Vector2Int position, TileType type)
        {
            gridData.SetTileType(position, type);
        }

        public bool IsValidPosition(Vector2Int position)
        {
            return gridData.IsValidPosition(position);
        }

        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            return gridData.GetWorldPosition(gridPosition);
        }

        public Vector2Int GetGridPosition(Vector3 worldPosition)
        {
            return gridData.GetGridPosition(worldPosition);
        }
        
        public TileData GetTileAtWorldPosition(Vector3 worldPosition)
        {
            var gridPos = GetGridPosition(worldPosition);
            return GetTile(gridPos);
        }

        public TileData GetRandomTraversableTile()
        {
            var availableTiles = new List<TileData>();
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var tile = GetTile(x, y);
                    if (tile != null && tile.CanBeOccupied())
                        availableTiles.Add(tile);
                }
            }

            if (availableTiles.Count == 0)
                return null;

            return availableTiles[Random.Range(0, availableTiles.Count)];
        }
        
        private void GenerateNeighborConnections()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var currentTile = gridData.GetTile(x, y);
                    if (currentTile != null)
                    {
                        currentTile.ClearNeighbors();
                        ConnectToNeighbors(currentTile);
                    }
                }
            }
        }

        private void ConnectToNeighbors(TileData tile)
        {
            foreach (var direction in orthogonalDirections)
            {
                var neighborPos = tile.Position + direction;
                var neighbor = gridData.GetTile(neighborPos);
                if (neighbor != null)
                    tile.AddNeighbor(neighbor);
            }
        }
    }
}