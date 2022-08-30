// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Handlers
{
    internal class InvocationHandler : IInvocationHandler
    {
        private readonly IFunctionsApplication _application;
        private readonly IInvocationFeaturesFactory _invocationFeaturesFactory;
        private readonly IOutputBindingsInfoProvider _outputBindingsInfoProvider;
        private readonly IInputConversionFeatureProvider _inputConversionFeatureProvider;
        private readonly ObjectSerializer _serializer;
        private readonly ILogger _logger;

        private ConcurrentDictionary<string, CancellationTokenSource> _inflightInvocations => new();

        public InvocationHandler(
            IFunctionsApplication application,
            IInvocationFeaturesFactory invocationFeaturesFactory,
            ObjectSerializer serializer,
            IOutputBindingsInfoProvider outputBindingsInfoProvider,
            IInputConversionFeatureProvider inputConversionFeatureProvider,
            ILoggerProvider loggerProvider)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _invocationFeaturesFactory = invocationFeaturesFactory ?? throw new ArgumentNullException(nameof(invocationFeaturesFactory));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _outputBindingsInfoProvider = outputBindingsInfoProvider ?? throw new ArgumentNullException(nameof(outputBindingsInfoProvider));
            _inputConversionFeatureProvider = inputConversionFeatureProvider ?? throw new ArgumentNullException(nameof(inputConversionFeatureProvider));
            _logger = loggerProvider.CreateLogger(nameof(InvocationHandler));
        }

        public async Task<InvocationResponse> InvokeAsync(InvocationRequest request)
        {
            using CancellationTokenSource cancellationTokenSource = new();
            FunctionContext? context = null;
            InvocationResponse response = new()
            {
                InvocationId = request.InvocationId,
                Result       = new StatusResult()
            };

            var token = cancellationTokenSource.Token;
            if (!_inflightInvocations.TryAdd(request.InvocationId, cancellationTokenSource))
            {
                // What do we want to do if we cannot keep track of a cancellation token source?
                token = CancellationToken.None;
                cancellationTokenSource.Dispose();
                _logger.LogInformation("Unable to store cancellation token source, providing an empty cancellation token");
            }

            try
            {
                var invocation = new GrpcFunctionInvocation(request);

                IInvocationFeatures invocationFeatures = _invocationFeaturesFactory.Create();
                invocationFeatures.Set<FunctionInvocation>(invocation);
                invocationFeatures.Set<IExecutionRetryFeature>(invocation);

                context = _application.CreateContext(invocationFeatures, token);
                invocationFeatures.Set<IFunctionBindingsFeature>(new GrpcFunctionBindingsFeature(context, request, _outputBindingsInfoProvider));

                if (_inputConversionFeatureProvider.TryCreate(typeof(DefaultInputConversionFeature), out var conversion))
                {
                    invocationFeatures.Set<IInputConversionFeature>(conversion!);
                }

                await _application.InvokeFunctionAsync(context);

                var functionBindings = context.GetBindings();

                foreach (var binding in functionBindings.OutputBindingData)
                {
                    var parameterBinding = new ParameterBinding
                    {
                        Name = binding.Key
                    };

                    if (binding.Value is not null)
                    {
                        parameterBinding.Data = await binding.Value.ToRpcAsync(_serializer);
                    }

                    response.OutputData.Add(parameterBinding);
                }

                if (functionBindings.InvocationResult is not null)
                {
                    TypedData? returnVal = await functionBindings.InvocationResult.ToRpcAsync(_serializer);
                    response.ReturnValue = returnVal;
                }

                response.Result.Status = StatusResult.Types.Status.Success;
            }
            catch (Exception ex)
            {
                response.Result.Exception = ex.ToRpcException();
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

        public void Cancel(string invocationId)
        {
            _inflightInvocations.TryGetValue(invocationId, out var cancellationTokenSource);
            if (cancellationTokenSource is not CancellationTokenSource)
            {
                return;
            }

            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Do nothing, normal behavior
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Unable to cancel invocation {invocationId}.", ex);
            }
        }
    }
}
