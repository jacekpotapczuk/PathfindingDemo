using System.Collections.Generic;
using UnityEngine;

namespace PathfindingDemo
{
    /// <summary>
    /// A* pathfinding algorithm implementation for grid-based navigation.
    /// </summary>
    public class AStarPathfinder
    {
        private SortedSet<INode> openList;
        private HashSet<INode> closedList;
        private INode endNode;

        public List<INode> GetPath(INode startNode, INode endNode)
        {
            if (!endNode.IsWalkable)
                return null;

            openList = new SortedSet<INode>(new ByFScore());
            closedList = new HashSet<INode>();
            this.endNode = endNode;
            startNode.Parent = null;
            openList.Add(startNode);

            while (DoPathfinding()) { }

            if (endNode.Parent == null)
                return null;

            var path = new List<INode>();
            var node = endNode;
            while (node != null)
            {
                path.Add(node);
                node = node.Parent;
            }

            path.Reverse();
            return path;
        }

        private bool DoPathfinding()
        {
            if (openList.Count == 0)
            {
                endNode.Parent = null;
                return false;
            }

            var currentNode = openList.Min;
            if (currentNode == endNode)
                return false;

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (var n in currentNode.GetNeighbours())
                AddNodeToOpenList(n, currentNode);

            return true;
        }

        private void AddNodeToOpenList(INode node, INode potentialParent)
        {
            if (!node.IsWalkable || closedList.Contains(node))
                return;

            if (openList.Contains(node))
            {
                var newGScore = potentialParent.GScore + 1;
                if (newGScore < node.GScore)
                {
                    openList.Remove(node);
                    node.Parent = potentialParent;
                    node.GScore = newGScore;
                    node.HScore = GetManhattanDistance(node, endNode);
                    openList.Add(node);
                }
            }
            else
            {
                node.Parent = potentialParent;
                node.GScore = potentialParent.GScore + 1;
                node.HScore = GetManhattanDistance(node, endNode);
                openList.Add(node);
            }
        }

        private static int GetManhattanDistance(INode node1, INode node2)
        {
            return Mathf.Abs(node1.X - node2.X) + Mathf.Abs(node1.Y - node2.Y);
        }
    }
}