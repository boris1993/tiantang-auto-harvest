using System;
using System.Net;

namespace tiantang_auto_harvest.Exceptions
{
    /// <summary>
    /// Base class of all the user-defined exceptions.
    /// </summary>
    public abstract class BaseAppException : Exception
    {
        public HttpStatusCode ResponseStatusCode { get; }

        protected BaseAppException(string message) : base(message)
        {
            ResponseStatusCode = HttpStatusCode.InternalServerError;
        }

        protected BaseAppException(string message, HttpStatusCode responseStatusCode) : base(message)
        {
            ResponseStatusCode = responseStatusCode;
        }

        /// <summary>
        /// Removing the stack trace because we don't need it in these user-defined exceptions
        /// </summary>
        public override string StackTrace
        {
            get => null;
        }
    }

    public class ExternalApiCallException : BaseAppException
    {
        public ExternalApiCallException(string message) : base(message, HttpStatusCode.InternalServerError)
        { }

        public ExternalApiCallException(string message, HttpStatusCode responseStatusCode) : base(message, responseStatusCode)
        { }
    }

    public class TaskCancelledException : BaseAppException
    {
        public TaskCancelledException() : base("任务被取消")
        { }
    }
}
