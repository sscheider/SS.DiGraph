using SS.DiGraph.Interfaces;
using SS.DiGraph.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace SS.DiGraph
{
    /// <summary>
    /// the node class. it contains a collection of edges for this origin node
    /// </summary>
    /// <typeparam name="T">T: class, IDisposable, new():: the node state</typeparam>
    public sealed class Node<T> : INode, INodeInternal where T : class, IDisposable, new()
    {
        // constants
        private static readonly TimeSpan MAX_WAIT = TimeSpan.FromSeconds(120);

        // fields
        private readonly StringHelper _stringUtility;
        private readonly Mutex _mtxEdgesCollection;
        private readonly TimeSpan _mtxTimeout;

        // properties
        private T State { get; set; }
        private ConcurrentDictionary<string, IEdge> EdgesCollection { get; set; }

        // constructors
        /// <summary>
        /// INTERNAL constructor with string
        /// </summary>
        /// <param name="initName">string:: node name</param>
        /// <exception cref="ArgumentNullException" >thrown when the name is null</exception>
        /// <remarks>the state will be the default value set of T</remarks>
        internal Node(string initName) : this(initName, new T(), MAX_WAIT)
        {

        }

        /// <summary>
        /// INTERNAL constructor with string and state initializer
        /// </summary>
        /// <param name="initName">string:: the node name</param>
        /// <param name="initState">T:: the initial state of the node</param>
        /// <param name="initGeneralTimeout">TimeSpan:: the duration to wait for locks. Capped between [0, MAX_WAIT].</param>
        /// <exception cref="ArgumentNullException" >thrown when the name is null</exception>
        internal Node(string initName, T initState, TimeSpan initGeneralTimeout)
        {
            if (initGeneralTimeout == null)
            {
                throw new ArgumentNullException("initGeneralTimeout");
            }

            Guid mutexName = Guid.NewGuid();
            _mtxEdgesCollection = new Mutex(false, mutexName.ToString());

            // clamping the timeout between [0, MAX_WAIT]
            TimeSpan minTimeout = initGeneralTimeout < MAX_WAIT ? initGeneralTimeout : MAX_WAIT;
            _mtxTimeout = minTimeout > TimeSpan.Zero ? minTimeout : TimeSpan.Zero;

            _stringUtility = new StringHelper();

            // name must exist
            if (string.IsNullOrWhiteSpace(initName))
            {
                throw new ArgumentNullException("initName");
            }
            Name = _stringUtility.ScrubName(initName);

            // state must exist
            if (initState == null)
            {
                throw new ArgumentNullException("initState");
            }
            State = initState;

            // timeout must exist
            if(initGeneralTimeout == null)
            {
                throw new ArgumentNullException("initGeneralTimeout");
            }

            EdgesCollection = new ConcurrentDictionary<string, IEdge>();
        }


        #region INodePublic Support
        // properties
        public string Name { get; private set; }

        // accessors
        /// <summary>
        /// accessor for State of this node 
        /// </summary>
        /// <returns>object:: the State of this node</returns>
        public object GetState() { return State; }

        /// <summary>
        /// accessor for the State of an edge
        /// </summary>
        /// <param name="initEdgeName">string:: edge name</param>
        /// <returns>object:: the State of an edge</returns>
        /// <exception cref="AbandonedMutexException" >thrown when the mutex is null</exception>
        /// <exception cref="ArgumentNullException" >thrown when the edge name parameter is null</exception>
        /// <exception cref="InvalidOperationException" >thrown when EdgesCollection is uninitialized</exception>
        /// <exception cref="KeyNotFoundException" >thrown when the edge name does not exist</exception>
        /// <exception cref="NullReferenceException" >thrown when the value in EdgesCollection is null</exception>
        /// <exception cref="TimeoutException" >thrown when obtaining the mutex times out</exception>
        public object GetEdgeState(string initEdgeName)
        {
            Object returnObject = null;

            if (string.IsNullOrWhiteSpace(initEdgeName))
            {
                throw new ArgumentNullException("initEdgeName");
            }

            if (_mtxEdgesCollection == null)
            {
                string exceptionMsg = $"The state of edge {initEdgeName} could not be retrieved because the mutex has been abanoned.";
                throw new AbandonedMutexException(exceptionMsg);
            }

            CheckEdgesCollection();

            if(!_mtxEdgesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The state of edge {initEdgeName} could not be retrieved because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedEdgeName = _stringUtility.ScrubName(initEdgeName);
                if (!EdgesCollection.ContainsKey(scrubbedEdgeName))
                {
                    string exceptionText = $"The edge {scrubbedEdgeName} on node {Name} does not exist in EdgesCollection.";
                    throw new KeyNotFoundException(exceptionText);
                }

                IEdge edgeComponent = EdgesCollection[scrubbedEdgeName];
                if (edgeComponent == null)
                {
                    string exceptionText = $"The edge {scrubbedEdgeName} on node {Name} is null.";
                    throw new NullReferenceException(exceptionText);
                }

                returnObject = edgeComponent.GetState();
            }
            finally
            {
                _mtxEdgesCollection.ReleaseMutex();
            }

            return returnObject;
        }

        // methods
        /// <summary>
        /// get a list of edge names
        /// </summary>
        /// <returns>List&lt;string&gt;:: a list of the edge names</returns>
        /// <exception cref="AbandonedMutexException" >thrown when the mutex is null</exception>
        /// <exception cref="InvalidOperationException" >thrown when EdgesCollection is uninitialized</exception>
        /// <exception cref="TimeoutException" >thrown when obtaining the mutex times out</exception>
        public IEnumerable<string> GetEdgeNames()
        {
            List<string> returnValue = new List<string>();

            if (_mtxEdgesCollection == null)
            {
                string exceptionMsg = $"Edge names could not be iterated because the mutex has been abanoned.";
                throw new AbandonedMutexException(exceptionMsg);
            }

            CheckEdgesCollection();

            if (!_mtxEdgesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"Edge names could not be iterated because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                foreach (string key in EdgesCollection.Keys)
                {
                    yield return key;
                }
            }
            finally
            {
                _mtxEdgesCollection.ReleaseMutex();
            }
        }

        /// <summary>
        /// perform the operation associated with this call
        /// </summary>
        /// <param name="initEdgeName">string:: the edge to traverse</param>
        /// <exception cref="AbandonedMutexException" >thrown when the mutex is null</exception>
        /// <exception cref="InvalidOperationException" >thrown when EdgesCollection is uninitialized</exception>
        /// <exception cref="KeyNotFoundException" >thrown when the edge name does not exist.</exception>
        /// <exception cref="NullReferenceException" >thrown when the edge State is null</exception>
        /// <exception cref="TimeoutException" >thrown when obtaining the mutex times out</exception>
        public void TraverseEdgeForward(string initEdgeName)
        {
            if (_mtxEdgesCollection == null)
            {
                string exceptionMsg = $"The edge {initEdgeName} could not be retrieved because the mutex has been abanoned.";
                throw new AbandonedMutexException(exceptionMsg);
            }

            CheckEdgesCollection();

            if (!_mtxEdgesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The edge {initEdgeName} could not be retrieved because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedEdgeName = _stringUtility.ScrubName(initEdgeName);
                if (!EdgesCollection.ContainsKey(scrubbedEdgeName))
                {
                    throw new KeyNotFoundException($"Edge {scrubbedEdgeName} not found on Node {Name}.");
                }

                IEdge foundEdge = EdgesCollection[scrubbedEdgeName];
                foundEdge.Forward(this);
            }
            finally
            {
                _mtxEdgesCollection.ReleaseMutex();
            }
        }

        /// <summary>
        /// perform the operation associated with this call
        /// </summary>
        /// <param name="initEdgeName">string:: the name of the edge</param>
        /// <exception cref="AbandonedMutexException" >thrown when the mutex is null</exception>
        /// <exception cref="InvalidOperationException" >thrown when EdgesCollection is uninitialized</exception>
        /// <exception cref="KeyNotFoundException" >thrown when the edge name does not exist.</exception>
        /// <exception cref="NullReferenceException" >thrown when the edge State is null</exception>
        /// <exception cref="TimeoutException" >thrown when obtaining the mutex times out</exception>
        public void TraverseEdgeReverse(string initEdgeName)
        {
            if (_mtxEdgesCollection == null)
            {
                string exceptionMsg = $"The edge {initEdgeName} could not be retrieved because the mutex has been abanoned.";
                throw new AbandonedMutexException(exceptionMsg);
            }

            CheckEdgesCollection();

            if (!_mtxEdgesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The edge {initEdgeName} could not be retrieved because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedEdgeName = _stringUtility.ScrubName(initEdgeName);
                if (!EdgesCollection.ContainsKey(scrubbedEdgeName))
                {
                    throw new KeyNotFoundException($"Edge {scrubbedEdgeName} not found on Node {Name}.");
                }

                IEdge foundEdge = EdgesCollection[scrubbedEdgeName];
                foundEdge.Reverse(this);
            }
            finally
            {
                _mtxEdgesCollection.ReleaseMutex();
            }
        }
        #endregion

        #region INodeInternal Support
        // methods
        /// <summary>
        /// INTERNAL Create an edge with default initialization of edge State
        /// </summary>
        /// <typeparam name="TEdge">The type of edge state. constraints - where TEdge : class, IEdgeState, IDisposable, new()</typeparam>
        /// <param name="initName">string:: name of the edge</param>
        /// <param name="initTerminalNode">INode:: the terminal node</param>
        /// <param name="initIsDirected">bool:: if true, it is directed, otherwise it is bidirectional</param>
        /// <returns>IEdge:: the created edge</returns>
        /// <exception cref="AbandonedMutexException" >thrown when the mutex is null</exception>
        /// <exception cref="ApplicationException" >thrown when the Edge cannot be added to EdgesCollection</exception>
        /// <exception cref="ArgumentException" >thrown when the edge name already exists</exception>
        /// <exception cref="InvalidOperationException" >thrown when the Edges Collection is uninitialized</exception>
        /// <exception cref="TimeoutException" >thrown when obtaining the mutex times out</exception>
        /* internal */ IEdge INodeInternal.CreateEdge<TEdge>(string initName, INode initTerminalNode, bool initIsDirected)
        {
            return ((INodeInternal)this).CreateEdge<TEdge>(initName, new TEdge(), initTerminalNode, initIsDirected);
        }

        /// <summary>
        /// INTERNAL Create an edge with specific initialization of edge state
        /// </summary>
        /// <typeparam name="TEdge">The type of edge state. constraints - where TEdge : class, IEdgeState, IDisposable, new()</typeparam>
        /// <param name="initName">string:: name of the edge</param>
        /// <param name="initState">TEdge:: the initial State</param>
        /// <param name="initTerminalNode">INode:: the terminal node</param>
        /// <param name="initIsDirected">bool:: if true, it is directed, otherwise it is bidirectional</param>
        /// <returns>IEdge:: the created edge</returns>
        /// <exception cref="AbandonedMutexException" >thrown when the mutex is null</exception>
        /// <exception cref="ApplicationException" >thrown when the Edge cannot be added to EdgesCollection</exception>
        /// <exception cref="ArgumentException" >thrown when the edge name already exists</exception>
        /// <exception cref="InvalidOperationException" >thrown when the Edges Collection is uninitialized</exception>
        /// <exception cref="TimeoutException" >thrown when obtaining the mutex times out</exception>
        /* internal */ IEdge INodeInternal.CreateEdge<TEdge>(string initName, TEdge initState, INode initTerminalNode, bool initIsDirected) /* where TEdge : class, IDisposable, new() */
        {
            IEdge returnValue = null;

            if (_mtxEdgesCollection == null)
            {
                string exceptionMsg = $"The edge {initName} could not be created because the mutex has been abanoned.";
                throw new AbandonedMutexException(exceptionMsg);
            }

            CheckEdgesCollection();

            if (!_mtxEdgesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The edge {initName} could not be created because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedName = _stringUtility.ScrubName(initName);
                if (EdgesCollection.ContainsKey(scrubbedName))
                {
                    string exceptionText = $"The edge {scrubbedName} already exists";
                    throw new ArgumentException(exceptionText);
                }

                returnValue = new Edge<TEdge>(scrubbedName, initState, initTerminalNode, initIsDirected);
                if (!EdgesCollection.TryAdd(scrubbedName, returnValue))
                {
                    string exceptionText = $"Edge {scrubbedName} could not be added to node {Name} EdgesCollection.";
                    throw new ApplicationException(exceptionText);
                }
            }
            finally
            {
                _mtxEdgesCollection.ReleaseMutex();
            }

            return returnValue;
        }

        /// <summary>
        /// INTERNAL deletes an edge
        /// </summary>
        /// <param name="initEdgeName">string:: the name of the edge</param>
        /// <exception cref="AbandonedMutexException" >thrown when the mutex is null</exception>
        /// <exception cref="ApplicationException" >thrown when the edge cannot be removed from EdgesCollection</exception>
        /// <exception cref="ArgumentNullException" >thrown when the edge name is null</exception>
        /// <exception cref="InvalidOperationException" >thrown when EdgesCollection is uninitialized</exception>
        /// <exception cref="TimeoutException" >thrown when obtaining the mutex times out</exception>
        /* internal */ void INodeInternal.DeleteEdge(string initEdgeName)
        {
            if (string.IsNullOrWhiteSpace(initEdgeName))
            {
                throw new ArgumentNullException("initEdgeName");
            }

            if (_mtxEdgesCollection == null)
            {
                string exceptionMsg = $"The edge {initEdgeName} could not be deleted because the mutex has been abanoned.";
                throw new AbandonedMutexException(exceptionMsg);
            }

            CheckEdgesCollection();

            if (!_mtxEdgesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The edge {initEdgeName} could not be deleted because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedEdgeName = _stringUtility.ScrubName(initEdgeName);
                if (!EdgesCollection.ContainsKey(scrubbedEdgeName))
                {
                    string exceptionText = $"Edge {scrubbedEdgeName} does not exist in EdgesCollection.";
                    throw new KeyNotFoundException(exceptionText);
                }

                IEdge component = null;
                if (EdgesCollection.TryRemove(scrubbedEdgeName, out component))
                {
                    if (component != null) component.Dispose();
                }
                else
                {
                    string exceptionText = $"Edge {scrubbedEdgeName} could not be removed from node {Name} EdgesCollection.";
                    throw new ApplicationException(exceptionText);
                }
            }
            finally
            {
                _mtxEdgesCollection.ReleaseMutex();
            }

            return;
        }

        /// <summary>
        /// INTERNAL Deletes edges that terminate at a given node
        /// </summary>
        /// <param name="initNodeName">string:: the node name</param>
        /// <exception cref="AbandonedMutexException" >thrown when the mutex is null</exception>
        /// <exception cref="ApplicationException" >thrown when the edge cannot be removed from the EdgesCollection</exception>
        /// <exception cref="ArgumentNullException" >thrown when the node name is null</exception>
        /// <exception cref="InvalidOperationException" >thrown when EdgesCollection is uninitialized</exception>
        /// <exception cref="TimeoutException" >thrown when obtaining the mutex times out</exception>
        /* internal */ void INodeInternal.DeleteEdgesTerminatingOnNode(string initNodeName)
        {
            if(string.IsNullOrWhiteSpace(initNodeName))
            {
                throw new ArgumentNullException("initNodeName");
            }

            if (_mtxEdgesCollection == null)
            {
                string exceptionMsg = $"The edges on node {initNodeName} could not be iterated because the mutex has been abanoned.";
                throw new AbandonedMutexException(exceptionMsg);
            }

            CheckEdgesCollection();

            if (!_mtxEdgesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The edges on node {initNodeName} could not be iterated because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedNodeName = _stringUtility.ScrubName(initNodeName);
                // make a list of those to be removed 
                List<string> nameList = new List<string>();
                foreach (string name in EdgesCollection.Keys)
                {
                    IEdge edgeComponent = EdgesCollection[name];
                    if (edgeComponent.IsTerminalNode(scrubbedNodeName))
                    {
                        nameList.Add(name);
                    }
                }

                // remove the nodes that were found
                foreach (string name in nameList)
                {
                    IEdge edgeComponent = null;
                    EdgesCollection.TryRemove(name, out edgeComponent);
                    if (edgeComponent != null) edgeComponent.Dispose();
                }
            }
            finally
            {
                _mtxEdgesCollection.ReleaseMutex();
            }

            return;
        }

        #endregion


        #region IDisposable Support
        // this section was intenally generated by Visual Studio 

        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// dispose of managed and unmanaged objects
        /// </summary>
        /// <param name="disposing">bool:: true if called from code, false if called from finalizer</param>
        public void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        if(_mtxEdgesCollection != null)
                        {
                            // wait indefinitely
                            _mtxEdgesCollection.WaitOne();
                        }

                        if (_stringUtility != null)
                        {
                            _stringUtility.Dispose();
                        }

                        if (EdgesCollection != null)
                        {
                            foreach (string key in EdgesCollection.Keys)
                            {
                                IEdge item = EdgesCollection[key];
                                if (item != null)
                                {
                                    item.Dispose();
                                    item = null;
                                }
                            }

                            EdgesCollection.Clear();
                            EdgesCollection = null;
                        }
                    }
                    finally
                    {
                        if(_mtxEdgesCollection != null)
                        {
                            _mtxEdgesCollection.ReleaseMutex();
                            _mtxEdgesCollection.Close(); // will be disposed when last handle is lost
                        }
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        /// <summary>
        /// dispose this
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

        // private methods
        #region Private Methods
        /// <summary>
        /// ensure that EdgesCollection is instantiated
        /// </summary>
        /// <exception cref="InvalidOperationException" >thrown when the EdgesCollection is uninstantiated.</exception>
        private void CheckEdgesCollection()
        {
            if (EdgesCollection == null)
            {
                string exceptionText = "An internal initialization has failed for EdgesCollection.";
                throw new InvalidOperationException(exceptionText);
            }
        }
        #endregion
    }
}
