﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Diagnostics;
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
        private readonly IOptions<WorkerOptions> _workerOptions;
        private readonly ILogger<FunctionsApplication> _logger;
        private readonly IWorkerDiagnostics _diagnostics;
        private readonly FunctionActivitySourceFactory _functionActivitySourceFactory;

        public FunctionsApplication(
            FunctionExecutionDelegate functionExecutionDelegate,
            IFunctionContextFactory functionContextFactory,
            IOptions<WorkerOptions> workerOptions,
            ILogger<FunctionsApplication> logger,
            IWorkerDiagnostics diagnostics,
            FunctionActivitySourceFactory functionActivitySourceFactory)
        {
            _functionExecutionDelegate = functionExecutionDelegate ?? throw new ArgumentNullException(nameof(functionExecutionDelegate));
            _functionContextFactory = functionContextFactory ?? throw new ArgumentNullException(nameof(functionContextFactory));
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _functionActivitySourceFactory = functionActivitySourceFactory ?? throw new ArgumentNullException(nameof(functionActivitySourceFactory));
        }

        public FunctionContext CreateContext(IInvocationFeatures features, CancellationToken token = default)
        {
            var invocation = features.Get<FunctionInvocation>() ?? throw new InvalidOperationException($"The {nameof(FunctionInvocation)} feature is required.");

            var functionDefinition = _functionMap[invocation.FunctionId];
            features.Set<FunctionDefinition>(functionDefinition);

            return _functionContextFactory.Create(features, token);
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

        public async Task InvokeFunctionAsync(FunctionContext context)
        {
            Activity? activity = null;

            if (Activity.Current is null)
            {
                // This will act as an internal activity that represents remote Host activity. This cannot be tracked as this is not associate to an ActivitySource. 
                activity = new Activity(nameof(InvokeFunctionAsync));
                activity.Start();

                if (ActivityContext.TryParse(context.TraceContext.TraceParent, context.TraceContext.TraceState, true, out ActivityContext activityContext))
                {
                    activity.SetId(context.TraceContext.TraceParent);
                    activity.SetSpanId(activityContext.SpanId.ToString());
                    activity.SetTraceId(activityContext.TraceId.ToString());
                    activity.SetRootId(activityContext.TraceId.ToString());
                    activity.ActivityTraceFlags = activityContext.TraceFlags;
                    activity.TraceStateString = activityContext.TraceState;
                }
            }

            var scope = new FunctionInvocationScope(context.FunctionDefinition.Name, context.InvocationId);

            using var logScope = _logger.BeginScope(scope);
            using Activity? invokeActivity = _functionActivitySourceFactory.StartInvoke(context);

            try
            {
                await _functionExecutionDelegate(context);
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                Log.InvocationError(_logger, context.FunctionDefinition.Name, context.InvocationId, ex);

                throw;
            }

            invokeActivity?.Stop();
            activity?.Stop();
        }
    }
}
