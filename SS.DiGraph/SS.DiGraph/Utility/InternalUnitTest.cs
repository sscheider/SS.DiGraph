using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SS.DiGraph.Interfaces;

namespace SS.DiGraph.Utility
{
    [ExcludeFromCodeCoverage]
    public class InternalUnitTest
    {
        /// <summary>
        /// PRIVATE class withing InternalUnitTest to create a node state
        /// </summary>
        private class NodeState : IDisposable 
        {
            public string ItemString { get; set; }

            public NodeState()
            {
                
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~NodeState() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion

        }

        /// <summary>
        /// PRIVATE class within InternalUnitTest to create an edge state
        /// </summary>
        private class EdgeState : IEdgeState
        {
            public string ItemString { get; set; }

            public EdgeState()
            {

            }

            public void ForwardPath(INode iniOriginNode, INode iniTerminalNode)
            {
                ItemString = "forward";
            }

            public void ReversePath(INode iniOriginNode, INode iniTerminalNode)
            {
                ItemString = "reverse";
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~EdgeState() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion

        }

        public bool ExecuteTests()
        {
            int assertFailedCount = 0;

            if (!TestEdge()) assertFailedCount++;
            if (!TestNode()) assertFailedCount++;
            if (!TestDigraph()) assertFailedCount++;
            return assertFailedCount == 0;
        }

        private bool TestEdge()
        {
            int assertFailCount = 0;

            // test constructors
            // arrange
            INode termNode1 = new Node<NodeState>("node1");
            EdgeState state2 = new EdgeState();
            INode termNode2 = new Node<NodeState>("node1");

            // act
            IEdge edge1 = new Edge<EdgeState>("edge1", termNode1);
            IEdge edge2 = new Edge<EdgeState>("edge2", state2, termNode2, true);

            // assert
            if (edge1 == null) assertFailCount++;
            if (edge2 == null) assertFailCount++;
            EdgeState state11 = (EdgeState)edge1.GetState();
            if (!string.IsNullOrWhiteSpace(state11.ItemString)) assertFailCount++;
            EdgeState state21 = (EdgeState)edge2.GetState();
            if (!string.IsNullOrWhiteSpace(state21.ItemString)) assertFailCount++;
            if (!edge1.IsTerminalNode(termNode1.Name)) assertFailCount++;
            if (!edge2.IsTerminalNode(termNode2.Name)) assertFailCount++;

            // test methods
            // arrange
            state2.ItemString = "1234";

            // act
            edge2.Forward(termNode1);

            // assert
            if (state2.ItemString != "forward") assertFailCount++;

            // arrange
            state2.ItemString = "4321";

            // act
            try
            {
                edge2.Reverse(termNode1);
                assertFailCount++;
            }
            catch(InvalidOperationException ioex)
            {
                // good state
            }
            catch(Exception ex)
            {
                assertFailCount++;
            }

            // assert
            if (state2.ItemString != "4321") assertFailCount++;

            // arrange

            // act
            
            // assert

            // pass testing into the object
            //if (!((IInternalUnitTestable)edge2).ExecuteTest()) assertFailCount++;

            // cleanup
            edge1.Dispose();
            edge2.Dispose();

            // report results
            return assertFailCount == 0;
        }

        private bool TestNode()
        {
            int assertFailCount = 0;

            // report results
            return assertFailCount == 0;
        }

        private bool TestDigraph()
        {
            int assertFailCount = 0;

            // report results
            return assertFailCount == 0;
        }
    }
}
