using System.Collections.Generic;

namespace PathfindingDemo
{
    public interface INode
    {
        public int X { get; }
        public int Y { get; }
    
        public List<INode> GetNeighbours();
    
        public INode Parent { get; set; }
    
        public int GScore { get; set; }
    
        public int HScore { get; set; }
    
        public int FScore { get; }

        public bool IsWalkable { get; }
    }
}