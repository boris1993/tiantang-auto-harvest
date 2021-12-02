using System;
using System.Net;

namespace tiantang_auto_harvest.Exceptions
{
    /// <summary>
    /// Base class of all the user-defined exceptions.
    /// </summary>
    public abstract class BaseAppException : Exception
    {
        public HttpStatusCode ResponseStatusCode { get; set; }

        public BaseAppException(string message) : base(message)
        {
            ResponseStatusCode = HttpStatusCode.InternalServerError;
        }

        public BaseAppException(string message, HttpStatusCode responseStatusCode) : base(message)
        {
            ResponseStatusCode = responseStatusCode;
        }

        /// <summary>
        /// Removing the stack trace because we don't need it in these user-defined exceptions
        /// </summary>
        public override string StackTrace => null;
    }

    public class ExternalAPICallException : BaseAppException
    {
        public ExternalAPICallException(string message) : base(message, HttpStatusCode.InternalServerError)
        { }

        public ExternalAPICallException(string message, HttpStatusCode responseStatusCode) : base(message, responseStatusCode)
        { }
    }
}
