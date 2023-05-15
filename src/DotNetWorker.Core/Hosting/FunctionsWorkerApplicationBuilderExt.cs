// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Configuration
{
    internal class FunctionsWorkerApplicationBuilderExt : FunctionsWorkerApplicationBuilder, IFunctionsWorkerApplicationBuilderExt
    {
        public FunctionsWorkerApplicationBuilderExt(IServiceCollection services, FunctionsWorkerApplicationBuilderContext context)
            : base(services)
        {
            Context = context;
        }

        public FunctionsWorkerApplicationBuilderContext Context { get; private set; }
    }
}
