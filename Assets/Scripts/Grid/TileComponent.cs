using UnityEngine;

namespace PathfindingDemo
{
    public class TileComponent : MonoBehaviour
    {
        private TileData tileData;

        public TileData TileData => tileData;

        public void Initialize(TileData data)
        {
            tileData = data;
        }

        public void CycleTileType()
        {
            var gridGenerator = FindFirstObjectByType<GridGenerator>();
            if (gridGenerator == null) return;

            TileType nextType = tileData.Type switch
            {
                TileType.Traversable => TileType.Obstacle,
                TileType.Obstacle => TileType.Cover,
                TileType.Cover => TileType.Traversable,
                _ => TileType.Traversable
            };

            gridGenerator.SetTileType(tileData.Position, nextType);
        }
    }
}