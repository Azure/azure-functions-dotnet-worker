// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    public static class FunctionExecutionContextExtensions
    {
        public static ILogger<T> GetLogger<T>(this FunctionContext context)
        {
            return context.InstanceServices.GetService<ILogger<T>>();
        }

        public static ILogger GetLogger(this FunctionContext context, string categoryName)
        {
            return context.InstanceServices
                   .GetService<ILoggerFactory>()
                   .CreateLogger(categoryName);
        }
    }
}
