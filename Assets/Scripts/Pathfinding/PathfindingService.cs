using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PathfindingDemo
{
    public static class PathfindingService
    {
        public static List<TileData> FindPath(Grid grid, TileData start, TileData target, PathType pathType)
        {
            if (grid == null || start == null || target == null)
            {
                UnityEngine.Debug.LogError("PathfindingService: Invalid parameters provided");
                return new List<TileData>();
            }

            if (start == target)
            {
                return new List<TileData> { start };
            }

            // Reset pathfinding properties for clean state
            ResetTilePathfindingData(start);
            ResetTilePathfindingData(target);

            // Set walkability context on tiles
            SetWalkabilityContext(grid, pathType);

            // Use standard pathfinder
            var pathfinder = new AStarPathfinder();
            var nodePath = pathfinder.GetPath(start, target);

            if (nodePath == null || nodePath.Count == 0)
            {
                UnityEngine.Debug.Log($"PathfindingService: No path found from {start.Position} to {target.Position}");
                return new List<TileData>();
            }

            // Convert INode path back to TileData path
            var tilePath = new List<TileData>();
            foreach (var node in nodePath)
            {
                if (node is TileData tileData)
                {
                    tilePath.Add(tileData);
                }
            }

            return tilePath;
        }

        private static void ResetTilePathfindingData(TileData tile)
        {
            tile.Parent = null;
            tile.GScore = 0;
            tile.HScore = 0;
        }

        private static void SetWalkabilityContext(Grid grid, PathType pathType)
        {
            // Set current pathfinding context on all tiles
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var tile = grid.GetTile(x, y);
                    if (tile != null)
                    {
                        tile.CurrentPathType = pathType;
                    }
                }
            }
        }

        public static bool IsPathInRange(List<TileData> path, int maxRange)
        {
            if (path == null || path.Count == 0)
                return false;

            // Path length includes start tile, so actual moves = path.Count - 1
            return (path.Count - 1) <= maxRange;
        }

        public static List<TileData> GetInRangePath(List<TileData> path, int maxRange)
        {
            if (path == null || path.Count == 0)
                return new List<TileData>();

            int maxTiles = maxRange + 1; // +1 because we include the start tile
            if (path.Count <= maxTiles)
                return new List<TileData>(path);

            return path.GetRange(0, maxTiles);
        }

        public static List<TileData> GetOutOfRangePath(List<TileData> path, int maxRange)
        {
            if (path == null || path.Count == 0)
                return new List<TileData>();

            int maxTiles = maxRange + 1;
            if (path.Count <= maxTiles)
                return new List<TileData>();

            return path.GetRange(maxTiles - 1, path.Count - maxTiles + 1);
        }

        /// <summary>
        /// Gets a movement segment that excludes the starting tile and returns exactly maxRange movement tiles.
        /// Used for proper multi-turn visualization where each turn should show the actual movement tiles.
        /// </summary>
        public static List<TileData> GetMovementSegment(List<TileData> path, int maxRange, bool includeStartTile = false)
        {
            if (path == null || path.Count == 0)
                return new List<TileData>();

            int startIndex = includeStartTile ? 0 : 1; // Skip start tile for movement visualization
            int maxTiles = includeStartTile ? maxRange + 1 : maxRange;

            // If path is too short, return what we have (excluding start tile if needed)
            if (path.Count <= startIndex)
                return new List<TileData>();

            int endIndex = UnityEngine.Mathf.Min(startIndex + maxTiles, path.Count);
            int segmentLength = endIndex - startIndex;

            if (segmentLength <= 0)
                return new List<TileData>();

            return path.GetRange(startIndex, segmentLength);
        }

        /// <summary>
        /// Finds optimal path for move-to-attack scenarios by calculating in two stages:
        /// 1. All reachable positions within movement range (movement rules only)
        /// 2. Attack paths from each reachable position to target (attack rules)
        /// Returns the optimal combined path.
        /// </summary>
        public static List<TileData> FindMoveToAttackPath(Grid grid, TileData start, TileData target, int moveRange, int attackRange)
        {
            if (grid == null || start == null || target == null)
            {
                UnityEngine.Debug.LogError("PathfindingService: Invalid parameters provided for move-to-attack pathfinding");
                return new List<TileData>();
            }

            if (start == target)
            {
                return new List<TileData> { start };
            }

            // Step 1: Find all reachable positions within movement range using movement rules
            var reachablePositions = FindReachablePositions(grid, start, moveRange);
            if (reachablePositions.Count == 0)
            {
                UnityEngine.Debug.Log("PathfindingService: No reachable positions found for movement");
                return new List<TileData>();
            }

            List<TileData> bestPath = null;
            int shortestDistance = int.MaxValue;

            // Step 2: For each reachable position, calculate attack path to target
            foreach (var reachablePos in reachablePositions)
            {
                // Calculate attack path from this position to target
                var attackPath = FindPath(grid, reachablePos, target, PathType.Attack);

                if (attackPath.Count > 0 && IsPathInRange(attackPath, attackRange))
                {
                    // Calculate movement path to this position
                    var movementPath = FindPath(grid, start, reachablePos, PathType.Movement);

                    if (movementPath.Count > 0)
                    {
                        // Combine paths (remove duplicate tile at connection point)
                        var combinedPath = new List<TileData>(movementPath);
                        for (int i = 1; i < attackPath.Count; i++) // Skip first tile to avoid duplicate
                        {
                            combinedPath.Add(attackPath[i]);
                        }

                        // Check if this is the shortest valid path
                        if (combinedPath.Count < shortestDistance)
                        {
                            shortestDistance = combinedPath.Count;
                            bestPath = combinedPath;
                        }
                    }
                }
            }

            if (bestPath == null)
            {
                UnityEngine.Debug.Log("PathfindingService: No valid move-to-attack path found");
                return new List<TileData>();
            }

            UnityEngine.Debug.Log($"PathfindingService: Found move-to-attack path with {bestPath.Count} tiles");
            return bestPath;
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

            // Find all positions within attack range of the target
            var attackPositions = new List<TileData>();

            for (int dx = -attackRange; dx <= attackRange; dx++)
            {
                for (int dy = -attackRange; dy <= attackRange; dy++)
                {
                    int manhattanDistance = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (manhattanDistance > 0 && manhattanDistance <= attackRange)
                    {
                        int x = target.Position.x + dx;
                        int y = target.Position.y + dy;

                        if (x >= 0 && x < grid.Width && y >= 0 && y < grid.Height)
                        {
                            var tile = grid.GetTile(x, y);
                            if (tile != null && tile.CanBeOccupied())
                            {
                                // Verify attack path is valid (can attack through cover)
                                var attackPath = FindPath(grid, tile, target, PathType.Attack);
                                if (attackPath.Count > 0 && IsPathInRange(attackPath, attackRange))
                                {
                                    attackPositions.Add(tile);
                                }
                            }
                        }
                    }
                }
            }

            if (attackPositions.Count == 0)
            {
                UnityEngine.Debug.Log("PathfindingService: No valid attack positions found");
                return null;
            }

            // Find the attack position with the shortest movement path
            TileData bestPosition = null;
            int shortestDistance = int.MaxValue;

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

            while (currentPosition != target) // Calculate as many turns as needed
            {
                // Calculate path from current position to target using movement rules
                var fullPath = FindPath(grid, currentPosition, target, PathType.Movement);

                if (fullPath.Count == 0)
                {
                    // No path possible
                    break;
                }

                // Get the movement segment for this turn (excluding start tile, exactly moveRange tiles)
                var turnSegment = GetMovementSegment(fullPath, moveRange, false);

                if (turnSegment.Count == 0)
                {
                    // Can't make progress
                    break;
                }

                // Add this turn's pure movement segment (no overlaps)
                result.Add((new List<TileData>(turnSegment), turnNumber));

                // Move to the end of this turn's movement
                var nextPosition = turnSegment[turnSegment.Count - 1];

                // Check if we've reached the target
                if (nextPosition == target)
                {
                    break;
                }

                // Prevent infinite loops
                if (visitedPositions.Contains(nextPosition))
                {
                    break;
                }

                visitedPositions.Add(currentPosition);
                currentPosition = nextPosition;
                turnNumber++;
            }

            return result;
        }

        /// <summary>
        /// Finds all positions reachable within the given range using movement rules
        /// </summary>
        private static List<TileData> FindReachablePositions(Grid grid, TileData start, int maxRange)
        {
            var reachablePositions = new List<TileData>();
            var visited = new HashSet<TileData>();
            var queue = new Queue<(TileData tile, int distance)>();

            // Set movement pathfinding context
            SetWalkabilityContext(grid, PathType.Movement);

            queue.Enqueue((start, 0));
            visited.Add(start);

            while (queue.Count > 0)
            {
                var (currentTile, distance) = queue.Dequeue();

                // Add to reachable positions if within range
                if (distance <= maxRange)
                {
                    reachablePositions.Add(currentTile);
                }

                // Continue searching if we haven't reached max range
                if (distance < maxRange)
                {
                    foreach (var neighbor in currentTile.GetNeighbors())
                    {
                        if (!visited.Contains(neighbor) && neighbor.IsWalkableForPathType(PathType.Movement) && !neighbor.IsOccupied())
                        {
                            visited.Add(neighbor);
                            queue.Enqueue((neighbor, distance + 1));
                        }
                    }
                }
            }

            return reachablePositions;
        }
    }
}