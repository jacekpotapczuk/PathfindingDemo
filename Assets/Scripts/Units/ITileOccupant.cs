namespace PathfindingDemo
{
    public interface ITileOccupant
    {
        TileData CurrentTile { get; }
        bool CanOccupyTile(TileData tile);
        void SetTile(TileData tile);
        void RemoveFromTile();
    }
}