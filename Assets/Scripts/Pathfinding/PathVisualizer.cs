using System.Collections.Generic;
using UnityEngine;

namespace PathfindingDemo
{
    /// <summary>
    /// Visualizes pathfinding results with pooled tile objects and multi-turn path display.
    /// </summary>
    public class PathVisualizer : MonoBehaviour
    {
        [Header("Path Visualization Settings")]
        [SerializeField] private Material movePathMaterial;
        [SerializeField] private Material movePathMaterial2;
        [SerializeField] private Material attackPathMaterial;
        [SerializeField] private float pathHeight = 0.1f;
        [SerializeField] private int poolSize = 50;

        private List<TileData> currentPath = new List<TileData>();
        private PathType currentPathType = PathType.Movement;
        private bool showPath = false;

        private List<GameObject> pathTilePool = new List<GameObject>();
        private List<GameObject> activeTiles = new List<GameObject>();

        private void Start()
        {
            InitializePathTilePool();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            PlayerController.OnPathCalculated += ShowPath;
            PlayerController.OnMoveToAttackPathCalculated += ShowMoveToAttackPath;
            PlayerController.OnMultiTurnPathCalculated += ShowMultiTurnPath;
            PlayerController.OnPathHidden += HidePath;
        }

        private void UnsubscribeFromEvents()
        {
            PlayerController.OnPathCalculated -= ShowPath;
            PlayerController.OnMoveToAttackPathCalculated -= ShowMoveToAttackPath;
            PlayerController.OnMultiTurnPathCalculated -= ShowMultiTurnPath;
            PlayerController.OnPathHidden -= HidePath;
        }

        private void InitializePathTilePool()
        {
            for (var i = 0; i < poolSize; i++)
                CreatePathTile(i);
        }

        private GameObject CreatePathTile(int index)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = $"PathTile_{index}";
            tile.transform.SetParent(transform);
            tile.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
            tile.SetActive(false);

            var collider = tile.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            pathTilePool.Add(tile);
            return tile;
        }

        private void ExpandPool()
        {
            var oldSize = pathTilePool.Count;
            var newTilesCount = oldSize;

            for (var i = 0; i < newTilesCount; i++)
                CreatePathTile(oldSize + i);

            poolSize = pathTilePool.Count;
        }

        private GameObject GetPooledTile()
        {
            foreach (var tile in pathTilePool)
            {
                if (!tile.activeInHierarchy)
                    return tile;
            }

            ExpandPool();
            foreach (var tile in pathTilePool)
            {
                if (!tile.activeInHierarchy)
                    return tile;
            }

            return CreatePathTile(pathTilePool.Count);
        }

        public void ShowPath(List<TileData> path, PathType pathType, int maxRange)
        {
            if (path == null || path.Count == 0)
            {
                HidePath();
                return;
            }

            HidePath();
            currentPath = new List<TileData>(path);
            currentPathType = pathType;
            showPath = true;

            var inRangePath = PathfindingService.GetInRangePath(path, maxRange);
            ShowPathSegment(inRangePath, true);
        }

        public void ShowMoveToAttackPath(List<TileData> fullPath, int moveRange, int attackRange)
        {
            if (fullPath == null || fullPath.Count == 0)
            {
                HidePath();
                return;
            }

            HidePath();
            currentPath = new List<TileData>(fullPath);
            currentPathType = PathType.Attack;
            showPath = true;

            var movementSegment = PathfindingService.GetInRangePath(fullPath, moveRange);
            var totalReachableRange = moveRange + attackRange;
            var reachableSegment = PathfindingService.GetInRangePath(fullPath, totalReachableRange);
            var attackSegment = new List<TileData>();

            for (var i = movementSegment.Count; i < reachableSegment.Count; i++)
            {
                if (i < fullPath.Count)
                    attackSegment.Add(fullPath[i]);
            }

            ShowPathSegmentWithMaterial(movementSegment, movePathMaterial);
            ShowPathSegmentWithMaterial(attackSegment, attackPathMaterial);
        }

        public void ShowMultiTurnPath(List<(List<TileData> segment, int turnNumber)> turnSegments, List<TileData> attackSegment = null)
        {
            HidePath();

            if (turnSegments == null || turnSegments.Count == 0)
            {
                return;
            }

            showPath = true;

            // Show each turn segment with appropriate material
            foreach (var (segment, turnNumber) in turnSegments)
            {
                var material = GetMaterialForTurn(turnNumber);
                if (material != null)
                    ShowPathSegmentWithMaterial(segment, material);
            }

            if (attackSegment != null && attackSegment.Count > 0)
                ShowPathSegmentWithMaterial(attackSegment, attackPathMaterial);
        }

        private Material GetMaterialForTurn(int turnNumber)
        {
            return turnNumber % 2 == 1 ? movePathMaterial : movePathMaterial2;
        }


        private void ShowPathSegment(List<TileData> pathSegment, bool inRange)
        {
            if (pathSegment == null || pathSegment.Count == 0)
                return;

            Material materialToUse = GetMaterialForPath(inRange);
            if (materialToUse == null)
                return;

            ShowPathSegmentWithMaterial(pathSegment, materialToUse);
        }

        private void ShowPathSegmentWithMaterial(List<TileData> pathSegment, Material material)
        {
            if (pathSegment == null || pathSegment.Count == 0 || material == null)
                return;

            foreach (var tile in pathSegment)
            {
                var pathTile = GetPooledTile();
                var position = GetTileWorldPosition(tile);
                pathTile.transform.position = position;
                pathTile.GetComponent<MeshRenderer>().material = material;
                pathTile.SetActive(true);
                activeTiles.Add(pathTile);
            }
        }

        private Material GetMaterialForPath(bool inRange)
        {
            return currentPathType == PathType.Attack ? attackPathMaterial : movePathMaterial;
        }

        public void HidePath()
        {
            showPath = false;
            currentPath.Clear();

            foreach (var tile in activeTiles)
            {
                if (tile != null)
                    tile.SetActive(false);
            }
            activeTiles.Clear();
        }

        public bool IsShowingPath()
        {
            return showPath && currentPath.Count > 0;
        }

        private Vector3 GetTileWorldPosition(TileData tile)
        {
            if (tile?.TileObject != null)
            {
                var position = tile.TileObject.transform.position;
                position.y += pathHeight;
                return position;
            }
            return Vector3.zero;
        }


        public string GetPathInfo()
        {
            if (!showPath)
                return "No path shown";

            return $"{currentPathType} Path: {currentPath.Count} tiles";
        }

        public string GetPoolInfo()
        {
            var activeTileCount = activeTiles.Count;
            var totalPoolSize = pathTilePool.Count;
            var availableTiles = totalPoolSize - activeTileCount;

            return $"Pool: {activeTileCount}/{totalPoolSize} tiles active, {availableTiles} available";
        }
    }
}