using System.Collections.Generic;
using System.Linq;

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
    }
}