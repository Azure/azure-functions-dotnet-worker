// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Middleware;
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
        private readonly FunctionInvocationManager _functionInvocationManager;
        private readonly IOptions<WorkerOptions> _workerOptions;
        private readonly ILogger<FunctionsApplication> _logger;
        private readonly IWorkerDiagnostics _diagnostics;

        public FunctionsApplication(
            FunctionExecutionDelegate functionExecutionDelegate,
            IFunctionContextFactory functionContextFactory,
            FunctionInvocationManager functionInvocationManager,
            IOptions<WorkerOptions> workerOptions,
            ILogger<FunctionsApplication> logger,
            IWorkerDiagnostics diagnostics)
        {
            _functionExecutionDelegate = functionExecutionDelegate ?? throw new ArgumentNullException(nameof(functionExecutionDelegate));
            _functionContextFactory = functionContextFactory ?? throw new ArgumentNullException(nameof(functionContextFactory));
            _functionInvocationManager = functionInvocationManager ?? throw new ArgumentNullException(nameof(functionInvocationManager));
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public FunctionContext CreateContext(IInvocationFeatures features)
        {
            CancellationTokenSource? cancellationTokenSource = null;
            var invocation = features.Get<FunctionInvocation>() ?? throw new InvalidOperationException($"The {nameof(FunctionInvocation)} feature is required.");

            var functionDefinition = _functionMap[invocation.FunctionId];
            features.Set<FunctionDefinition>(functionDefinition);

            if (functionDefinition.Parameters.Where(p => p.Type == typeof(CancellationToken)).Any())
            {
                cancellationTokenSource = new CancellationTokenSource();
            }

            var token = cancellationTokenSource is null ? CancellationToken.None : cancellationTokenSource.Token;
            var context = _functionContextFactory.Create(features, token);

            var invocationDetails = new FunctionInvocationDetails()
                                        {
                                            FunctionContext = context,
                                            CancellationTokenSource = cancellationTokenSource ?? null,
                                        };

            _functionInvocationManager.TryAddInvocationDetails(context.InvocationId, invocationDetails);

            return context;
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

            _diagnostics.OnFunctionLoaded(definition);
        }

        public Task InvokeFunctionAsync(FunctionContext context)
        {
            var scope = new FunctionInvocationScope(context.FunctionDefinition.Name, context.InvocationId);
            using (_logger.BeginScope(scope))
            {
                return _functionExecutionDelegate(context);
            }
        }

        public void CancelInvocation(string invocationId)
        {
            var invocationDetails = _functionInvocationManager.TryGetInvocationDetails(invocationId);
            if (invocationDetails?.CancellationTokenSource is not null)
            {
                invocationDetails.CancellationTokenSource.Cancel();
            }
        }

        public void RemoveInvocationRecord(string invocationId)
        {
            _functionInvocationManager.TryRemoveInvocationDetails(invocationId);
        }
    }
}
