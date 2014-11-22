using System;

namespace ServiceHelper
{
    public sealed class TickTimeoutEventArgs : EventArgs
    {
        public bool Abort
        {
            get;
            set;
        }
    }
}
