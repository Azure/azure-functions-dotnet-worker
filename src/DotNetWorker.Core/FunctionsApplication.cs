// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
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
        private readonly IFunctionTelemetryProvider _functionTelemetryProvider;

        public FunctionsApplication(
            FunctionExecutionDelegate functionExecutionDelegate,
            IFunctionContextFactory functionContextFactory,
            IOptions<WorkerOptions> workerOptions,
            ILogger<FunctionsApplication> logger,
            IWorkerDiagnostics diagnostics,
            IFunctionTelemetryProvider functionTelemetryProvider)
        {
            _functionExecutionDelegate = functionExecutionDelegate ?? throw new ArgumentNullException(nameof(functionExecutionDelegate));
            _functionContextFactory = functionContextFactory ?? throw new ArgumentNullException(nameof(functionContextFactory));
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _functionTelemetryProvider = functionTelemetryProvider ?? throw new ArgumentNullException(nameof(functionTelemetryProvider));
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
            using var logScope = _logger.BeginScope(_functionTelemetryProvider.GetScopeAttributes(context).ToList());
            using Activity? invokeActivity = _functionTelemetryProvider.StartActivityForInvocation(context);

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
            finally
            {
                var tags = invokeActivity?.Tags;

                if (tags is not null && context.Items is not null)
                {
                    var known = TraceConstants.KnownAttributes.All;
                    var validTags = new List<KeyValuePair<string, string>>();

                    foreach (var (key, value) in tags)
                    {
                        // avoid overwriting protected attributes
                        if (!known.Contains(key) && value is not null)
                        {
                            validTags.Add(new KeyValuePair<string, string>(key, value));
                        }
                    }

                    if (validTags.Count > 0)
                    {
                        context.Items[TraceConstants.FunctionContextKeys.FunctionContextItemsKey] = validTags;
                    }
                }
            }
        }
    }
}
