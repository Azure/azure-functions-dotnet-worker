// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions
{
    /// <summary>
    /// The exception that is thrown when an invalid binding source is provided.
    /// </summary>
    public class InvalidBindingSourceException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidBindingSourceException class with a specified error message.
        /// </summary>
        /// <param name="source">The source(s) that is supported.</param>
        public InvalidBindingSourceException(string source) : base($"Unexpected binding source. Only '{source}' is supported.") { }

        /// <summary>
        /// Initializes a new instance of the InvalidBindingSourceException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="source">The source(s) that is supported.</param>
        /// <param name="innerException">The exception that is the cause of the current exception
        /// or a null reference if no inner exception is specified..</param>
        public InvalidBindingSourceException(string source, Exception? innerException) : base($"Unexpected binding source. Only '{source}' is supported.", innerException) { }
    }
}
