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

        public async ValueTask ExecuteAsync(FunctionContext context)
        {
            var invoker = _invokerCache.GetOrAdd(context.FunctionId,
                static (_, state) =>
                {
                    var (factory, context) = state;
                    return factory.Create(context.FunctionDefinition);
                }, (_invokerFactory, context));

            object? instance = invoker.CreateInstance(context);
            var inputBindingFeature = context.Features.Get<IFunctionInputBindingFeature>();

            FunctionInputBindingResult inputBindingResult;
            if (inputBindingFeature is null)
            {
                Log.FunctionInputFeatureUnavailable(_logger, context);
                var emptyArgsArray = new object?[context.FunctionDefinition.Parameters.Length];
                inputBindingResult = new(emptyArgsArray);
            }
            else
            {
                inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context);
            }

            context.GetBindings().InvocationResult = await invoker.InvokeAsync(instance, inputBindingResult.Values);
        }
    }
}
