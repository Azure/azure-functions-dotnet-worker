// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// <see cref="FunctionContext" /> extensions for <see cref="ILogger"/>.
    /// </summary>
    public static class FunctionContextLoggerExtensions
    {
        /// <summary>
        /// Creates a new <see cref="ILogger"/> instance using the full name of the given type.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="ILogger{T}"/>.</returns>
        public static ILogger<T> GetLogger<T>(this FunctionContext context)
        {
            return context.InstanceServices.GetService<ILogger<T>>()!;
        }

        /// <summary>
        /// Creates a new <see cref="ILogger"/> instance for the specified <see cref="FunctionContext"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>The <see cref="ILogger"/>.</returns>
        public static ILogger GetLogger(this FunctionContext context, string categoryName)
        {
            return context.InstanceServices
                    .GetService<ILoggerFactory>()!
                    .CreateLogger(categoryName);
        }
    }
}
