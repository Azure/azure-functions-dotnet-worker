// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Describes the remote exception that caused a function invocation to be retried.
    /// </summary>
    /// <remarks>
    /// This is diagnostic data reported by the Functions host. It does not represent
    /// a locally throwable exception.
    /// </remarks>
    public sealed class FunctionRetryException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionRetryException"/> class.
        /// </summary>
        /// <param name="source">The remote exception source.</param>
        /// <param name="type">The remote exception type name.</param>
        /// <param name="message">The remote exception message.</param>
        /// <param name="stackTrace">The remote exception stack trace.</param>
        /// <param name="isUserException">
        /// A value indicating whether the remote failure originated in user code.
        /// </param>
        public FunctionRetryException(
            string source,
            string type,
            string message,
            string stackTrace,
            bool isUserException)
        {
            Source = source;
            Type = type;
            Message = message;
            StackTrace = stackTrace;
            IsUserException = isUserException;
        }

        /// <summary>Gets the remote exception source.</summary>
        public string Source { get; }

        /// <summary>Gets the remote exception type name.</summary>
        public string Type { get; }

        /// <summary>Gets the remote exception message.</summary>
        public string Message { get; }

        /// <summary>Gets the remote exception stack trace.</summary>
        public string StackTrace { get; }

        /// <summary>Gets a value indicating whether the remote failure originated in user code.</summary>
        public bool IsUserException { get; }
    }
}
