// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Google.Protobuf.Collections;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Handlers
{
    internal class InvocationHandler : IInvocationHandler
    {
        private readonly IFunctionsApplication _application;
        private readonly IInvocationFeaturesFactory _invocationFeaturesFactory;
        private readonly IOutputBindingsInfoProvider _outputBindingsInfoProvider;
        private readonly IInputConversionFeatureProvider _inputConversionFeatureProvider;
        private readonly WorkerOptions _workerOptions;
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _inflightInvocations;

        public InvocationHandler(
            IFunctionsApplication application,
            IInvocationFeaturesFactory invocationFeaturesFactory,
            IOutputBindingsInfoProvider outputBindingsInfoProvider,
            IInputConversionFeatureProvider inputConversionFeatureProvider,
            IOptions<WorkerOptions> workerOptions,
            ILogger<InvocationHandler> logger)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _invocationFeaturesFactory = invocationFeaturesFactory ?? throw new ArgumentNullException(nameof(invocationFeaturesFactory));
            _outputBindingsInfoProvider = outputBindingsInfoProvider ?? throw new ArgumentNullException(nameof(outputBindingsInfoProvider));
            _inputConversionFeatureProvider = inputConversionFeatureProvider ?? throw new ArgumentNullException(nameof(inputConversionFeatureProvider));
            _workerOptions = workerOptions.Value ?? throw new ArgumentNullException(nameof(workerOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _inflightInvocations = new ConcurrentDictionary<string, CancellationTokenSource>();

            if (_workerOptions.Serializer is null)
            {
                throw new InvalidOperationException($"The {nameof(WorkerOptions)}.{nameof(WorkerOptions.Serializer)} is null");
            }
        }

        public async Task<InvocationResponse> InvokeAsync(InvocationRequest request)
        {
            using CancellationTokenSource cancellationTokenSource = new();
            FunctionContext? context = null;
            InvocationResponse response = new()
            {
                InvocationId = request.InvocationId,
                Result = new StatusResult()
            };

            if (!_inflightInvocations.TryAdd(request.InvocationId, cancellationTokenSource))
            {
                var exception = new InvalidOperationException("Unable to track CancellationTokenSource");
                response.Result.Status = StatusResult.Types.Status.Failure;
                response.Result.Exception = exception.ToRpcException();

                return response;
            }

            try
            {
                var invocation = new GrpcFunctionInvocation(request);

                IInvocationFeatures invocationFeatures = _invocationFeaturesFactory.Create();
                invocationFeatures.Set<FunctionInvocation>(invocation);
                invocationFeatures.Set<IExecutionRetryFeature>(invocation);

                context = _application.CreateContext(invocationFeatures, cancellationTokenSource.Token);
                invocationFeatures.Set<IFunctionBindingsFeature>(new GrpcFunctionBindingsFeature(context, request, _outputBindingsInfoProvider));

                if (_inputConversionFeatureProvider.TryCreate(typeof(DefaultInputConversionFeature), out var conversion))
                {
                    invocationFeatures.Set<IInputConversionFeature>(conversion!);
                }

                await _application.InvokeFunctionAsync(context);

                var serializer = _workerOptions.Serializer!;
                var functionBindings = context.GetBindings();

                foreach (var binding in functionBindings.OutputBindingData)
                {
                    var parameterBinding = new ParameterBinding
                    {
                        Name = binding.Key
                    };

                    if (binding.Value is not null)
                    {
                        parameterBinding.Data = await binding.Value.ToRpcAsync(serializer);
                    }

                    response.OutputData.Add(parameterBinding);
                }

                if (functionBindings.InvocationResult is not null)
                {
                    TypedData returnVal = await functionBindings.InvocationResult.ToRpcAsync(serializer);
                    response.ReturnValue = returnVal;
                }

                RpcTraceContext traceContext = AddTraceContextTags(request, context);
                response.TraceContext = traceContext;

                response.Result.Status = StatusResult.Types.Status.Success;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
#pragma warning disable CS0618 // Type or member is obsolete
                response.Result.Exception = _workerOptions.EnableUserCodeException ? ex.ToUserRpcException() : ex.ToRpcException();
#pragma warning restore CS0618 // Type or member is obsolete

                response.Result.Status = StatusResult.Types.Status.Failure;
                if (ex.InnerException is TaskCanceledException or OperationCanceledException)
                {
                    response.Result.Status = StatusResult.Types.Status.Cancelled;
                }
            }
            finally
            {
                _inflightInvocations.TryRemove(request.InvocationId, out var cts);

                if (context is IAsyncDisposable asyncContext)
                {
                    await asyncContext.DisposeAsync();
                }

                (context as IDisposable)?.Dispose();
            }

            return response;
        }

        public bool TryCancel(string invocationId)
        {
            if (_inflightInvocations.TryGetValue(invocationId, out var cancellationTokenSource))
            {
                try
                {
                    cancellationTokenSource?.Cancel();
                    return true;
                }
                catch (ObjectDisposedException)
                {
                    // Do nothing, normal behavior
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to cancel invocation '{invocationId}'.", invocationId);
                    throw;
                }
            }

            return false;
        }

        private RpcTraceContext AddTraceContextTags(InvocationRequest request, FunctionContext context)
        {
            RpcTraceContext traceContext = new RpcTraceContext
            {
                TraceParent = request.TraceContext.TraceParent,
                TraceState = request.TraceContext.TraceState,
                Attributes = { }
            };

            var invocationTags = context.Items.TryGetValue(TraceConstants.InternalKeys.FunctionContextItemsKey, out var tagsObj)
                ? tagsObj as System.Collections.Generic.IDictionary<string, string>
                : null;

            if (invocationTags is not null)
            {
                foreach (var tag in invocationTags)
                {
                    if (!TraceConstants.KnownAttributes.All.Contains(tag.Key))
                    {
                        traceContext.Attributes[tag.Key] = tag.Value;
                    }
                }
            }

            return traceContext;
        }
    }
}
