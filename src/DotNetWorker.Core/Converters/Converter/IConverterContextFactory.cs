// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An abstraction to create <see cref="ConverterContext"/> instances.
    /// </summary>
    internal interface IConverterContextFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="ConverterContext"/>
        /// </summary>
        /// <param name="targetType">The target type.</param>
        /// <param name="source">The source.</param>
        /// <param name="functionContext">The function context.</param>
        /// <returns>A new instance of <see cref="ConverterContext"/></returns>
        ConverterContext Create(Type targetType, object? source, FunctionContext functionContext);

        /// <summary>
        /// Creates a new instance of <see cref="ConverterContext"/>
        /// </summary>
        /// <param name="targetType">The target type.</param>
        /// <param name="source">The source.</param>
        /// <param name="functionContext">The function context.</param>
        /// <param name="properties">A property bag for specifying additional meta data.</param>
        /// <returns>A new instance of <see cref="ConverterContext"/></returns>
        ConverterContext Create(Type targetType, object? source, FunctionContext functionContext, IReadOnlyDictionary<string, object> properties);
    }
}
