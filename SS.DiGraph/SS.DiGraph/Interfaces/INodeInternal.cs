using System;

namespace SS.DiGraph.Interfaces
{
    internal interface INodeInternal
    {
        IEdge CreateEdge<TEdge>(string initName, INode initTerminalNode, bool initIsDirected) where TEdge : class, IEdgeState, IDisposable, new();
        IEdge CreateEdge<TEdge>(string initName, TEdge initState, INode initTerminalNode, bool initIsDirected) where TEdge : class, IEdgeState, IDisposable, new();
        void DeleteEdge(string initEdgeName);
        void DeleteEdgesTerminatingOnNode(string initNodeName);
    }
}
