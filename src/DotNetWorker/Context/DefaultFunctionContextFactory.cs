// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal class DefaultFunctionContextFactory : IFunctionContextFactory
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IInvocationFeaturesFactory _invocationFeatures;

        public DefaultFunctionContextFactory(IServiceScopeFactory serviceScopeFactory, IInvocationFeaturesFactory invocationFeaturesFactory)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new System.ArgumentNullException(nameof(serviceScopeFactory));
            _invocationFeatures = invocationFeaturesFactory ?? throw new System.ArgumentNullException(nameof(invocationFeaturesFactory));
        }

        public FunctionContext Create(FunctionInvocation invocation, FunctionDefinition definition)
        {
            return new DefaultFunctionContext(_serviceScopeFactory, invocation, definition, _invocationFeatures.Create());
        }
    }
}
