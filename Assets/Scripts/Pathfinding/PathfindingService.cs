using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PathfindingDemo
{
    /// <summary>
    /// Static service providing high-level pathfinding operations for tactical movement and combat.
    /// </summary>
    public static class PathfindingService
    {
        public static List<TileData> FindPath(Grid grid, TileData start, TileData target, PathType pathType)
        {
            if (grid == null || start == null || target == null)
            {
                Debug.LogError("PathfindingService: Invalid parameters provided");
                return new List<TileData>();
            }

            if (start == target)
                return new List<TileData> { start };

            ResetTilePathfindingData(start);
            ResetTilePathfindingData(target);
            SetWalkabilityContext(grid, pathType);

            var pathfinder = new AStarPathfinder();
            var nodePath = pathfinder.GetPath(start, target);

            if (nodePath == null || nodePath.Count == 0)
                return new List<TileData>();

            var tilePath = new List<TileData>();
            foreach (var node in nodePath)
            {
                if (node is TileData tileData)
                    tilePath.Add(tileData);
            }

            return tilePath;
        }

        public static bool IsPathInRange(List<TileData> path, int maxRange, PathType pathType = PathType.Movement)
        {
            if (path == null || path.Count == 0)
                return false;

            // For both movement and attack paths, exclude only the starting tile from distance calculation
            // Path length includes start tile, so actual distance = path.Count - 1
            return (path.Count - 1) <= maxRange;
        }

        public static List<TileData> GetInRangePath(List<TileData> path, int maxRange, PathType pathType = PathType.Movement)
        {
            if (path == null || path.Count == 0)
                return new List<TileData>();

            // For both movement and attack paths, the calculation is the same:
            // We want to return up to maxRange + 1 tiles (including the start tile)
            // The difference is in how we validate the range (IsPathInRange) and visualize (PathVisualizer)
            var maxTiles = maxRange + 1;
            if (path.Count <= maxTiles)
                return new List<TileData>(path);

            return path.GetRange(0, maxTiles);
        }

        public static List<TileData> GetOutOfRangePath(List<TileData> path, int maxRange)
        {
            if (path == null || path.Count == 0)
                return new List<TileData>();

            var maxTiles = maxRange + 1;
            if (path.Count <= maxTiles)
                return new List<TileData>();

            return path.GetRange(maxTiles - 1, path.Count - maxTiles + 1);
        }

        /// <summary>
        /// Finds the best attack position for targeting an enemy. Returns the optimal position
        /// within attack range that is reachable via movement and provides the shortest path.
        /// </summary>
        public static TileData FindBestAttackPosition(Grid grid, TileData start, TileData target, int attackRange)
        {
            if (grid == null || start == null || target == null)
            {
                UnityEngine.Debug.LogError("PathfindingService: Invalid parameters for FindBestAttackPosition");
                return null;
            }

            var attackPositions = new List<TileData>();
            for (var dx = -attackRange; dx <= attackRange; dx++)
            {
                for (var dy = -attackRange; dy <= attackRange; dy++)
                {
                    var manhattanDistance = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (manhattanDistance > 0 && manhattanDistance <= attackRange)
                    {
                        var x = target.Position.x + dx;
                        var y = target.Position.y + dy;

                        if (x >= 0 && x < grid.Width && y >= 0 && y < grid.Height)
                        {
                            var tile = grid.GetTile(x, y);
                            if (tile != null && tile.CanBeOccupied())
                            {
                                var attackPath = FindPath(grid, tile, target, PathType.Attack);
                                if (attackPath.Count > 0 && IsPathInRange(attackPath, attackRange, PathType.Attack))
                                    attackPositions.Add(tile);
                            }
                        }
                    }
                }
            }

            if (attackPositions.Count == 0)
                return null;

            TileData bestPosition = null;
            var shortestDistance = int.MaxValue;

            foreach (var attackPos in attackPositions)
            {
                var movementPath = FindPath(grid, start, attackPos, PathType.Movement);
                if (movementPath.Count > 0 && movementPath.Count < shortestDistance)
                {
                    shortestDistance = movementPath.Count;
                    bestPosition = attackPos;
                }
            }

            return bestPosition;
        }

        /// <summary>
        /// Calculates a multi-turn movement path showing the sequence of moves needed to reach the target.
        /// Returns path segments for each turn with metadata about turn numbers.
        /// </summary>
        public static List<(List<TileData> segment, int turnNumber)> FindMultiTurnMovementPath(Grid grid, TileData start, TileData target, int moveRange)
        {
            if (grid == null || start == null || target == null)
            {
                UnityEngine.Debug.LogError("PathfindingService: Invalid parameters for FindMultiTurnMovementPath");
                return new List<(List<TileData>, int)>();
            }

            var result = new List<(List<TileData>, int)>();
            var currentPosition = start;
            int turnNumber = 1;
            var visitedPositions = new HashSet<TileData>();

            while (currentPosition != target)
            {
                var fullPath = FindPath(grid, currentPosition, target, PathType.Movement);
                if (fullPath.Count == 0)
                    break;

                var turnSegment = GetMovementSegment(fullPath, moveRange, false);
                if (turnSegment.Count == 0)
                    break;

                result.Add((new List<TileData>(turnSegment), turnNumber));
                var nextPosition = turnSegment[turnSegment.Count - 1];

                if (nextPosition == target)
                    break;

                if (visitedPositions.Contains(nextPosition))
                    break;

                visitedPositions.Add(currentPosition);
                currentPosition = nextPosition;
                turnNumber++;
            }

            return result;
        }
        
        /// <summary>
        /// Gets a movement segment that excludes the starting tile and returns exactly maxRange movement tiles.
        /// Used for proper multi-turn visualization where each turn should show the actual movement tiles.
        /// </summary>
        private static List<TileData> GetMovementSegment(List<TileData> path, int maxRange, bool includeStartTile = false)
        {
            if (path == null || path.Count == 0)
                return new List<TileData>();

            var startIndex = includeStartTile ? 0 : 1;
            var maxTiles = includeStartTile ? maxRange + 1 : maxRange;

            if (path.Count <= startIndex)
                return new List<TileData>();

            var endIndex = UnityEngine.Mathf.Min(startIndex + maxTiles, path.Count);
            var segmentLength = endIndex - startIndex;

            return segmentLength <= 0 ? new List<TileData>() : path.GetRange(startIndex, segmentLength);
        }
        
        private static void ResetTilePathfindingData(TileData tile)
        {
            tile.Parent = null;
            tile.GScore = 0;
            tile.HScore = 0;
        }

        private static void SetWalkabilityContext(Grid grid, PathType pathType)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                for (var y = 0; y < grid.Height; y++)
                {
                    var tile = grid.GetTile(x, y);
                    if (tile != null)
                        tile.CurrentPathType = pathType;
                }
            }
        }
    }
}