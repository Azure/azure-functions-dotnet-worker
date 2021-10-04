// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An abstraction to create <see cref="ConverterContext"/> instances.
    /// </summary>
    public interface IConverterContextFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="DefaultConverterContext"/>
        /// </summary>
        /// <param name="targetType">The target type.</param>
        /// <param name="source">The source.</param>
        /// <param name="functionContext">The function context.</param>
        /// <returns>A new instance of <see cref="DefaultConverterContext"/></returns>
        ConverterContext Create(Type targetType, object? source, FunctionContext functionContext);
    }
}
