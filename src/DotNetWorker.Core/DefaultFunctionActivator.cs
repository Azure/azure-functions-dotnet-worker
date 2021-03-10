// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultFunctionActivator : IFunctionActivator
    {
        public T CreateInstance<T>(IServiceProvider services)
        {
            return ActivatorUtilities.CreateInstance<T>(services, Array.Empty<object>());
        }
    }
}
