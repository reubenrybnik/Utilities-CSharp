using System;
using System.Runtime.Serialization;

namespace ServiceHelper
{
    [Serializable]
    public sealed class ServiceTaskFailedException : Exception
    {
        public ServiceTaskFailedException()
            : this(null)
        {
        }

        public ServiceTaskFailedException(string message)
            : this(message, null)
        {
        }

        public ServiceTaskFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
            this.HResult = Constants.ERROR_EXCEPTION_IN_SERVICE;
        }

        public ServiceTaskFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    internal sealed class InvalidInstallOptionException : Exception
    {
        public InvalidInstallOptionException()
            : this(null)
        {
        }

        public InvalidInstallOptionException(string message)
            : this(message, Constants.ERROR_BAD_ARGUMENTS)
        {
        }

        public InvalidInstallOptionException(string message, int hr)
            : base(message)
        {
            this.HResult = hr;
        }

        public InvalidInstallOptionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
