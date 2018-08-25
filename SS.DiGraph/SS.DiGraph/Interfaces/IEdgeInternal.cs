
namespace SS.DiGraph.Interfaces
{
    internal interface IEdgeInternal
    {
        void Forward(INode initNode);
        void Reverse(INode initNode);
        bool IsTerminalNode(string initNodeName);
    }

}
