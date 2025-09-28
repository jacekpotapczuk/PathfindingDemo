using System.Collections.Generic;

namespace PathfindingDemo
{
    /// <summary>
    /// Comparer for A* pathfinding nodes that prioritizes by F-score, then H-score, then coordinates.
    /// </summary>
    public class ByFScore : IComparer<INode>
    {
        public int Compare(INode node1, INode node2)
        {
            if (node1.FScore < node2.FScore)
                return -1;
            if (node2.FScore < node1.FScore)
                return 1;
        
            if (node1.HScore < node2.HScore)
                return -1;
            if (node2.HScore < node1.HScore)
                return 1;
        
            if (node1.X < node2.X)
                return -1;
            if (node1.X > node2.X)
                return 1;
            if (node1.Y < node2.Y)
                return -1;
            if (node1.Y > node2.Y)
                return 1;
        
            return 0;
        }
    }
}
