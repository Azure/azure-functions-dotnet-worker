// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.Diagnostics;
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
        private static readonly ActivitySource _activitySource = new ActivitySource("Microsoft.Azure.Functions.Worker");
        private IDictionary<string, ExecutionContext> _executionContexts = new Dictionary<string, ExecutionContext>();

        public FunctionsApplication(
            FunctionExecutionDelegate functionExecutionDelegate,
            IFunctionContextFactory functionContextFactory,
            IOptions<WorkerOptions> workerOptions,
            ILogger<FunctionsApplication> logger,
            IWorkerDiagnostics diagnostics)
        {
            _functionExecutionDelegate = functionExecutionDelegate ?? throw new ArgumentNullException(nameof(functionExecutionDelegate));
            _functionContextFactory = functionContextFactory ?? throw new ArgumentNullException(nameof(functionContextFactory));
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
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

        public Task InvokeFunctionAsync(FunctionContext context)
        {
            const string InvocationIdKey = "InvocationId";
            const string NameKey = "FunctionName";

            IEnumerable<KeyValuePair<string, object?>> GetTags(FunctionContext context)
            {
                yield return new KeyValuePair<string, object?>(InvocationIdKey, context.InvocationId);
                yield return new KeyValuePair<string, object?>(NameKey, context.FunctionDefinition.Name);
            }

            TaskCompletionSource<bool> complete = new TaskCompletionSource<bool>();

            ExecutionContext executionContext = _executionContexts[context.InvocationId];
            ExecutionContext.Run(executionContext, t =>
                {
                    var activity = _activitySource.StartActivity("Worker.Invoke", ActivityKind.Internal, Activity.Current.Context, tags: GetTags(context));
                    var scope = new FunctionInvocationScope(context.FunctionDefinition.Name, context.InvocationId);
                    using (_logger.BeginScope(scope))
                    {
                        _functionExecutionDelegate(context)
                            .ContinueWith(_ =>
                            {
                                activity?.Stop();
                                activity?.Dispose();
                                (t as TaskCompletionSource<bool>)!.TrySetResult(true);
                            });
                    }
                }, complete);

            return complete.Task;
        }

        public void RaiseHostTelemetryEvent(HostTelemetryEvent telemetry)
        {
            var tags = telemetry.Payload.Select(p => new System.Collections.Generic.KeyValuePair<string, object?>(p.Key, p.Value));

            switch (telemetry.EventName)
            {
                case "function.start":
                    var activity = _activitySource.StartActivity("Host.Invoke", ActivityKind.Server, telemetry.Payload["Traceparent"], tags);
                    _executionContexts[telemetry.InvocationId] = ExecutionContext.Capture()!;
                    break;
                case "input.start":
                    var context = _executionContexts[telemetry.InvocationId];
                    ExecutionContext.Run(context, _ =>
                    {
                        _activitySource.StartActivity("InputBindings", ActivityKind.Server);
                        _executionContexts[telemetry.InvocationId] = ExecutionContext.Capture()!;
                    }, null);
                    break;
                case "input.end":
                    context = _executionContexts[telemetry.InvocationId];
                    ExecutionContext.Run(context, _ =>
                    {
                        Activity.Current!.Stop();
                        _executionContexts[telemetry.InvocationId] = ExecutionContext.Capture()!;
                    }, null);
                    break;
                case "output.start":
                    context = _executionContexts[telemetry.InvocationId];
                    ExecutionContext.Run(context, _ =>
                    {
                        _activitySource.StartActivity("OutputBindings", ActivityKind.Server);
                        _executionContexts[telemetry.InvocationId] = ExecutionContext.Capture()!;
                    }, null);
                    break;
                case "output.end":
                    context = _executionContexts[telemetry.InvocationId];
                    ExecutionContext.Run(context, _ =>
                    {
                        Activity.Current!.Stop();
                        _executionContexts[telemetry.InvocationId] = ExecutionContext.Capture()!;
                    }, null);
                    break;
                case "function.end":
                    context = _executionContexts[telemetry.InvocationId];
                    _executionContexts.Remove(telemetry.InvocationId);
                    ExecutionContext.Run(context, _ =>
                    {
                        Activity.Current!.Stop();
                    }, null);
                    context.Dispose();
                    break;
                default:
                    break;
            }
        }
    }
}
