using System;
using System.Threading;

namespace ServiceHelper
{
    /// <summary>
    /// Well-known documented constants.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// Represents an infinite wait timeout; also defined by Timeout.InfiniteTimeSpan in .NET 4.5 and above.
        /// </summary>
        public static readonly TimeSpan InfiniteTimeSpan = TimeSpan.FromMilliseconds(Timeout.Infinite);

        #region HResults

        /// <summary>
        /// One or more arguments are not correct.
        /// </summary>
        public const int ERROR_BAD_ARGUMENTS = unchecked((int)0x800700A0);

        /// <summary>
        /// The requested control is not valid for this service.
        /// </summary>
        public const int ERROR_INVALID_SERVICE_CONTROL = unchecked((int)0x8007041C);

        /// <summary>
        /// The service did not respond to the start or control request in a timely fashion.
        /// </summary>
        public const int ERROR_SERVICE_REQUEST_TIMEOUT = unchecked((int)0x8007041D);

        /// <summary>
        /// An instance of the service is already running.
        /// </summary>
        public const int ERROR_SERVICE_ALREADY_RUNNING = unchecked((int)0x80070420);

        /// <summary>
        /// An exception occurred in the service when handling the control request.
        /// </summary>
        public const int ERROR_EXCEPTION_IN_SERVICE = unchecked((int)0x80070428);

        #endregion
    }
}
