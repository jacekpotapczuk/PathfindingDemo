using System.Collections.Generic;
using UnityEngine;

namespace PathfindingDemo
{
    public class PathVisualizer : MonoBehaviour
    {
        [Header("Path Visualization Settings")]
        [SerializeField] private Material movePathMaterial;
        [SerializeField] private Material outOfRangeMaterial;
        [SerializeField] private Material attackPathMaterial;
        [SerializeField] private float pathHeight = 0.1f;
        [SerializeField] private int poolSize = 50;

        private List<TileData> currentPath = new List<TileData>();
        private List<TileData> inRangePath = new List<TileData>();
        private List<TileData> outOfRangePath = new List<TileData>();
        private PathType currentPathType = PathType.Movement;
        private bool showPath = false;

        private List<GameObject> pathTilePool = new List<GameObject>();
        private List<GameObject> activeTiles = new List<GameObject>();

        private void Start()
        {
            InitializePathTilePool();
        }

        private void InitializePathTilePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                CreatePathTile(i);
            }
            Debug.Log($"PathVisualizer: Initialized pool with {poolSize} tiles");
        }

        private GameObject CreatePathTile(int index)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = $"PathTile_{index}";
            tile.transform.SetParent(transform);
            tile.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
            tile.SetActive(false);

            Collider collider = tile.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            pathTilePool.Add(tile);
            return tile;
        }

        private void ExpandPool()
        {
            int oldSize = pathTilePool.Count;
            int newTilesCount = oldSize; // Double the size

            for (int i = 0; i < newTilesCount; i++)
            {
                CreatePathTile(oldSize + i);
            }

            poolSize = pathTilePool.Count;
            Debug.Log($"PathVisualizer: Expanded pool from {oldSize} to {poolSize} tiles");
        }

        private GameObject GetPooledTile()
        {
            foreach (GameObject tile in pathTilePool)
            {
                if (!tile.activeInHierarchy)
                {
                    return tile;
                }
            }

            // No available tiles, expand the pool
            ExpandPool();

            // Return the first newly created tile
            foreach (GameObject tile in pathTilePool)
            {
                if (!tile.activeInHierarchy)
                {
                    return tile;
                }
            }

            // Fallback: Create a single tile on demand if expansion somehow failed
            Debug.LogWarning("PathVisualizer: Pool expansion failed, creating emergency tile");
            return CreatePathTile(pathTilePool.Count);
        }

        public void ShowPath(List<TileData> path, int maxRange, PathType pathType)
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

            inRangePath = PathfindingService.GetInRangePath(path, maxRange);
            outOfRangePath = PathfindingService.GetOutOfRangePath(path, maxRange);

            ShowPathSegment(inRangePath, true);
            ShowPathSegment(outOfRangePath, false);

            Debug.Log($"PathVisualizer: Showing {pathType} path with {path.Count} tiles " +
                     $"(In range: {inRangePath.Count}, Out of range: {outOfRangePath.Count})");
        }

        private void ShowPathSegment(List<TileData> pathSegment, bool inRange)
        {
            if (pathSegment == null || pathSegment.Count == 0)
                return;

            Material materialToUse = GetMaterialForPath(inRange);
            if (materialToUse == null)
                return;

            foreach (var tile in pathSegment)
            {
                GameObject pathTile = GetPooledTile();

                Vector3 position = GetTileWorldPosition(tile);
                pathTile.transform.position = position;
                pathTile.GetComponent<MeshRenderer>().material = materialToUse;
                pathTile.SetActive(true);
                activeTiles.Add(pathTile);
            }
        }

        private Material GetMaterialForPath(bool inRange)
        {
            if (currentPathType == PathType.Attack)
            {
                return inRange ? attackPathMaterial : outOfRangeMaterial;
            }
            else // Movement path
            {
                return inRange ? movePathMaterial : outOfRangeMaterial;
            }
        }

        public void HidePath()
        {
            showPath = false;
            currentPath.Clear();
            inRangePath.Clear();
            outOfRangePath.Clear();

            foreach (GameObject tile in activeTiles)
            {
                if (tile != null)
                {
                    tile.SetActive(false);
                }
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
                Vector3 position = tile.TileObject.transform.position;
                position.y += pathHeight;
                return position;
            }
            return Vector3.zero;
        }


        // Debug information
        public string GetPathInfo()
        {
            if (!showPath)
                return "No path shown";

            return $"{currentPathType} Path: {currentPath.Count} tiles " +
                   $"(In range: {inRangePath.Count}, Out of range: {outOfRangePath.Count})";
        }

        public string GetPoolInfo()
        {
            int activeTileCount = activeTiles.Count;
            int totalPoolSize = pathTilePool.Count;
            int availableTiles = totalPoolSize - activeTileCount;

            return $"Pool: {activeTileCount}/{totalPoolSize} tiles active, {availableTiles} available";
        }
    }
}