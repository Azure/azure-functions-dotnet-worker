// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace WorkerCoreNullCompatibility;

public static class CompileCompatibility
{
    public static void AddWorkerCoreWithNullConfigure(IServiceCollection services)
    {
        services.AddFunctionsWorkerCore(null);
    }
}
