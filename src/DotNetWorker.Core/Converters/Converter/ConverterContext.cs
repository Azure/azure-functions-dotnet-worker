// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// A type defining the information needed for an input conversion operation.
    /// </summary>
    public abstract class ConverterContext
    {
        /// <summary>
        /// The target type to which conversion should happen.
        /// </summary>
        public abstract Type TargetType { get; }

        /// <summary>
        /// The source data used for conversion.
        /// </summary>
        public abstract object? Source { get; }

        /// <summary>
        /// The function context.
        /// </summary>
        public abstract FunctionContext FunctionContext { get; }
                
        /// <summary>
        /// Property bag of additional meta information used for conversion.
        /// </summary>
        public abstract IReadOnlyDictionary<string, object> Properties { get; }
    }
}
