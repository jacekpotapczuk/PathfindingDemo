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
    public class TileData : INode
    {
        public Vector2Int Position { get; private set; }
        public TileType Type { get; set; }
        public GameObject TileObject { get; set; }
        public ITileOccupant OccupiedBy { get; private set; }

        // INode properties for pathfinding
        public int X => Position.x;
        public int Y => Position.y;
        public INode Parent { get; set; }
        public int GScore { get; set; }
        public int HScore { get; set; }
        public int FScore => GScore + HScore;

        // Pathfinding context
        public PathType CurrentPathType { get; set; } = PathType.Movement;

        private readonly List<TileData> neighbors = new List<TileData>(4);

        public TileData(Vector2Int position, TileType type = TileType.Traversable)
        {
            Position = position;
            Type = type;
            // Initialize pathfinding properties
            Parent = null;
            GScore = 0;
            HScore = 0;
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

        // INode interface method for pathfinding
        public List<INode> GetNeighbours()
        {
            var nodeNeighbors = new List<INode>();
            foreach (var neighbor in neighbors)
            {
                nodeNeighbors.Add(neighbor);
            }
            return nodeNeighbors;
        }

        // Pathfinding-specific neighbor filtering
        public List<INode> GetNeighbours(PathType pathType)
        {
            var validNeighbors = new List<INode>();
            foreach (var neighbor in neighbors)
            {
                bool isWalkable = pathType switch
                {
                    PathType.Movement => neighbor.CanMoveThrough() && !neighbor.IsOccupied(),
                    PathType.Attack => neighbor.CanAttackThrough(),
                    _ => false
                };

                if (isWalkable)
                {
                    validNeighbors.Add(neighbor);
                }
            }
            return validNeighbors;
        }

        // Context-aware walkability for pathfinding
        public bool IsWalkable => IsWalkableForPathType(CurrentPathType);

        public bool IsWalkableForPathType(PathType pathType)
        {
            return pathType switch
            {
                PathType.Movement => CanMoveThrough(),
                PathType.Attack => CanAttackThrough(),
                _ => false
            };
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

        public bool IsOccupied()
        {
            return OccupiedBy != null;
        }

        public bool CanBeOccupied()
        {
            return IsTraversable() && !IsOccupied();
        }

        public void SetOccupant(ITileOccupant occupant)
        {
            if (occupant == null)
            {
                Debug.LogError("Cannot set null occupant");
                return;
            }

            if (!CanBeOccupied())
            {
                Debug.LogError($"Tile {Position} cannot be occupied - Type: {Type}, Occupied: {IsOccupied()}");
                return;
            }

            OccupiedBy = occupant;
        }

        public void RemoveOccupant()
        {
            OccupiedBy = null;
        }

        public override string ToString()
        {
            var occupantInfo = IsOccupied() ? " [Occupied]" : "";
            return $"Tile({Position.x}, {Position.y}) - {Type}{occupantInfo}";
        }
    }
}