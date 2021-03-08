﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    internal partial class FunctionsApplication : IFunctionsApplication
    {
        private readonly ConcurrentDictionary<string, FunctionDefinition> _functionMap = new ConcurrentDictionary<string, FunctionDefinition>();
        private readonly FunctionExecutionDelegate _functionExecutionDelegate;
        private readonly IFunctionContextFactory _functionContextFactory;
        private readonly ILogger<FunctionsApplication> _logger;
        private readonly IOptions<WorkerOptions> _workerOptions;

        public FunctionsApplication(FunctionExecutionDelegate functionExecutionDelegate, IFunctionContextFactory functionContextFactory,
             IOptions<WorkerOptions> workerOptions, ILogger<FunctionsApplication> logger)
        {
            _functionExecutionDelegate = functionExecutionDelegate ?? throw new ArgumentNullException(nameof(functionExecutionDelegate));
            _functionContextFactory = functionContextFactory ?? throw new ArgumentNullException(nameof(functionContextFactory));
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public FunctionContext CreateContext(IInvocationFeatures features)
        {
            var invocation = features.Get<FunctionInvocation>() ?? throw new InvalidOperationException($"The {nameof(FunctionInvocation)} feature is required.");

            var functionDefinition = _functionMap[invocation.FunctionId];
            features.Set<FunctionDefinition>(functionDefinition);

            return _functionContextFactory.Create(features);
        }

        public void LoadFunction(FunctionDefinition definition)
        {
            if (definition.Id is null)
            {
                throw new InvalidOperationException("The function ID for the current load request is invalid");
            }

            if (!_functionMap.TryAdd(definition.Id, definition))
            {
                throw new InvalidOperationException($"Unable to load Function '{definition.Name}'. A function with the id '{definition.Id}' name already exists.");
            }

            Log.FunctionDefinitionCreated(_logger, definition);
        }

        public Task InvokeFunctionAsync(FunctionContext context)
        {
            var scope = new FunctionInvocationScope(context.FunctionDefinition.Name, context.Id);
            using (_logger.BeginScope(scope))
            {
                return _functionExecutionDelegate(context);
            }
        }
    }
}
