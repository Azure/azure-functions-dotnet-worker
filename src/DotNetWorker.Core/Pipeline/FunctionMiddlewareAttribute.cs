// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Middleware
{
    /// <summary>
    /// Represents IFunctionsWorkerMiddleware that operates on an attribute-based.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class FunctionMiddlewareAttribute<T> : Attribute
        where T : class, IFunctionsWorkerMiddleware
    { }
}
