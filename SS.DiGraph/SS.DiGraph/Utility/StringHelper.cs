using System;
using System.Collections.Generic;
using System.Text;

namespace SS.DiGraph.Utility
{
    /// <summary>
    /// a helper class for strings
    /// </summary>
    public sealed class StringHelper : IDisposable 
    {
        /// <summary>
        /// scrubbing operation for names
        /// </summary>
        /// <param name="initName">string:: the name to scrub</param>
        /// <returns>string:: a scrubbed name</returns>
        internal string ScrubName(string initName)
        {
            return initName.Trim();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public void Dispose(bool disposing)
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
        // ~StringHelper() {
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
}
