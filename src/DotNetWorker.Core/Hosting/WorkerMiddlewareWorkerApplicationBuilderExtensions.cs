// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Provides extension methods to work with Worker Middleware against a <see cref="IHostBuilder"/>.
    /// </summary>
    public static class MiddlewareWorkerApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="IFunctionsWorkerApplicationBuilder"/> to use the default set of middleware used by the worker, in the following order:
        /// <list type="number">
        ///     <item>
        ///         <description><see cref="OutputBindingsMiddleware"/></description>
        ///     </item>
        ///     <item>
        ///         <description><see cref="FunctionExecutionMiddleware"/></description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chanining.</returns>
        public static IFunctionsWorkerApplicationBuilder UseDefaultWorkerMiddleware(this IFunctionsWorkerApplicationBuilder builder)
        {
            return builder.UseOutputBindingsMiddleware()
                .UseFunctionExecutionMiddleware();
        }

        /// <summary>
        /// Configures the <see cref="IFunctionsWorkerApplicationBuilder"/> to use the default <see cref="FunctionExecutionMiddleware"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chanining.</returns>
        public static IFunctionsWorkerApplicationBuilder UseFunctionExecutionMiddleware(this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder.Services.Any(d => d.ServiceType == typeof(FunctionExecutionMiddleware)))
            {
                return builder;
            }

            builder.Services.AddSingleton<FunctionExecutionMiddleware>();

            builder.Use(next =>
            {
                return context =>
                {
                    var middleware = context.InstanceServices.GetRequiredService<FunctionExecutionMiddleware>();

                    return middleware.Invoke(context);
                };
            });

            return builder;
        }

        /// <summary>
        /// Configures the <see cref="IFunctionsWorkerApplicationBuilder"/> to use the default <see cref="OutputBindingsMiddleware"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chanining.</returns>
        public static IFunctionsWorkerApplicationBuilder UseOutputBindingsMiddleware(this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder.Services.Any(d => d.ServiceType == typeof(OutputBindingsMiddleware)))
            {
                return builder;
            }

            builder.Services.AddSingleton<OutputBindingsMiddleware>(p => throw new InvalidOperationException($"Type {nameof(OutputBindingsMiddleware)} cannot be resolved using the container."));

            builder.Use(next =>
            {
                return context =>
                {
                    return OutputBindingsMiddleware.Invoke(context, next);
                };
            });

            return builder;
        }

        /// <summary>
        /// Configures the <see cref="IFunctionsWorkerApplicationBuilder"/> to use the provided middleware type.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chanining.</returns>
        public static IFunctionsWorkerApplicationBuilder UseMiddleware<T>(this IFunctionsWorkerApplicationBuilder builder)
            where T : class, IFunctionsWorkerMiddleware
        {
            builder.Services.AddSingleton<T>();

            builder.Use(next =>
            {
                return context =>
                {
                    var middleware = context.InstanceServices.GetRequiredService<T>();

                    return middleware.Invoke(context, next);
                };
            });

            return builder;
        }

        /// <summary>
        /// Configures the <see cref="IFunctionsWorkerApplicationBuilder"/> to use the provided inline middleware delegate.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <param name="middleware">The middleware to add to the invocation pipeline.</param>
        /// <returns>The same <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static IFunctionsWorkerApplicationBuilder UseMiddleware(this IFunctionsWorkerApplicationBuilder builder, Func<FunctionContext, Func<Task>, Task> middleware)
        {

            builder.Use(next =>
            {
                return context =>
                {
                    return middleware(context, () => next.Invoke(context));
                };
            });

            return builder;
        }
    }
}
