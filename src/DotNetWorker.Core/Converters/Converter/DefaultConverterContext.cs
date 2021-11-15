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
        public DefaultConverterContext(Type targetType, object? source, FunctionContext context, IReadOnlyDictionary<string, object> properties)
        {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(context));
            Source = source;
            FunctionContext = context ?? throw new ArgumentNullException(nameof(context));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        /// <inheritdoc/>
        public override Type TargetType { get; }

        /// <inheritdoc/>
        public override object? Source { get; }

        /// <inheritdoc/>
        public override FunctionContext FunctionContext { get; }

        /// <inheritdoc/>
        public override IReadOnlyDictionary<string, object> Properties { get; }
    }
}
