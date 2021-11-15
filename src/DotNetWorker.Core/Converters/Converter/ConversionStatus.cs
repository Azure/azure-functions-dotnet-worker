// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Conversion status enumeration.
    /// </summary>
    public enum ConversionStatus
    {
        /// <summary>
        /// Converter did not act on the input to execute a conversion operation.
        /// </summary>
        Unhandled,

        /// <summary>
        /// Conversion operation was successful.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Conversion operation failed.
        /// </summary>
        Failed
    }
}
