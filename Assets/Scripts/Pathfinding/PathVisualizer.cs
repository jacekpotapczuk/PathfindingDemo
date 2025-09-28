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

        private PathType currentPathType = PathType.Movement;

        private readonly List<GameObject> pathTilePool = new List<GameObject>();
        private readonly List<GameObject> activeTiles = new List<GameObject>();

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
            PlayerController.OnMultiTurnPathCalculated += ShowMultiTurnPath;
            PlayerController.OnPathHidden += HidePath;
        }

        private void UnsubscribeFromEvents()
        {
            PlayerController.OnPathCalculated -= ShowPath;
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

        private void ShowPath(List<TileData> path, PathType pathType, int maxRange)
        {
            if (path == null || path.Count == 0)
            {
                HidePath();
                return;
            }

            HidePath();
            currentPathType = pathType;

            var inRangePath = PathfindingService.GetInRangePath(path, maxRange, pathType);

            // For attack paths, exclude the player's starting tile from visualization
            if (pathType == PathType.Attack && inRangePath.Count > 1)
            {
                var visualPath = inRangePath.GetRange(1, inRangePath.Count - 1);
                ShowPathSegment(visualPath);
            }
            else
            {
                ShowPathSegment(inRangePath);
            }
        }
        
        private void ShowMultiTurnPath(List<(List<TileData> segment, int turnNumber)> turnSegments, List<TileData> attackSegment = null)
        {
            HidePath();

            if (turnSegments == null || turnSegments.Count == 0)
            {
                return;
            }

            // Show each turn segment with appropriate material
            foreach (var (segment, turnNumber) in turnSegments)
            {
                var material = GetMaterialForTurn(turnNumber);
                if (material != null)
                    ShowPathSegmentWithMaterial(segment, material);
            }

            if (attackSegment != null && attackSegment.Count > 0)
            {
                // For attack segments, exclude the starting tile from visualization
                var visualAttackSegment = attackSegment.Count > 1 ? attackSegment.GetRange(1, attackSegment.Count - 1) : attackSegment;
                ShowPathSegmentWithMaterial(visualAttackSegment, attackPathMaterial);
            }
        }

        private Material GetMaterialForTurn(int turnNumber)
        {
            return turnNumber % 2 == 1 ? movePathMaterial : movePathMaterial2;
        }


        private void ShowPathSegment(List<TileData> pathSegment)
        {
            if (pathSegment == null || pathSegment.Count == 0)
                return;

            Material materialToUse = GetMaterialForPath();
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

        private Material GetMaterialForPath()
        {
            return currentPathType == PathType.Attack ? attackPathMaterial : movePathMaterial;
        }

        private void HidePath()
        {
            foreach (var tile in activeTiles)
            {
                if (tile != null)
                    tile.SetActive(false);
            }
            activeTiles.Clear();
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
    }
}