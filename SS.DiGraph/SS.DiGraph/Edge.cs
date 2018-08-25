using System;
using System.Diagnostics.CodeAnalysis;
using SS.DiGraph.Interfaces;
using SS.DiGraph.Utility;

namespace SS.DiGraph
{
    /// <summary>
    /// INTERNAL the edge class
    /// </summary>
    /// <typeparam name="T">T: class, IEdgeState, IDisposable, new():: the type of state for this edge </typeparam>
    internal sealed class Edge<T> : IEdge where T : class, IEdgeState, IDisposable, new()
    {
        // fields
        private readonly StringHelper _stringUtility;

        // properties
        private T State { get; set; }
        private INode TerminalNode { get; set; }
        private bool IsDirected { get; set; }

        #region IEdgePublic Support
        // properties
        public string Name { get; private set; }

        // accessors
        /// <summary>
        /// accessor for the state of this edge
        /// </summary>
        /// <returns>pbject:: the state of the edge</returns>
        public object GetState(){ return State; }
        #endregion

        #region constructors
        /// <summary>
        /// constructor, implicit initialization of state
        /// </summary>
        /// <param name="initName">string:: name of the edge</param>
        /// <param name="initTerminalNode">INode:: the node that terminates this edge</param>
        /// <param name="initIsDirected">bool:: when true, it is a directed edge, otherwise, it is bidirectional</param>
        /// <exception cref="ArgumentNullException" >thrown when name or terminal node are null</exception>
        internal Edge(string initName, INode initTerminalNode, bool initIsDirected = true) : 
            this(initName, new T(), initTerminalNode, initIsDirected)
        {
        }

        /// <summary>
        /// constructor, explicit initialization of state
        /// </summary>
        /// <param name="initName">string:: name of the edge</param>
        /// <param name="initState">T:: the initial edge state</param>
        /// <param name="initTerminalNode">INode:: the node that terminates this edge</param>
        /// <param name="initIsDirected">bool:: when true, it is a directed edge, otherwise, it is bidirectional</param>
        /// <exception cref="ArgumentNullException" >thrown when name or terminal node are null</exception>
        internal Edge(string initName, T initState, INode initTerminalNode, bool initIsDirected)
        {
            _stringUtility = new StringHelper();

            // name must exist
            if (string.IsNullOrWhiteSpace(initName))
            {
                throw new ArgumentNullException("initName");
            }

            Name = _stringUtility.ScrubName(initName);

            // state can be null
            State = initState;

            // terminalNode cannot be null
            if (initTerminalNode == null)
            {
                throw new ArgumentNullException("initTerminalNode");
            }

            TerminalNode = initTerminalNode;
            IsDirected = initIsDirected;
        }
        #endregion

        // methods
        #region IEdgeInternal Support
        /// <summary>
        /// INTERNAL Forward command
        /// </summary>
        /// <param name="initNode">INode:: the origin node for this edge</param>
        /// <exception cref="ArgumentNullException" >thrown when the origin node is null.</exception>
        /// <exception cref="NullReferenceException" >thrown when the edge state is null.</exception>
        /// <remarks><para>this method calls the ForwardPath() method in the edge state.</para></remarks>
        /* internal */ void IEdgeInternal.Forward(INode initNode)
        {
            if(initNode == null)
            {
                throw new ArgumentNullException("initNode");
            }
            
            if(State == null)
            {
                throw new NullReferenceException("State is null.");
            }

            State.ForwardPath(initNode, TerminalNode);
        }

        /// <summary>
        /// INTERNAL Reverse command
        /// </summary>
        /// <param name="initNode">the origin node for this edge</param>
        /// <exception cref="ArgumentNullException" >thrown when the origin node is null.</exception>
        /// <exception cref="NullReferenceException" >thrown when the edge state is null.</exception>
        /// <exception cref="InvalidOperationException" >thrown when the edge is not bidirectional</exception>
        /// <remarks><para>this method calls the ReversePath() method in edge state</para></remarks>
        /// <remarks><para>this method will only be executed on bidirectional edges</para></remarks>
        /* internal */ void IEdgeInternal.Reverse(INode initNode)
        {
            if (initNode == null)
            {
                throw new ArgumentNullException("initNode");
            }

            if(IsDirected)
            {
                throw new InvalidOperationException("The edge is directed, which does not allow reverse traversal.");
            }

            if (State == null)
            {
                throw new NullReferenceException("State is null.");
            }

            State.ReversePath(initNode, TerminalNode);
        }

        /// <summary>
        /// INTERNAL test for terminal node name
        /// </summary>
        /// <param name="initNodeName">string:: the name to check for</param>
        /// <returns>bool:: true if this edge contains a terminal node with a matching name, false otherwise</returns>
        /// <exception cref="ArgumentNullException" >thrown when the name parameter is null</exception>
        /* internal */ bool IEdgeInternal.IsTerminalNode(string initNodeName)
        {
            if(string.IsNullOrWhiteSpace(initNodeName))
            {
                throw new ArgumentNullException("initNodeName");
            }

            string scrubbedNodeName = _stringUtility.ScrubName(initNodeName);

            return (initNodeName == TerminalNode.Name);
        }
        #endregion


        #region IDisposable Support
        // this section was intenally generated by Visual Studio 

        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// dispose of managed and unmanaged objects
        /// </summary>
        /// <param name="disposing">bool:: true if called from code, false if called from finalizer</param>
        internal void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if(State != null)
                    {
                        State.Dispose();
                        State = null;
                    }
                    if (TerminalNode != null)
                    {
                        TerminalNode.Dispose();
                        TerminalNode = null;
                    }

                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        /// <summary>
        /// INTERNAL dispose this
        /// </summary>
        /* internal */ void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
