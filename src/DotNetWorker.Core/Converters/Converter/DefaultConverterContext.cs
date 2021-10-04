// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// A type defining the information needed for an input conversion operation.
    /// </summary>
    internal sealed class DefaultConverterContext : ConverterContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="DefaultConverterContext"/>
        /// </summary>
        /// <param name="targetType">The target type.</param>
        /// <param name="source">The source.</param>
        /// <param name="context">The function context.</param>
        public DefaultConverterContext(Type targetType, object? source, FunctionContext context)
        {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(context));
            FunctionContext = context ?? throw new ArgumentNullException(nameof(context));
            Source = source;
        }

        /// <inheritdoc/>
        public override Type TargetType { get; set; }

        /// <inheritdoc/>
        public override object? Source { get; set; }

        /// <inheritdoc/>
        public override FunctionContext FunctionContext { get; set; }

        /// <inheritdoc/>
        public override IReadOnlyDictionary<string, object>? Properties { get; set;}
    }
}
