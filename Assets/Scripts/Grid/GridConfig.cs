using UnityEngine;

namespace PathfindingDemo
{
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Pathfinding Demo/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        [Header("Grid Dimensions")]
        [SerializeField] private int defaultWidth = 10;
        [SerializeField] private int defaultHeight = 10;
        [SerializeField] private float tileSize = 1f;

        [Header("Tile Prefab")]
        [SerializeField] private GameObject tilePrefab;

        [Header("Materials")]
        [SerializeField] private Material traversableMaterial;
        [SerializeField] private Material obstacleMaterial;
        [SerializeField] private Material coverMaterial;

        [Header("Generation Settings")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool showGridGizmos = true;

        [Header("Random Generation")]
        [SerializeField] private bool useRandomGeneration = false;
        [SerializeField] [Range(0f, 1f)] private float obstacleChance = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float coverChance = 0.1f;

        public int DefaultWidth => defaultWidth;
        public int DefaultHeight => defaultHeight;
        public float TileSize => tileSize;
        public GameObject TilePrefab => tilePrefab;
        public Material TraversableMaterial => traversableMaterial;
        public Material ObstacleMaterial => obstacleMaterial;
        public Material CoverMaterial => coverMaterial;
        public bool GenerateOnStart => generateOnStart;
        public bool ShowGridGizmos => showGridGizmos;
        public bool UseRandomGeneration => useRandomGeneration;
        public float ObstacleChance => obstacleChance;
        public float CoverChance => coverChance;

        private void OnValidate()
        {
            defaultWidth = Mathf.Max(1, defaultWidth);
            defaultHeight = Mathf.Max(1, defaultHeight);
            tileSize = Mathf.Max(0.1f, tileSize);
        }

        public TileType GetRandomTileType()
        {
            if (!useRandomGeneration)
                return TileType.Traversable;

            float random = Random.value;

            if (random < obstacleChance)
                return TileType.Obstacle;
            else if (random < obstacleChance + coverChance)
                return TileType.Cover;
            else
                return TileType.Traversable;
        }

        public void ApplyToGenerator(GridGenerator generator)
        {
            if (generator == null) return;

            // Note: This would require adding setters to GridGenerator
            // or making the fields public for this to work
            Debug.Log($"Applying GridConfig: {defaultWidth}x{defaultHeight}");
        }
    }
}