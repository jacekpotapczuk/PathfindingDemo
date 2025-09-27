using System.Collections.Generic;
using UnityEngine;

namespace PathfindingDemo
{
    public class PathVisualizer : MonoBehaviour
    {
        [Header("Path Visualization Settings")]
        [SerializeField] private Color inRangeColor = Color.cyan;
        [SerializeField] private Color outOfRangeColor = Color.gray;
        [SerializeField] private Color attackPathColor = Color.magenta;
        [SerializeField] private float pathHeight = 0.1f;

        private List<TileData> currentPath = new List<TileData>();
        private List<TileData> inRangePath = new List<TileData>();
        private List<TileData> outOfRangePath = new List<TileData>();
        private PathType currentPathType = PathType.Movement;
        private bool showPath = false;

        public void ShowPath(List<TileData> path, int maxRange, PathType pathType)
        {
            if (path == null || path.Count == 0)
            {
                HidePath();
                return;
            }

            currentPath = new List<TileData>(path);
            currentPathType = pathType;
            showPath = true;

            // Split path into in-range and out-of-range segments
            inRangePath = PathfindingService.GetInRangePath(path, maxRange);
            outOfRangePath = PathfindingService.GetOutOfRangePath(path, maxRange);

            Debug.Log($"PathVisualizer: Showing {pathType} path with {path.Count} tiles " +
                     $"(In range: {inRangePath.Count}, Out of range: {outOfRangePath.Count})");
        }

        public void HidePath()
        {
            showPath = false;
            currentPath.Clear();
            inRangePath.Clear();
            outOfRangePath.Clear();
        }

        public bool IsShowingPath()
        {
            return showPath && currentPath.Count > 0;
        }

        private void OnDrawGizmos()
        {
            if (!showPath || currentPath.Count == 0)
                return;

            DrawPathSegment(inRangePath, GetPathColor(true));
            DrawPathSegment(outOfRangePath, GetPathColor(false));
        }

        private void DrawPathSegment(List<TileData> pathSegment, Color color)
        {
            if (pathSegment == null || pathSegment.Count < 2)
                return;

            Gizmos.color = color;

            for (int i = 0; i < pathSegment.Count - 1; i++)
            {
                Vector3 start = GetTileWorldPosition(pathSegment[i]);
                Vector3 end = GetTileWorldPosition(pathSegment[i + 1]);

                // Draw line between tiles
                Gizmos.DrawLine(start, end);

                // Draw direction arrow
                DrawArrow(start, end);
            }

            // Draw tiles in path
            foreach (var tile in pathSegment)
            {
                Vector3 position = GetTileWorldPosition(tile);
                Gizmos.DrawWireCube(position, new Vector3(0.8f, pathHeight, 0.8f));
            }

            // Highlight start and end
            if (pathSegment.Count > 0)
            {
                // Start tile
                Vector3 startPos = GetTileWorldPosition(pathSegment[0]);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(startPos, 0.3f);

                // End tile
                Vector3 endPos = GetTileWorldPosition(pathSegment[pathSegment.Count - 1]);
                Gizmos.color = currentPathType == PathType.Attack ? attackPathColor : color;
                Gizmos.DrawWireSphere(endPos, 0.4f);
            }
        }

        private void DrawArrow(Vector3 start, Vector3 end)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 arrowHead = end - direction * 0.2f;

            // Simple arrow using perpendicular lines
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up) * 0.1f;
            Gizmos.DrawLine(end, arrowHead + perpendicular);
            Gizmos.DrawLine(end, arrowHead - perpendicular);
        }

        private Color GetPathColor(bool inRange)
        {
            if (currentPathType == PathType.Attack)
            {
                return inRange ? attackPathColor : outOfRangeColor;
            }
            else
            {
                return inRange ? inRangeColor : outOfRangeColor;
            }
        }

        private Vector3 GetTileWorldPosition(TileData tile)
        {
            if (tile?.TileObject != null)
            {
                Vector3 position = tile.TileObject.transform.position;
                position.y += pathHeight;
                return position;
            }
            return Vector3.zero;
        }

        public void UpdatePathColors(Color inRange, Color outOfRange, Color attack)
        {
            inRangeColor = inRange;
            outOfRangeColor = outOfRange;
            attackPathColor = attack;
        }

        // Debug information
        public string GetPathInfo()
        {
            if (!showPath)
                return "No path shown";

            return $"{currentPathType} Path: {currentPath.Count} tiles " +
                   $"(In range: {inRangePath.Count}, Out of range: {outOfRangePath.Count})";
        }
    }
}