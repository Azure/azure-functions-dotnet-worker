// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal partial class DefaultFunctionExecutor : IFunctionExecutor
    {
        private readonly ConcurrentDictionary<string, IFunctionInvoker> _invokerCache = new();

        private readonly ILogger<DefaultFunctionExecutor> _logger;
        private readonly IFunctionInvokerFactory _invokerFactory;

        public DefaultFunctionExecutor(IFunctionInvokerFactory invokerFactory, ILogger<DefaultFunctionExecutor> logger)
        {
            _invokerFactory = invokerFactory ?? throw new ArgumentNullException(nameof(invokerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync(FunctionContext context)
        {
            var invoker = _invokerCache.GetOrAdd(context.FunctionId,
                _ => _invokerFactory.Create(context.FunctionDefinition));

            object? instance = invoker.CreateInstance(context);
            var modelBindingFeature = context.Features.Get<IModelBindingFeature>();

            object?[] inputArguments;
            if (modelBindingFeature is null)
            {
                Log.ModelBindingFeatureUnavailable(_logger, context);
                inputArguments = new object?[context.FunctionDefinition.Parameters.Length];
            }
            else
            {
                inputArguments = await modelBindingFeature.BindFunctionInputAsync(context);
            }

            context.GetBindings().InvocationResult = await invoker.InvokeAsync(instance, inputArguments);
        }
    }
}
