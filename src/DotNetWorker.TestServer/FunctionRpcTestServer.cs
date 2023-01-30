using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.Options;
using SampleIntegrationTests;

namespace Microsoft.Azure.Functions.Worker.TestServer;

internal class FunctionRpcTestServer : FunctionRpc.FunctionRpcBase, ITestServer
{
    private readonly WorkerOptions _options;
    private event EventHandler<InvocationRespondedEventArgs>? InvocationResponded;
    private IServerStreamWriter<StreamingMessage>? _serverStreamWriter;
    private readonly FunctionsLookup _functionsLookup = new FunctionsLookup();

    private class InvocationRespondedEventArgs : EventArgs
    {
        public InvocationResponse Response { get; }

        public InvocationRespondedEventArgs(InvocationResponse response)
        {
            Response = response;
        }
    }

    public FunctionRpcTestServer(IOptions<WorkerOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task StartAsync() => await _functionsLookup.WaitForFunctionsLoading();

    /// <inheritdoc />
    public async Task<FunctionResponse> CallAsync(string name, IDictionary<string, object> arguments,
        CancellationToken cancellationToken = default)
    {
        var function = _functionsLookup.Lookup(name);

        var result = await CallAsyncCore(function, arguments, cancellationToken);
        return FunctionResponse.From(result);
    }

    private async Task<InvocationResponse> CallAsyncCore(FunctionsLookup.TestFunctionDefinition function,
        IDictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var request = await CreateInvocationRequest(function, arguments);
        return await ExecuteInvocationAsync(request, cancellationToken);
    }

    private async Task<InvocationRequest> CreateInvocationRequest(FunctionsLookup.TestFunctionDefinition function,
        IDictionary<string, object> arguments)
    {
        var invocationRequest = new InvocationRequest
        {
            FunctionId = function.Id,
            InvocationId = Guid.NewGuid().ToString(),
            TraceContext = new RpcTraceContext()
        };

        foreach (var argument in arguments)
        {
            if (!function.InputBindings.TryGetValue(argument.Key, out var binding))
            {
                throw new ArgumentOutOfRangeException(
                    $"The function {function.Name} has no argument named {argument.Key}");
            }

            var data = await GetData(binding.Type, argument.Value);

            var parameterBinding = new Grpc.Messages.ParameterBinding { Name = argument.Key, Data = data };
            invocationRequest.InputData.Add(parameterBinding);
        }

        return invocationRequest;
    }

    private async Task<Grpc.Messages.TypedData> GetData(string bindingType, object argumentValue)
    {

        if (bindingType == "httpTrigger" && argumentValue is TestHttpRequest request)
        {
            var rpcHttp = new RpcHttp
            {
                Method = request.Method,
                Body = await request.Body.ToRpcAsync(_options.Serializer!),
                Url = request.Url.AbsoluteUri,
            };
            var result = new Grpc.Messages.TypedData { Http = rpcHttp };
            return result;
        }

        return await argumentValue.ToRpcAsync(_options.Serializer!);
    }

    public async Task<InvocationResponse> Test(string name, InvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var function = _functionsLookup.Lookup(name);
        request.FunctionId = function.Id;

        return await ExecuteInvocationAsync(request, cancellationToken);
    }

    private async Task<InvocationResponse> ExecuteInvocationAsync(InvocationRequest request,
        CancellationToken cancellationToken)
    {
        await _functionsLookup.WaitForFunctionsLoading();

        var message = new StreamingMessage { InvocationRequest = request };
        var result = new TaskCompletionSource<InvocationResponse>();
        InvocationResponded += (_, args) =>
        {
            if (args.Response.InvocationId == request.InvocationId)
                result.SetResult(args.Response);
        };

        await _serverStreamWriter!.WriteAsync(message, cancellationToken);

        return await result.Task;
    }

    /// <inheritdoc />
    public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream,
        IServerStreamWriter<StreamingMessage> serverStreamWriter, ServerCallContext context)
    {
        _serverStreamWriter = serverStreamWriter;
        await StartReaderAsync(requestStream);
    }

    private async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> requestStream)
    {
        while (await requestStream.MoveNext())
        {
            await ProcessMessageAsync(requestStream.Current);
        }
    }

    private async Task ProcessMessageAsync(StreamingMessage request)
    {
        switch (request.ContentCase)
        {
            case StreamingMessage.ContentOneofCase.None:
                break;
            case StreamingMessage.ContentOneofCase.StartStream:
                await WorkerInit(request);
                break;
            case StreamingMessage.ContentOneofCase.WorkerInitRequest:
                break;
            case StreamingMessage.ContentOneofCase.WorkerInitResponse:
                await LoadFunctions(request);
                break;
            case StreamingMessage.ContentOneofCase.WorkerHeartbeat:
                break;
            case StreamingMessage.ContentOneofCase.WorkerTerminate:
                break;
            case StreamingMessage.ContentOneofCase.WorkerStatusRequest:
                break;
            case StreamingMessage.ContentOneofCase.WorkerStatusResponse:
                break;
            case StreamingMessage.ContentOneofCase.FileChangeEventRequest:
                break;
            case StreamingMessage.ContentOneofCase.WorkerActionResponse:
                break;
            case StreamingMessage.ContentOneofCase.FunctionLoadRequest:
                break;
            case StreamingMessage.ContentOneofCase.FunctionLoadResponse:

                var functionId = request.FunctionLoadResponse.FunctionId;
                _functionsLookup.IsLoaded(functionId);

                break;
            case StreamingMessage.ContentOneofCase.InvocationRequest:
                break;
            case StreamingMessage.ContentOneofCase.InvocationResponse:
                InvocationResponded?.Invoke(this, new InvocationRespondedEventArgs(request.InvocationResponse));
                break;
            case StreamingMessage.ContentOneofCase.InvocationCancel:
                break;
            case StreamingMessage.ContentOneofCase.RpcLog:
                break;
            case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadRequest:
                request.FunctionEnvironmentReloadResponse.Result = Grpc.Messages.StatusResult.Success;
                break;
            case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadResponse:
                break;
            case StreamingMessage.ContentOneofCase.CloseSharedMemoryResourcesRequest:
                break;
            case StreamingMessage.ContentOneofCase.CloseSharedMemoryResourcesResponse:
                break;
            case StreamingMessage.ContentOneofCase.FunctionsMetadataRequest:
                request.FunctionMetadataResponse.UseDefaultMetadataIndexing = true;
                request.FunctionMetadataResponse.Result = Grpc.Messages.StatusResult.Success;
                break;
            case StreamingMessage.ContentOneofCase.FunctionMetadataResponse:

                break;
            case StreamingMessage.ContentOneofCase.FunctionLoadRequestCollection:
                break;
            case StreamingMessage.ContentOneofCase.FunctionLoadResponseCollection:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task LoadFunctions(StreamingMessage request)
    {
        foreach (var metadata in _functionsLookup.All)
        {
            request.FunctionLoadRequest = new FunctionLoadRequest
            {
                FunctionId = metadata.FunctionId,
                ManagedDependencyEnabled = true,
                Metadata = metadata
            };
            await _serverStreamWriter!.WriteAsync(request);
        }
    }

    private async Task WorkerInit(StreamingMessage request)
    {
        request.WorkerInitRequest = new WorkerInitRequest
        {
            FunctionAppDirectory = AppContext.BaseDirectory,
            HostVersion = "4.0.0.0",
            WorkerDirectory = AppContext.BaseDirectory
        };
        await _serverStreamWriter!.WriteAsync(request);
    }

}


