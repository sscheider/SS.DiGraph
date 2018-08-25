using System;
using System.Collections.Generic;

namespace SS.DiGraph.Interfaces
{
    public interface INodePublic : IDisposable 
    {
        string Name { get; }
        object GetState();
        object GetEdgeState(string initEdgeName);
        IEnumerable<string> GetEdgeNames();
        void TraverseEdgeForward(string initEdgeName);
        void TraverseEdgeReverse(string initEdgeName);
    }
}
