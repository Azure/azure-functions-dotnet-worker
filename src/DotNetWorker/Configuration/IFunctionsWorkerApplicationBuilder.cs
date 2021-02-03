// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker
{
    public interface IFunctionsWorkerApplicationBuilder
    {
        IServiceCollection Services { get; }

        IFunctionsWorkerApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware);
    }
}
