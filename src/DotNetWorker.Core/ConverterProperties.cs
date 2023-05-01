// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// Information about converter
    /// </summary>
    internal class ConverterProperties
    {
        /// <summary>
        /// Support for Json Deserialization by converter
        /// </summary>
        internal bool SupportsJsonDeserialization { get; set; }

        /// <summary>
        /// List of types supported by the converter
        /// </summary>
        internal List<Type> SupportedTypes { get; set; }
    }
}
