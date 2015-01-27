using System;
using System.Runtime.Serialization;

namespace ServiceHelper
{
    /// <summary>
    /// Thrown when an initialization or cleanup service task fails; typically wraps the exception
    /// that caused the failure.
    /// </summary>
    [Serializable]
    public sealed class ServiceTaskFailedException : Exception
    {
        /// <summary>
        /// Create a new ServiceTaskFailedException with no additional information.
        /// </summary>
        public ServiceTaskFailedException()
            : this(null)
        {
        }

        /// <summary>
        /// Create a new ServiceTaskFailedException with the specified message.
        /// </summary>
        /// <param name="message">The message to provide to code that catches this exception.</param>
        public ServiceTaskFailedException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Create a new ServiceTaskFailedException with the specified message that wraps a root cause exception.
        /// </summary>
        /// <param name="message">The message to provide to code that catches this exception.</param>
        /// <param name="innerException">The exception that provides more information on the root cause for the
        /// exceptional circumstance.</param>
        public ServiceTaskFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
            this.HResult = Constants.ERROR_EXCEPTION_IN_SERVICE;
        }

        /// <summary>
        /// Initializes a new ServiceTaskFailedException with serialized data.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo that holds the serialized object
        /// data about the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext that contains contextual
        /// information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">The info parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or System.Exception.HResult is zero (0).
        /// </exception>
        protected ServiceTaskFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Thrown during non-interactive service install when a specified service install option is not valid.
    /// </summary>
    [Serializable]
    public sealed class InvalidInstallOptionException : Exception
    {
        /// <summary>
        /// Create a new InvalidInstallOptionException with no additional information.
        /// </summary>
        public InvalidInstallOptionException()
            : this(null)
        {
        }

        /// <summary>
        /// Create a new InvalidInstallOptionException with the specified message.
        /// </summary>
        /// <param name="message">The message to provide to code that catches this exception.</param>
        public InvalidInstallOptionException(string message)
            : this(message, Constants.ERROR_BAD_ARGUMENTS)
        {
        }

        /// <summary>
        /// Create a new InvalidInstallOptionException with the specified message that wraps a root cause exception.
        /// </summary>
        /// <param name="message">The message to provide to code that catches this exception.</param>
        /// <param name="hResult">The Windows HResult that represents the cause of the installation failure.</param>
        public InvalidInstallOptionException(string message, int hResult)
            : base(message)
        {
            this.HResult = hResult;
        }

        /// <summary>
        /// Initializes a new InvalidInstallOptionException with serialized data.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo that holds the serialized object
        /// data about the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext that contains contextual
        /// information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">The info parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or System.Exception.HResult is zero (0).
        /// </exception>
        protected InvalidInstallOptionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
