using UnityEngine;

namespace PathfindingDemo
{
    public enum UnitType
    {
        Player,
        Enemy
    }

    public class UnitComponent : MonoBehaviour, ITileOccupant
    {
        [Header("Unit Settings")]
        [SerializeField] private UnitType unitType = UnitType.Player;
        [SerializeField] private int moveRange = 4;
        [SerializeField] private int attackRange = 4;

        private TileData currentTile;

        public UnitType Type => unitType;
        public int MoveRange => moveRange;
        public int AttackRange => attackRange;
        public TileData CurrentTile => currentTile;

        public bool CanOccupyTile(TileData tile)
        {
            return tile != null && tile.CanBeOccupied();
        }

        public void SetTile(TileData tile)
        {
            if (tile == null)
            {
                Debug.LogError("Cannot set unit to null tile");
                return;
            }

            if (!CanOccupyTile(tile))
            {
                Debug.LogError($"Unit cannot occupy tile at {tile.Position}");
                return;
            }

            // Remove from current tile if we have one
            RemoveFromTile();

            // Set new tile
            currentTile = tile;
            tile.SetOccupant(this);

            // Position the unit GameObject on the tile
            if (tile.TileObject != null)
            {
                Vector3 tilePosition = tile.TileObject.transform.position;
                tilePosition.y += 0.5f; // Place unit above the tile
                transform.position = tilePosition;
            }
        }

        public void RemoveFromTile()
        {
            if (currentTile != null)
            {
                currentTile.RemoveOccupant();
                currentTile = null;
            }
        }

        public void SetMoveRange(int range)
        {
            moveRange = Mathf.Max(0, range);
        }

        public void SetAttackRange(int range)
        {
            attackRange = Mathf.Max(0, range);
        }

        private void OnDestroy()
        {
            RemoveFromTile();
        }

        public override string ToString()
        {
            var tileInfo = currentTile != null ? $" on {currentTile.Position}" : " (no tile)";
            return $"{unitType} Unit{tileInfo} - Move:{moveRange}, Attack:{attackRange}";
        }
    }
}