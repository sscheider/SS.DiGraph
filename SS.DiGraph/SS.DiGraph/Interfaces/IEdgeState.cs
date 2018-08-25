using System;
using System.Collections.Generic;
using System.Text;

namespace SS.DiGraph.Interfaces
{
    public interface IEdgeState : IDisposable
    {
        void ForwardPath(INode iniOriginNode, INode iniTerminalNode);
        void ReversePath(INode iniOriginNode, INode iniTerminalNode);
    }
}

