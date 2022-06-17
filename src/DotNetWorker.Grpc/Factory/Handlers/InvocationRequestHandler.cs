using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Factory.Contracts;
using Microsoft.Azure.Functions.Worker.Grpc.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Grpc.Factory.Handlers;

internal class InvocationRequestHandler : IGrpcWorkerMessageHandler
{
    private readonly IFunctionsApplication _application;
    private readonly IInvocationFeaturesFactory _invocationFeaturesFactory;
    private readonly IOutputBindingsInfoProvider _outputBindingsInfoProvider;
    private readonly IInputConversionFeatureProvider _inputConversionFeatureProvider;
    private readonly ObjectSerializer _serializer;

    public InvocationRequestHandler(
        IFunctionsApplication application!!,
        IInvocationFeaturesFactory invocationFeaturesFactory!!,
        IOutputBindingsInfoProvider outputBindingsInfoProvider!!,
        IInputConversionFeatureProvider inputConversionFeatureProvider!!,
        IOptions<WorkerOptions> workerOptions)
    {
        _application = application;
        _invocationFeaturesFactory = invocationFeaturesFactory;
        _outputBindingsInfoProvider = outputBindingsInfoProvider;
        _inputConversionFeatureProvider = inputConversionFeatureProvider;
        _serializer = workerOptions.Value.Serializer ?? throw new InvalidOperationException(nameof(workerOptions.Value.Serializer));
    }

    public async Task<StreamingMessage> HandleMessageAsync(StreamingMessage request)
    {
        StreamingMessage messageResponse = new StreamingMessage
        {
            RequestId = request.RequestId
        };

        FunctionContext? context = null;
        InvocationResponse response = new()
        {
            InvocationId = request.InvocationRequest.InvocationId,
        };

        try
        {
            var invocation = new GrpcFunctionInvocation(request.InvocationRequest);

            var invocationFeatures = _invocationFeaturesFactory.Create();
            invocationFeatures.Set<FunctionInvocation>(invocation);
            invocationFeatures.Set<IExecutionRetryFeature>(invocation);

            context = _application.CreateContext(invocationFeatures);
            invocationFeatures.Set<IFunctionBindingsFeature>(new GrpcFunctionBindingsFeature(
                context,
                request.InvocationRequest,
                _outputBindingsInfoProvider));

            if (_inputConversionFeatureProvider.TryCreate(typeof(DefaultInputConversionFeature), out var conversion))
            {
                invocationFeatures.Set(conversion!);
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

            if (functionBindings.InvocationResult != null)
            {
                var returnVal = await functionBindings.InvocationResult.ToRpcAsync(_serializer);

                response.ReturnValue = returnVal;
            }

            response.Result = new StatusResult
            {
                Status = StatusResult.Types.Status.Success
            };
        }
        catch (Exception ex)
        {
            response.Result = new StatusResult
            {
                Exception = ex.ToRpcException(),
                Status = StatusResult.Types.Status.Failure
            };
        }
        finally
        {
            (context as IDisposable)?.Dispose();
        }

        messageResponse.InvocationResponse = response;
        return messageResponse;
    }
}
