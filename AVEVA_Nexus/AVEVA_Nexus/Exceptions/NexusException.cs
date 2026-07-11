using System;

namespace AVEVA_Nexus.Exceptions
{
    public class NexusException : Exception
    {
        public int StatusCode { get; }

        public NexusException(
            string message,
            int statusCode = 400)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}