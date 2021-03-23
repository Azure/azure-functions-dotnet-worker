// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultFunctionActivator : IFunctionActivator
    {
        public object? CreateInstance(Type instanceType, FunctionContext context)
        {
            if (instanceType is null)
            {
                throw new ArgumentNullException(nameof(instanceType));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return ActivatorUtilities.CreateInstance(context.InstanceServices, instanceType, Array.Empty<object>());
        }
    }
}
