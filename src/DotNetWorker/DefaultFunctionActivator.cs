// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Azure.Functions.Worker
{
    public class DefaultFunctionActivator : IFunctionActivator
    {
        public T CreateInstance<T>(IServiceProvider services)
        {
            return ActivatorUtilities.CreateInstance<T>(services, Array.Empty<object>());
        }
    }


}
