using System.Collections.Generic;
using UnityEngine;

namespace PathfindingDemo
{
    public enum TileType
    {
        Traversable = 0,
        Obstacle = 1,
        Cover = 2
    }

    [System.Serializable]
    public class TileData
    {
        public Vector2Int Position { get; private set; }
        public TileType Type { get; set; }
        public GameObject TileObject { get; set; }

        private readonly List<TileData> neighbors = new List<TileData>(4);

        public TileData(Vector2Int position, TileType type = TileType.Traversable)
        {
            Position = position;
            Type = type;
        }

        public void AddNeighbor(TileData neighbor)
        {
            if (neighbor != null && !neighbors.Contains(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        public IReadOnlyList<TileData> GetNeighbors()
        {
            return neighbors.AsReadOnly();
        }

        public void ClearNeighbors()
        {
            neighbors.Clear();
        }

        public bool IsTraversable()
        {
            return Type == TileType.Traversable;
        }

        public bool CanMoveThrough()
        {
            return Type == TileType.Traversable;
        }

        public bool CanAttackThrough()
        {
            return Type == TileType.Traversable || Type == TileType.Cover;
        }

        public override string ToString()
        {
            return $"Tile({Position.x}, {Position.y}) - {Type}";
        }
    }
}