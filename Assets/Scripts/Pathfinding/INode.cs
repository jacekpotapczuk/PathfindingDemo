using System.Collections.Generic;

namespace PathfindingDemo
{
    /// <summary>
    /// Interface for pathfinding nodes with A* algorithm support.
    /// </summary>
    public interface INode
    {
        int X { get; }
        int Y { get; }
        List<INode> GetNeighbours();
        INode Parent { get; set; }
        int GScore { get; set; }
        int HScore { get; set; }
        int FScore { get; }
        bool IsWalkable { get; }
    }
}