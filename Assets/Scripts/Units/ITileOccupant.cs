namespace PathfindingDemo
{
    /// <summary>
    /// Interface for objects that can occupy grid tiles.
    /// </summary>
    public interface ITileOccupant
    {
        TileData CurrentTile { get; }
        bool CanOccupyTile(TileData tile);
        void SetTile(TileData tile);
        void RemoveFromTile();
    }
}