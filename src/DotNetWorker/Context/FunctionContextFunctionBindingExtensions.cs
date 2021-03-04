// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Context.Features;

namespace Microsoft.Azure.Functions.Worker
{
    internal static class FunctionContextFunctionBindingExtensions
    {
        /// <summary>
        /// Gets the function bindings feature for the current context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="IFunctionBindingsFeature"/>.</returns>
        /// <exception cref="InvalidOperationException">If there is no registered <see cref="IFunctionBindingsFeature"/>.</exception>        
        public static IFunctionBindingsFeature GetBindings(this FunctionContext context)
        {
            return context.Features.GetRequired<IFunctionBindingsFeature>();
        }
    }
}
