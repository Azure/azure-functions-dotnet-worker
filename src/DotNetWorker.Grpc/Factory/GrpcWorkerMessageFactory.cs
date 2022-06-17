using System;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Factory.Contracts;
using Microsoft.Azure.Functions.Worker.Grpc.Factory.Handlers;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MsgType = Microsoft.Azure.Functions.Worker.Grpc.Messages.StreamingMessage.ContentOneofCase;

namespace Microsoft.Azure.Functions.Worker.Grpc.Factory;

internal class GrpcWorkerMessageFactory : IGrpcWorkerMessageFactory
{
    private readonly IServiceProvider _provider;

    public GrpcWorkerMessageFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IGrpcWorkerMessageHandler? CreateHandler(MsgType msgType)
    {
        return msgType switch
        {
            MsgType.InvocationRequest => new InvocationRequestHandler(
                _provider.GetRequiredService<IFunctionsApplication>(),
                _provider.GetRequiredService<IInvocationFeaturesFactory>(),
                _provider.GetRequiredService<IOutputBindingsInfoProvider>(),
                _provider.GetRequiredService<IInputConversionFeatureProvider>(),
                _provider.GetRequiredService<IOptions<WorkerOptions>>()),
            MsgType.WorkerInitRequest => new WorkerInitRequestHandler(),
            MsgType.WorkerStatusRequest => new WorkerStatusResponseHandler(),
            MsgType.FunctionsMetadataRequest => new FunctionsMetadataRequestHandler(
                _provider.GetRequiredService<IFunctionMetadataProvider>()),
            MsgType.WorkerTerminate => new WorkerTerminateHandler(
                _provider.GetRequiredService<IHostApplicationLifetime>()),
            MsgType.FunctionLoadRequest => new FunctionLoadRequestHandler(
                _provider.GetRequiredService<IMethodInfoLocator>(),
                _provider.GetRequiredService<IFunctionsApplication>()),
            MsgType.FunctionEnvironmentReloadRequest => new FunctionEnvironmentReloadRequestHandler(),
            _ => null,
        };
    }
}
