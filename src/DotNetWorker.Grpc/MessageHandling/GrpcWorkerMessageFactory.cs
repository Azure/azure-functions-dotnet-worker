// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MsgType = Microsoft.Azure.Functions.Worker.Grpc.Messages.StreamingMessage.ContentOneofCase;

namespace Microsoft.Azure.Functions.Worker.Grpc;

internal class GrpcWorkerMessageFactory : IGrpcWorkerMessageFactory
{
    private readonly IFunctionsApplication _functionsApplication;
    private readonly IInvocationFeaturesFactory _invocationFeaturesFactory;
    private readonly IOutputBindingsInfoProvider _outputBindingsInfoProvider;
    private readonly IInputConversionFeatureProvider _inputConversionFeatureProvider;
    private readonly IOptions<WorkerOptions> _workerOptions;
    private readonly IFunctionMetadataProvider _functionMetadataProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IMethodInfoLocator _methodInfoLocator;

    public GrpcWorkerMessageFactory(
        IFunctionsApplication functionsApplication,
        IInvocationFeaturesFactory invocationFeaturesFactory,
        IOutputBindingsInfoProvider outputBindingsInfoProvider,
        IInputConversionFeatureProvider inputConversionFeatureProvider,
        IOptions<WorkerOptions> workerOptions,
        IFunctionMetadataProvider functionMetadataProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        IMethodInfoLocator methodInfoLocator)
    {
        _functionsApplication = functionsApplication;
        _invocationFeaturesFactory = invocationFeaturesFactory;
        _outputBindingsInfoProvider = outputBindingsInfoProvider;
        _inputConversionFeatureProvider = inputConversionFeatureProvider;
        _workerOptions = workerOptions;
        _functionMetadataProvider = functionMetadataProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        _methodInfoLocator = methodInfoLocator;
    }

    public IGrpcWorkerMessageHandler? CreateHandler(MsgType msgType)
    {
        return msgType switch
        {
            MsgType.InvocationRequest => new InvocationRequestHandler(
                _functionsApplication,
                _invocationFeaturesFactory,
                _outputBindingsInfoProvider,
                _inputConversionFeatureProvider,
                _workerOptions),
            MsgType.WorkerInitRequest => new WorkerInitRequestHandler(),
            MsgType.WorkerStatusRequest => new WorkerStatusResponseHandler(),
            MsgType.FunctionsMetadataRequest => new FunctionsMetadataRequestHandler(
                _functionMetadataProvider),
            MsgType.WorkerTerminate => new WorkerTerminateHandler(
                _hostApplicationLifetime),
            MsgType.FunctionLoadRequest => new FunctionLoadRequestHandler(
                _methodInfoLocator,
                _functionsApplication),
            MsgType.FunctionEnvironmentReloadRequest => new FunctionEnvironmentReloadRequestHandler(),
            _ => null,
        };
    }
}
