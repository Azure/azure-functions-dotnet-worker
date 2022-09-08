// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal class DefaultFunctionContextFactory : IFunctionContextFactory
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DefaultFunctionContextFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public FunctionContext Create(IInvocationFeatures invocationFeatures, CancellationToken token = default)
        {
            return new DefaultFunctionContext(_serviceScopeFactory, invocationFeatures, token);
        }
    }
}
