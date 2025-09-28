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
            Vector2Int.up,      // North
            Vector2Int.down,    // South
            Vector2Int.left,    // West
            Vector2Int.right    // East
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

        public List<TileData> GetTraversableNeighbors(TileData tile)
        {
            var traversableNeighbors = new List<TileData>();
            foreach (var neighbor in tile.GetNeighbors())
            {
                if (neighbor.CanMoveThrough())
                    traversableNeighbors.Add(neighbor);
            }
            return traversableNeighbors;
        }

        public List<TileData> GetAttackableNeighbors(TileData tile)
        {
            var attackableNeighbors = new List<TileData>();
            foreach (var neighbor in tile.GetNeighbors())
            {
                if (neighbor.CanAttackThrough())
                    attackableNeighbors.Add(neighbor);
            }
            return attackableNeighbors;
        }

        public void ResizeGrid(int newWidth, int newHeight)
        {
            gridData.ResizeGrid(newWidth, newHeight);
            GenerateNeighborConnections();
        }

        public TileData GetTileAtWorldPosition(Vector3 worldPosition)
        {
            var gridPos = GetGridPosition(worldPosition);
            return GetTile(gridPos);
        }

        public List<TileData> GetAllTilesOfType(TileType type)
        {
            var tilesOfType = new List<TileData>();
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var tile = GetTile(x, y);
                    if (tile != null && tile.Type == type)
                        tilesOfType.Add(tile);
                }
            }
            return tilesOfType;
        }

        public int GetNeighborCount(Vector2Int position)
        {
            var tile = GetTile(position);
            return tile?.GetNeighbors().Count ?? 0;
        }

        public bool IsEdgeTile(Vector2Int position)
        {
            return position.x == 0 || position.x == Width - 1 ||
                   position.y == 0 || position.y == Height - 1;
        }

        public bool IsCornerTile(Vector2Int position)
        {
            return (position.x == 0 || position.x == Width - 1) &&
                   (position.y == 0 || position.y == Height - 1);
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
    }
}