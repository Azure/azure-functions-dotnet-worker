// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal class DefaultFunctionContextFactory : IFunctionContextFactory
    {
        private IServiceScopeFactory _serviceScopeFactory;

        public DefaultFunctionContextFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public FunctionContext Create(FunctionInvocation invocation, FunctionDefinition definition)
        {
            var context = new DefaultFunctionContext(_serviceScopeFactory, invocation, definition);

            return context;
        }
    }
}
