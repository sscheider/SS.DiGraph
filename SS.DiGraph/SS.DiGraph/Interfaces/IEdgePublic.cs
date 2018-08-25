using System;

namespace SS.DiGraph.Interfaces
{
    public interface IEdgePublic : IDisposable
    {
        string Name { get; }
        object GetState();
    }
}
