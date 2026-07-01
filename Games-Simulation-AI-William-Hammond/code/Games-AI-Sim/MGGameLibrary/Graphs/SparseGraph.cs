using System.Collections.Generic;

namespace MGGameLibrary.Graphs
{
    /// <summary>
    /// A generic sparse graph data structure.
    /// TNode is the type of the nodes in the graph.
    /// TEdge is the type of the edges connecting nodes.
    /// </summary>
    public class SparseGraph<TNode, TEdge>
    {
        private Dictionary<TNode, List<(TEdge edge, TNode node)>> _adjacency = new();

        /// <summary>
        /// Adds a node to the graph. If the node already exists, nothing happens.
        /// </summary>
        public void AddNode(TNode node)
        {
            if (!_adjacency.ContainsKey(node))
                _adjacency[node] = new List<(TEdge, TNode)>();
        }

        /// <summary>
        /// Adds a directed edge from <paramref name="from"/> to <paramref name="to"/>
        /// with the given <paramref name="edge"/> data.
        /// Both nodes are added automatically if they are not already present.
        /// </summary>
        public void AddEdge(TNode from, TEdge edge, TNode to)
        {
            AddNode(from);
            AddNode(to);
            _adjacency[from].Add((edge, to));
        }

        /// <summary>
        /// Returns all outgoing edges (and their destination nodes) from the given node.
        /// Returns an empty list if the node is not in the graph.
        /// </summary>
        public List<(TEdge edge, TNode node)> GetEdges(TNode node)
        {
            if (_adjacency.TryGetValue(node, out var edges))
                return edges;
            return new List<(TEdge, TNode)>();
        }
    }
}