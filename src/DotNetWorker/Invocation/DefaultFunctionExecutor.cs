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
        private ConcurrentDictionary<string, IFunctionInvoker> _invokerCache = new ConcurrentDictionary<string, IFunctionInvoker>();

        private readonly ILogger<DefaultFunctionExecutor> _logger;
        private readonly IFunctionInvokerFactory _invokerFactory;

        public DefaultFunctionExecutor(IFunctionInvokerFactory invokerFactory, ILogger<DefaultFunctionExecutor> logger)
        {
            _invokerFactory = invokerFactory ?? throw new ArgumentNullException(nameof(invokerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync(FunctionContext context)
        {
            var invoker = _invokerCache.GetOrAdd(context.Invocation.FunctionId,
                _ => _invokerFactory.Create(context.FunctionDefinition.Metadata));

            object? instance = invoker.CreateInstance(context.InstanceServices);
            var bindingFeature = context.Features.Get<IModelBindingFeature>();

            object?[] inputArguments;
            if (bindingFeature is null)
            {
                Log.FunctionBindingFeatureUnavailable(_logger, context);
                inputArguments = new object?[context.FunctionDefinition.Parameters.Length];
            }
            else
            {
                inputArguments = bindingFeature.BindFunctionInput(context);
            }

            object? result = await invoker.InvokeAsync(instance, inputArguments);

            context.InvocationResult = result;
        }
    }
}
