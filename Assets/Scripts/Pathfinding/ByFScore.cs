using System.Collections.Generic;
using PathfindingDemo;

namespace PathfindingDemo
{
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
        
            // this order doesn't really matter,
            // we just want to distinguish between tiles and return 0 only of coordinates are the same
            // however this can be changed to alter search preferences
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
