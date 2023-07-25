// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions
{
    /// <summary>
    /// The exception that is thrown when an invalid content-type is provided.
    /// </summary>
    internal class InvalidContentTypeException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidContentTypeException class with a specified error message.
        /// </summary>
        /// <param name="actualContentType">The source that is being provided.</param>
        /// <param name="expectedContentType">The content type(s) that is supported.</param>
        public InvalidContentTypeException(string actualContentType, string expectedContentType)
            : base($"Unexpected content-type '{actualContentType}'. Only '{expectedContentType}' is supported.") { }

        /// <summary>
        /// Initializes a new instance of the InvalidContentTypeException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="actualContentType">The source that is being provided.</param>
        /// <param name="expectedContentType">The content type(s) that is supported.</param>
        /// <param name="innerException">The exception that is the cause of the current exception
        /// or a null reference if no inner exception is specified.</param>
        public InvalidContentTypeException(string actualContentType, string expectedContentType, Exception innerException)
            : base($"Unexpected content-type '{actualContentType}'. Only '{expectedContentType}' is supported.", innerException) { }
    }
}
