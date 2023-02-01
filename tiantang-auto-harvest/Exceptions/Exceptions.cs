using System;
using System.Net;
using System.Runtime.Serialization;

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

        protected BaseAppException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Removing the stack trace because we don't need it in these user-defined exceptions
        /// </summary>
        public override string StackTrace
        {
            get => null;
        }
    }

    [Serializable]
    public class ExternalApiCallException : BaseAppException
    {
        public ExternalApiCallException(string message) : base(message, HttpStatusCode.InternalServerError) { }

        public ExternalApiCallException(HttpStatusCode responseStatusCode) : base("HTTP请求失败", responseStatusCode) { }

        public ExternalApiCallException(string message, HttpStatusCode responseStatusCode) : base(message, responseStatusCode) { }

        protected ExternalApiCallException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class TaskCancelledException : BaseAppException
    {
        public TaskCancelledException() : base("任务被取消") { }

        protected TaskCancelledException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }
}
