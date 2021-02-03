// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal class DefaultFunctionExecutionContextFactory : IFunctionExecutionContextFactory
    {
        private IServiceScopeFactory _serviceScopeFactory;

        public DefaultFunctionExecutionContextFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public FunctionExecutionContext Create(FunctionInvocation invocation, FunctionDefinition definition)
        {
            var context = new DefaultFunctionExecutionContext(_serviceScopeFactory, invocation, definition);

            return context;
        }
    }
}
