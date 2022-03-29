// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// The exception that is thrown when input conversion for the function invocation fails.
    /// </summary>
    public class FunctionInputConverterException : FunctionWorkerException
    {
        /// <summary>
        /// Initializes a new instance of the FunctionInputConverterException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FunctionInputConverterException(string? message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the FunctionInputConverterException class with a specified error message
        /// and a reference to the inner exception that is thecause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception
        /// or a null reference if no inner exception is specified..</param>
        public FunctionInputConverterException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
