// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// A factory for creating <see cref="ConverterContext"/> instances.
    /// </summary>
    internal sealed class DefaultConverterContextFactory : IConverterContextFactory
    {
        /// <inheritdoc/>
        public ConverterContext Create(Type targetType, object? source, FunctionContext functionContext)
        {
            return Create(targetType, source, functionContext, ImmutableDictionary<string, object>.Empty);
        }

        /// <inheritdoc/>
        public ConverterContext Create(Type targetType, object? source, FunctionContext functionContext,
            IReadOnlyDictionary<string, object> properties)
        {
            return new DefaultConverterContext(targetType, source, functionContext, properties);
        }
    }
}
