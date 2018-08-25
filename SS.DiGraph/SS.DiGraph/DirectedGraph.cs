using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SS.DiGraph.Interfaces;
using SS.DiGraph.Utility;

namespace SS.DiGraph
{
    /// <summary>
    /// the directed graph class. it contains a collection of nodes.
    /// </summary>
    public sealed class DirectedGraph : IDirectedGraph
    {
        // constants
        private static readonly TimeSpan MAX_WAIT = TimeSpan.FromSeconds(120);

        // fields
        private readonly StringHelper _stringUtility;
        private readonly Mutex _mtxNodesCollection;
        private readonly TimeSpan _mtxTimeout;

        // properties
        private ConcurrentDictionary<string, INode> NodesCollection { get; set; }
        
        // constructors
        /// <summary>
        /// default constructor
        /// </summary>
        public DirectedGraph() : this(MAX_WAIT)
        {

        }

        /// <summary>
        /// parameterized constructor
        /// </summary>
        /// <param name="initGeneralTimeout">Timespan:: the duration to wait for mutexes. Max is MAX_WAIT.</param>
        /// <exception cref="ArgumentNullException">thrown when initGeneralTimeout is null</exception>
        public DirectedGraph(TimeSpan initGeneralTimeout)
        {
            if(initGeneralTimeout == null)
            {
                throw new ArgumentNullException("initGeneralTimeout");
            }

            Guid mutexName = Guid.NewGuid();
            _mtxNodesCollection = new Mutex(false, mutexName.ToString());

            // clamping the timeout between [0, MAX_WAIT]
            TimeSpan minTimeout = initGeneralTimeout < MAX_WAIT ? initGeneralTimeout : MAX_WAIT;
            _mtxTimeout = minTimeout > TimeSpan.Zero ? minTimeout : TimeSpan.Zero;

            _stringUtility = new StringHelper();
            NodesCollection = new ConcurrentDictionary<string, INode>();
        }

        // methods
        // Node methods
        /// <summary>
        /// Creates a Node on the DiGraph
        /// </summary>
        /// <typeparam name="T">Type for the Status of the Node. it will be instantiated with default values.</typeparam>
        /// <param name="initName">string:: the name of the node. it must be unique within the DiGraph. all leading and trailing whitespace is trimmed.</param>
        /// <returns>INode:: on success the object, otherwise null</returns>
        /// <exception cref="ArgumentNullException">thrown when the name is null or whitespace</exception>
        /// <exception cref="InvalidOperationException">thrown when NodesCollection is uninstantiated</exception>
        public INode CreateNode<T>(string initName) where T : class, IDisposable, new()
        {
            return CreateNode<T>(initName, new T());
        }

        /// <summary>
        /// Creates a Node on the DiGraph
        /// </summary>
        /// <typeparam name="T">Type for the Status of the Node.</typeparam>
        /// <param name="initName">string:: the name of the node. it must be unique within the DiGraph. all leading and trailing whitespace is trimmed.</param>
        /// <param name="initState">T:: the initial state of the Node.</param>
        /// <returns>INode:: on success the object, otherwise null</returns>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="ApplicationException">thrown when the Node cannot be added to NodesCollection</exception>
        /// <exception cref="ArgumentException">thrown when the name already exists</exception>
        /// <exception cref="ArgumentNullException">thrown when the name is null or whitespace</exception>
        /// <exception cref="InvalidOperationException">thrown when NodesCollection is uninstantiated</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public INode CreateNode<T>(string initName, T initState) where T : class, IDisposable, new()
        {
            INode returnValue = null;

            if(string.IsNullOrWhiteSpace(initName))
            {
                throw new ArgumentNullException("initName");
            }

            if(_mtxNodesCollection == null)
            {
                string exceptionMsg = $"The node {initName} could not be created because the mutex has been abanoned.";
                throw new AbandonedMutexException();
            }

            CheckNodesCollection();

            if (!_mtxNodesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The node {initName} could not be created because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                if (NodesCollection.ContainsKey(initName))
                {
                    string exceptionText = $"The node {initName} already exists.";
                    throw new ArgumentException(exceptionText);
                }

                string scrubbedName = _stringUtility.ScrubName(initName);
                returnValue = new Node<T>(scrubbedName, initState, _mtxTimeout);
                if (!NodesCollection.TryAdd(initName, returnValue))
                {
                    string exceptionText = $"The node {initName} could not be added to NodesCollection.";
                    throw new ApplicationException(exceptionText);
                }
            }
            finally
            {
                _mtxNodesCollection.ReleaseMutex();
            }

            return returnValue;
        }

        /// <summary>
        /// creates multiple nodes with the same state type from a string collection
        /// </summary>
        /// <typeparam name="T">Type for the Status of the Node. it will be instantiated with default values.</typeparam>
        /// <param name="initNames">IEnumerable&lt;string&gt;:: A collection of node names.</param>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="ArgumentNullException" >thrown if the name collection parameter is null</exception>
        /// <exception cref="InvalidOperationException" >thrown then NodesCollection is uninstantiated</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public void CreateNodes<T>(IEnumerable<string> initNames) where T : class, IDisposable, new()
        {
            if(initNames == null)
            {
                throw new ArgumentNullException("initNames");
            }

            if (_mtxNodesCollection == null)
            {
                string exceptionMsg = $"The node names could not be created because the mutex has been abanoned.";
                throw new AbandonedMutexException();
            }

            CheckNodesCollection();

            if (!_mtxNodesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The node names could not be created because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            // since a collection of nodes are being added, omit error notifications
            try
            {
                Parallel.ForEach<string>(initNames, (name) =>
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        string scrubbedName = _stringUtility.ScrubName(name);
                        if (!NodesCollection.ContainsKey(scrubbedName))
                        {
                            INode nodeItem = CreateNode<T>(scrubbedName);
                            NodesCollection.TryAdd(scrubbedName, nodeItem);
                        }
                    }
                });
            }
            finally
            {
                _mtxNodesCollection.ReleaseMutex();
            }

            return;
        }

        /// <summary>
        /// creates multiple nodes with the same state type from a Dictionary of node names and initial states
        /// </summary>
        /// <typeparam name="T">Type for the Status of the Node.</typeparam>
        /// <param name="initDictNamesStates">IDictionary&lt;string, T&gt;:: the dictionary containing the names and initial states</param>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="ArgumentNullException" >thrown if the dictionary parameter is null</exception>
        /// <exception cref="InvalidOperationException" >thrown then NodesCollection is uninstantiated</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public void CreateNodes<T>(IDictionary<string, T> initDictNamesStates) where T : class, IDisposable, new()
        {
            if(initDictNamesStates == null)
            {
                throw new ArgumentNullException("initDictNameStates");
            }

            if (_mtxNodesCollection == null)
            {
                string exceptionMsg = $"The dictionary of node names could not be created because the mutex has been abanoned.";
                throw new AbandonedMutexException();
            }

            CheckNodesCollection();

            if (!_mtxNodesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The node names could not be created because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                // since a collection of nodes are being added, omit error notifications and
                // report true if at least one succeeds.
                Parallel.ForEach<string>(initDictNamesStates.Keys, (name) =>
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        string scrubbedName = _stringUtility.ScrubName(name);
                        T state = initDictNamesStates[name];
                        if (!NodesCollection.ContainsKey(scrubbedName))
                        {
                            INode nodeItem = CreateNode<T>(scrubbedName, state);
                            NodesCollection.TryAdd(scrubbedName, nodeItem);
                        }
                    }
                });
            }
            finally
            {
                _mtxNodesCollection.ReleaseMutex();
            }

            return;
        }

        /// <summary>
        /// retrieve a specific node
        /// </summary>
        /// <param name="initName">string:: the node name</param>
        /// <returns>INode:: the requested node</returns>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="ArgumentNullException" >thrown if the name parameter is null</exception>
        /// <exception cref="InvalidOperationException" >thrown when NodesCollection is uninstantiated</exception>
        /// <exception cref="KeyNotFoundException" >thrown when the name does not appear in the NodesCollection</exception>
        /// <exception cref="NullReferenceException" >thrown when the node is null</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public INode GetNode(string initName)
        {
            INode returnObject = null;

            if(string.IsNullOrWhiteSpace(initName))
            {
                throw new ArgumentNullException("initName");
            }

            if (_mtxNodesCollection == null)
            {
                string exceptionMsg = $"The node {initName} could not be retrieved because the mutex has been abanoned.";
                throw new AbandonedMutexException();
            }

            CheckNodesCollection();

            if (!_mtxNodesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The node {initName} could not be retrieved because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedName = _stringUtility.ScrubName(initName);
                if (!NodesCollection.ContainsKey(scrubbedName))
                {
                    string exceptionText = $"Node {scrubbedName} does not exist in NodesCollection.";
                    throw new KeyNotFoundException(exceptionText);
                }

                returnObject = NodesCollection[scrubbedName];
                if (returnObject == null)
                {
                    string exceptionText = $"Node {scrubbedName} is null in NodesCollection.";
                    throw new NullReferenceException(exceptionText);
                }
            }
            finally
            {
                _mtxNodesCollection.ReleaseMutex();
            }
            return returnObject;
        }

        /// <summary>
        /// remove a node and all of its edges
        /// </summary>
        /// <param name="initName">string:: the node name to remove</param>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="ApplicationException" >thrown when the node cannot be removed from the NodesCollection</exception>
        /// <exception cref="ArgumentNullException" >thrown when the name parameter is null</exception>
        /// <exception cref="InvalidOperationException" >thrown then NodesCollection or EdgesCollection is uninstantiated</exception>
        /// <exception cref="KeyNotFoundException" >thrown when the node does not exist in NodesCollection</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public void DeleteNode(string initName)
        {
            if(string.IsNullOrWhiteSpace(initName))
            {
                throw new ArgumentNullException("initName");
            }

            if (_mtxNodesCollection == null)
            {
                string exceptionMsg = $"The node {initName} could not be deleted because the mutex has been abanoned.";
                throw new AbandonedMutexException();
            }

            CheckNodesCollection();

            if (!_mtxNodesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The node {initName} could not be deleted because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedName = _stringUtility.ScrubName(initName);
                if (!NodesCollection.ContainsKey(scrubbedName))
                {
                    string exceptionText = $"Node {scrubbedName} does not exist in NodesCollection.";
                    throw new KeyNotFoundException(exceptionText);
                }

                // find edges using this node as a terminal node and remove them
                foreach (string nodeName in NodesCollection.Keys)
                {
                    INode nodeComponent = NodesCollection[nodeName];
                    ((INodeInternal)nodeComponent).DeleteEdgesTerminatingOnNode(scrubbedName);
                }

                // remove the node and its edges
                INode component = null;
                if (NodesCollection.TryRemove(scrubbedName, out component))
                {
                    if (component != null) component.Dispose();
                }
                else
                {
                    string exceptionText = $"The node {scrubbedName} could not be removed from NodesCollection.";
                    throw new ApplicationException(exceptionText);
                }
            }
            finally
            {
                _mtxNodesCollection.ReleaseMutex();
            }

            return;
        }

        /// <summary>
        /// return the State of a node
        /// </summary>
        /// <param name="initName">string:: the requested node</param>
        /// <returns>object:: the State as an object</returns>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="ArgumentNullException" >thrown when the name parameter is null</exception>
        /// <exception cref="InvalidOperationException" >thrown when NodesCollection is uninstantiated</exception>
        /// <exception cref="KeyNotFoundException" >thrown when the node is not found</exception>
        /// <exception cref="NullReferenceException" >thrown when the node is null.</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public object GetNodeState(string initName)
        {
            object returnObject = null;

            if(string.IsNullOrWhiteSpace(initName))
            {
                throw new ArgumentNullException("initName");
            }

            if (_mtxNodesCollection == null)
            {
                string exceptionMsg = $"The state of node {initName} could not be retrieved because the mutex has been abanoned.";
                throw new AbandonedMutexException();
            }

            CheckNodesCollection();

            if (!_mtxNodesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The state of node {initName} could not be retrieved because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedName = _stringUtility.ScrubName(initName);
                if (!NodesCollection.ContainsKey(scrubbedName))
                {
                    string exceptionText = $"Node {scrubbedName} does not exist.";
                    throw new KeyNotFoundException(exceptionText);
                }

                INode nodeComponent = NodesCollection[scrubbedName];
                if (nodeComponent == null)
                {
                    string exceptionText = $"Node {scrubbedName} is null in NodesCollection.";
                    throw new NullReferenceException(exceptionText);
                }

                returnObject = nodeComponent.GetState();
            }
            finally
            {
                _mtxNodesCollection.ReleaseMutex();
            }

            return returnObject;
        }

        /// <summary>
        /// get a list of the node names
        /// </summary>
        /// <returns>List&lt;string&gt;:: a list of the node names, null if there are none</returns>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="InvalidOperationException" >thrown when NodesCollection is uninstantiated</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public List<string> GetNodeNames()
        {
            List<string> returnValue = new List<string>();

            if (_mtxNodesCollection == null)
            {
                string exceptionMsg = $"The nodes could not be iterated because the mutex has been abanoned.";
                throw new AbandonedMutexException();
            }

            CheckNodesCollection();

            if (!_mtxNodesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The nodes could not be iterated because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                foreach (string key in NodesCollection.Keys)
                {
                    returnValue.Add(key);
                }
            }
            finally
            {
                _mtxNodesCollection.ReleaseMutex();
            }

            return returnValue;
        }

        // Edge methods
        /// <summary>
        /// create an edge with a State of type TEdge, default initialization
        /// </summary>
        /// <typeparam name="TEdge">The type of the edge State</typeparam>
        /// <param name="initName">string:: the name of the edge. must be unique on the origin node.</param>
        /// <param name="initOriginNodeName">string:: the name of the origin node. must already exist.</param>
        /// <param name="initTerminalNodeName">string:: the name of the terminal node. must already exist.</param>
        /// <param name="initIsDirected">bool:: when true, this edge is directed, otherwise it is bidirectional</param>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="ArgumentNullException" >thrown when any of the three name parameters are null</exception>
        /// <exception cref="InvalidOperationException" >thrown when NodesCollection or EdgesCollection is uninstantiated</exception>
        /// <exception cref="KeyNotFoundException" >thrown when either of the two nodes do not exist.</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public void CreateEdge<TEdge>(string initName, string initOriginNodeName, string initTerminalNodeName, bool initIsDirected) where TEdge : class, IEdgeState, IDisposable, new()
        {
            CreateEdge<TEdge>(initName, new TEdge(), initOriginNodeName, initTerminalNodeName, initIsDirected);
        }

        /// <summary>
        /// create an edge with a State of type TEdge, copy initialization
        /// </summary>
        /// <typeparam name="TEdge">The type of the edge State</typeparam>
        /// <param name="initName">string:: the name of the edge. must be unique on the origin node.</param>
        /// <param name="initState">TEdge:: the initial state of the edge</param>
        /// <param name="initOriginNodeName">string:: the name of the origin node. must already exist.</param>
        /// <param name="initTerminalNodeName">string:: the name of the terminal node. must already exist.</param>
        /// <param name="initIsDirected">bool:: when true, this edge is directed, otherwise it is bidirectional</param>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="ArgumentNullException" >thrown when any of the three name parameters are null</exception>
        /// <exception cref="InvalidOperationException" >thrown when NodesCollection or EdgesCollection is uninstantiated</exception>
        /// <exception cref="KeyNotFoundException" >thrown when either of the two nodes do not exist.</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public void CreateEdge<TEdge>(string initName, TEdge initState, string initOriginNodeName, string initTerminalNodeName, bool initIsDirected) where TEdge : class, IEdgeState, IDisposable, new()
        {
            if(string.IsNullOrWhiteSpace(initName))
            {
                throw new ArgumentNullException("initName");
            }

            if(string.IsNullOrWhiteSpace(initOriginNodeName))
            {
                throw new ArgumentNullException("initOriginNode");
            }

            if(string.IsNullOrWhiteSpace(initTerminalNodeName))
            {
                throw new ArgumentNullException("initTerminalNodeName");
            }

            if (_mtxNodesCollection == null)
            {
                string exceptionMsg = $"The edge {initName} could not be created. The nodes could not be iterated because the mutex has been abanoned.";
                throw new AbandonedMutexException();
            }

            CheckNodesCollection();

            if (!_mtxNodesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The edge {initName} could not be created. The nodes could not be iterated because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedEdgeName = _stringUtility.ScrubName(initName);
                string scrubbedOriginNodeName = _stringUtility.ScrubName(initOriginNodeName);
                string scrubbedTerminalNodeName = _stringUtility.ScrubName(initTerminalNodeName);
                if (!NodesCollection.ContainsKey(scrubbedOriginNodeName))
                {
                    string exceptionText = $"The node {scrubbedOriginNodeName} does not exist";
                    throw new KeyNotFoundException(exceptionText);
                }

                if (!NodesCollection.ContainsKey(scrubbedTerminalNodeName))
                {
                    string exceptionText = $"The node {scrubbedTerminalNodeName} does not exist";
                    throw new KeyNotFoundException(exceptionText);
                }

                INode originNode = NodesCollection[scrubbedOriginNodeName];
                INode terminalNode = NodesCollection[scrubbedTerminalNodeName];
                IEdge returnValue = ((INodeInternal)originNode).CreateEdge<TEdge>(scrubbedEdgeName, initState, terminalNode, initIsDirected);
            }
            finally
            {
                _mtxNodesCollection.ReleaseMutex();
            }

            return;
        }

        /// <summary>
        /// remove an edge from a node
        /// </summary>
        /// <param name="initOriginNode">string:: the node that is the origin of the edge</param>
        /// <param name="initEdgeName">string:: the edge to remove</param>
        /// <returns>bool:: true on success, false otherwise</returns>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="ArgumentNullException" >thrown when either of the two name parameters are null</exception>
        /// <exception cref="InvalidOperationException" >thrown when NodesCollection or EdgesCollection is uninstantiated</exception>
        /// <exception cref="NullReferenceException" >thrown when the node is null in NodesCollection</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public void DeleteEdge(string initOriginNode, string initEdgeName)
        {
            if (string.IsNullOrWhiteSpace(initOriginNode))
            {
                throw new ArgumentNullException(initOriginNode);
            }

            if (string.IsNullOrWhiteSpace(initEdgeName))
            {
                throw new ArgumentNullException(initEdgeName);
            }

            if (_mtxNodesCollection == null)
            {
                string exceptionMsg = $"The edge {initEdgeName} could not be deleted. The nodes could not be iterated because the mutex has been abanoned.";
                throw new AbandonedMutexException();
            }

            CheckNodesCollection();

            if (!_mtxNodesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The edge {initEdgeName} could not be deleted. The nodes could not be iterated because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedEdgeName = _stringUtility.ScrubName(initEdgeName);
                string scrubbedOriginNode = _stringUtility.ScrubName(initOriginNode);
                if (NodesCollection.ContainsKey(scrubbedOriginNode))
                {
                    INode component = NodesCollection[scrubbedOriginNode];
                    if (component == null)
                    {
                        string exceptionText = $"Node {scrubbedOriginNode} is null.";
                        throw new NullReferenceException(exceptionText);
                    }

                }
            }
            finally
            {
                _mtxNodesCollection.ReleaseMutex();
            }

            return;
        }

        /// <summary>
        /// get the State of an edge
        /// </summary>
        /// <param name="initOriginNode">string:: the origin node holding the edge</param>
        /// <param name="initEdgeName">string:: the name of the edge</param>
        /// <returns>object:: the State as an object</returns>
        /// <exception cref="AbandonedMutexException">thrown when the mutex is null</exception>
        /// <exception cref="ArgumentNullException" >thrown when either of the two name parameters are null</exception>
        /// <exception cref="InvalidOperationException" >thrown when NodesCollection or EdgesCollection is uninstantiated</exception>
        /// <exception cref="KeyNotFoundException" >thrown when the node does not exist in NodesCollection or when the edge does not exist in EdgesCollection</exception>
        /// <exception cref="NullReferenceException" >thrown when the node is null in NodesCollection or when the edge is null in EdgesCollection</exception>
        /// <exception cref="TimeoutException">thrown when obtaining the mutex times out</exception>
        public object GetEdgeState(string initOriginNode, string initEdgeName)
        {
            object returnObject = null;

            if(string.IsNullOrWhiteSpace(initOriginNode))
            {
                throw new ArgumentNullException("initOriginNode");
            }

            if(string.IsNullOrWhiteSpace(initEdgeName))
            {
                throw new ArgumentNullException("initEdgeName");
            }

            if (_mtxNodesCollection == null)
            {
                string exceptionMsg = $"The edge {initEdgeName} could not be deleted. The nodes could not be iterated because the mutex has been abanoned.";
                throw new AbandonedMutexException();
            }

            CheckNodesCollection();

            if (!_mtxNodesCollection.WaitOne(_mtxTimeout))
            {
                // timed out waiting for the mutex
                string exceptionMsg = $"The edge {initEdgeName} could not be deleted. The nodes could not be iterated because a lock could not be obtained.";
                throw new TimeoutException(exceptionMsg);
            }

            try
            {
                string scrubbedEdgeName = _stringUtility.ScrubName(initEdgeName);
                string scrubbedOriginNode = _stringUtility.ScrubName(initOriginNode);
                if (!NodesCollection.ContainsKey(scrubbedOriginNode))
                {
                    string exceptionText = $"The node {scrubbedOriginNode} does not exist in NodesCollection.";
                    throw new KeyNotFoundException(exceptionText);
                }

                INode nodeComponent = NodesCollection[scrubbedOriginNode];
                if (nodeComponent == null)
                {
                    string exceptionText = $"The node {scrubbedOriginNode} is null in NodesCollection.";
                    throw new NullReferenceException(exceptionText);
                }

                returnObject = nodeComponent.GetEdgeState(scrubbedEdgeName);
            }
            finally
            {
                _mtxNodesCollection.ReleaseMutex();
            }

            return returnObject;
        }

        #region IDisposable Support
        // this section was intenally generated by Visual Studio 

        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Dispose of managed and unmanaged objects
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
                        if (_mtxNodesCollection != null)
                        {
                            _mtxNodesCollection.WaitOne();
                        }

                        // dispose managed state (managed objects).
                        if (_stringUtility != null)
                        {
                            _stringUtility.Dispose();
                        }

                        if (NodesCollection != null)
                        {
                            foreach (string key in NodesCollection.Keys)
                            {
                                INode item = NodesCollection[key];
                                // dispose each element
                                if (item != null)
                                {
                                    item.Dispose();
                                    item = null;
                                }
                            }

                            NodesCollection.Clear();
                            NodesCollection = null;
                        }
                    }
                    finally
                    {
                        if(_mtxNodesCollection != null)
                        {
                            _mtxNodesCollection.ReleaseMutex();
                            _mtxNodesCollection.Close(); // will be disposed when all references are lost
                        }
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        /// <summary>
        /// disposes this
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

        // private methods
        /// <summary>
        /// validates that the NodesCollection is intantiated, raises an Exception if not
        /// </summary>
        /// <exception cref="InvalidOperationException" >thrown when the NodesCollection is uninstantiated.</exception>
        private void CheckNodesCollection()
        {
            if (NodesCollection == null)
            {
                string exceptionText = "An internal initialization has failed for NodesCollection.";
                throw new InvalidOperationException(exceptionText);
            }
        }
    }

}

