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
        // Simple cache for pathfinding results to avoid duplicate calculations in same frame
        private static readonly Dictionary<(TileData start, TileData end, PathType type), List<TileData>> pathCache =
            new Dictionary<(TileData, TileData, PathType), List<TileData>>();
        
        private static int lastClearFrame = -1;
        
        public static List<TileData> FindPath(Grid grid, TileData start, TileData target, PathType pathType)
        {
            if (grid == null || start == null || target == null)
            {
                Debug.LogError("PathfindingService: Invalid parameters provided");
                return new List<TileData>();
            }

            if (start == target)
                return new List<TileData> { start };

            // Clear cache if we're in a new frame (prevents stale cache data)
            ClearCacheIfNeeded();

            // Check cache first
            var cacheKey = (start, target, pathType);
            if (pathCache.TryGetValue(cacheKey, out var cachedPath))
            {
                return new List<TileData>(cachedPath); // Return copy to prevent modification
            }

            ResetTilePathfindingData(start);
            ResetTilePathfindingData(target);
            SetWalkabilityContext(grid, pathType);

            var pathfinder = new AStarPathfinder();
            var nodePath = pathfinder.GetPath(start, target);

            var tilePath = new List<TileData>();
            if (nodePath != null && nodePath.Count > 0)
            {
                foreach (var node in nodePath)
                {
                    if (node is TileData tileData)
                        tilePath.Add(tileData);
                }
            }

            // Cache the result
            pathCache[cacheKey] = new List<TileData>(tilePath);

            return tilePath;
        }

        /// <summary>
        /// Clears the pathfinding cache when we enter a new frame to prevent stale data.
        /// </summary>
        private static void ClearCacheIfNeeded()
        {
            var currentFrame = Time.frameCount;
            if (currentFrame != lastClearFrame)
            {
                pathCache.Clear();
                lastClearFrame = currentFrame;
            }
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
        /// Finds the best attack position using bidirectional BFS algorithm.
        /// Searches from enemy (attack range) and player (movement) until they meet.
        /// </summary>
        public static TileData FindBestAttackPosition(Grid grid, TileData start, TileData target, int attackRange)
        {
            if (grid == null || start == null || target == null)
            {
                UnityEngine.Debug.LogError("PathfindingService: Invalid parameters for FindBestAttackPosition");
                return null;
            }

            // Step 1: BFS from enemy to find all valid attack positions
            var attackPositions = GetAttackPositionsBFS(grid, target, attackRange);
            if (attackPositions.Count == 0)
                return null;

            // Step 2: Expanding BFS from player until we hit an attack position
            return FindNearestAttackPositionBFS(grid, start, attackPositions);
        }

        /// <summary>
        /// Uses BFS to find all positions from which the enemy can be attacked.
        /// </summary>
        private static HashSet<TileData> GetAttackPositionsBFS(Grid grid, TileData enemyPosition, int attackRange)
        {
            var attackPositions = new HashSet<TileData>();
            var visited = new HashSet<TileData>();
            var queue = new Queue<(TileData tile, int distance)>();

            queue.Enqueue((enemyPosition, 0));
            visited.Add(enemyPosition);

            while (queue.Count > 0)
            {
                var (currentTile, distance) = queue.Dequeue();

                // If within attack range and not the enemy position itself
                if (distance > 0 && distance <= attackRange && currentTile.CanBeOccupied())
                {
                    attackPositions.Add(currentTile);
                }

                // Continue BFS if we haven't reached max attack range
                if (distance < attackRange)
                {
                    foreach (var neighbor in currentTile.GetNeighbors())
                    {
                        if (!visited.Contains(neighbor) && neighbor.CanAttackThrough())
                        {
                            visited.Add(neighbor);
                            queue.Enqueue((neighbor, distance + 1));
                        }
                    }
                }
            }

            return attackPositions;
        }

        /// <summary>
        /// Uses expanding BFS from player position to find the nearest attack position.
        /// </summary>
        private static TileData FindNearestAttackPositionBFS(Grid grid, TileData playerPosition, HashSet<TileData> attackPositions)
        {
            var visited = new HashSet<TileData>();
            var queue = new Queue<(TileData tile, int distance)>();

            queue.Enqueue((playerPosition, 0));
            visited.Add(playerPosition);

            while (queue.Count > 0)
            {
                var (currentTile, distance) = queue.Dequeue();

                // Check if current tile is a valid attack position
                if (distance > 0 && attackPositions.Contains(currentTile))
                {
                    return currentTile; // Found the nearest attack position!
                }

                // Continue BFS expansion
                foreach (var neighbor in currentTile.GetNeighbors())
                {
                    if (!visited.Contains(neighbor) && neighbor.CanMoveThrough())
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, distance + 1));
                    }
                }
            }

            return null; // No reachable attack position found
        }



        /// <summary>
        /// Fast attack validation using Manhattan distance instead of expensive pathfinding.
        /// Works for grid-based tactical games where attack paths follow movement rules.
        /// </summary>
        private static bool CanAttackFromPosition(TileData attackPos, TileData target, int attackRange)
        {
            var distance = GetManhattanDistance(attackPos, target);
            return distance <= attackRange;
        }

        /// <summary>
        /// Helper method to calculate Manhattan distance between two tiles.
        /// </summary>
        private static int GetManhattanDistance(TileData tile1, TileData tile2)
        {
            return Mathf.Abs(tile1.Position.x - tile2.Position.x) + Mathf.Abs(tile1.Position.y - tile2.Position.y);
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