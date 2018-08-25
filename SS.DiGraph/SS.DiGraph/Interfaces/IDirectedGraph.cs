using System;
using System.Collections.Generic;

namespace SS.DiGraph.Interfaces
{
    public interface IDirectedGraph : IDisposable 
    {
        INode CreateNode<T>(string initName) where T : class, IDisposable, new();
        INode CreateNode<T>(string initName, T initState) where T : class, IDisposable, new();
        void CreateNodes<T>(IEnumerable<string> initNames) where T : class, IDisposable, new();
        void CreateNodes<T>(IDictionary<string, T> initDictNamesStates) where T : class, IDisposable, new();
        INode GetNode(string initName);
        object GetNodeState(string initName);
        void DeleteNode(string initName);
        List<string> GetNodeNames();
        void CreateEdge<TEdge>(string initName, string initOriginNodeName, string initTerminalNodeName, bool initIsDirected) where TEdge : class, IEdgeState, IDisposable, new();
        void CreateEdge<TEdge>(string initName, TEdge initState, string initOriginNodeName, string initTerminalNodeName, bool initIsDirected) where TEdge : class, IEdgeState, IDisposable, new();
        void DeleteEdge(string initOriginNode, string initEdgeName);
        object GetEdgeState(string initOriginNode, string initEdgeName);
    }
}
