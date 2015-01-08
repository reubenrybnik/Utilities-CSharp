using System;

namespace ServiceHelper
{
    /// <summary>
    /// Provides data and options relevant to a TickTimeout event.
    /// </summary>
    [Serializable]
    public sealed class TickTimeoutEventArgs : EventArgs
    {
        /// <summary>
        /// Set to <c>true</c> to abort the current service loop and <c>false</c> to allow the service thread
        /// to continue to run to completion. The default is <c>false</c>.
        /// </summary>
        public bool Abort
        {
            get;
            set;
        }
    }
}
