using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Sdk;

namespace SampleIntegrationTests;

internal class FunctionRpcTestServer : FunctionRpc.FunctionRpcBase
{
    private event EventHandler<InvocationRespondedEventArgs> InvocationResponded;
    private event EventHandler<FunctionLoadedEventArgs> FunctionLoaded;
    private class InvocationRespondedEventArgs : EventArgs
    {
        public InvocationResponse Response { get; }

        public InvocationRespondedEventArgs(InvocationResponse response)
        {
            Response = response;
        }
    }
    private class FunctionLoadedEventArgs : EventArgs
    {
        public string FunctionId { get; }

        public FunctionLoadedEventArgs(string functionId)
        {
            FunctionId = functionId;
        }
    }

    private static readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions();

    private IServerStreamWriter<StreamingMessage> _serverStreamWriter;
    private readonly IDictionary<string, (bool isLoaded, RpcFunctionMetadata metadata)> _functions;
    public FunctionRpcTestServer()
    {
        _functions = GetFunctionsMetadata().ToDictionary(a => a.FunctionId, a => (false, a));
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var namingPolicy = new FunctionsJsonNamingPolicy();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = true,
            IgnoreReadOnlyProperties = true,
            DictionaryKeyPolicy = namingPolicy,
            PropertyNamingPolicy = namingPolicy
        };

        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    public async Task<InvocationResponse> Test(string name, InvocationRequest request)
    {
        var function = _functions.First(a => a.Value.metadata.Name == name);
        request.FunctionId = function.Value.metadata.FunctionId;
            
        return await ExecuteInvocation(request);
    }

    private async Task<InvocationResponse> ExecuteInvocation(InvocationRequest request)
    {
        await WaitForFunctionsLoading();


        var message = new StreamingMessage
        {
            InvocationRequest = request
        };
        var result = new TaskCompletionSource<InvocationResponse>();
        InvocationResponded += (sender, args) =>
        {
            if (args.Response.InvocationId == request.InvocationId)
                result.SetResult(args.Response);
        };
        await _serverStreamWriter.WriteAsync(message);
        return await result.Task;
    }

    private Task WaitForFunctionsLoading()
    {
        if (_functions.All(a => a.Value.isLoaded))
            return Task.CompletedTask;
        var result = new TaskCompletionSource();
        FunctionLoaded += (_, args) =>
        {
            if(_functions.All(a => a.Value.isLoaded))
                result.SetResult();
        };
        return result.Task;
    }

    /// <inheritdoc />
    public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream, IServerStreamWriter<StreamingMessage> serverStreamWriter, ServerCallContext context)
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

    private IReadOnlyCollection<RpcFunctionMetadata> GetFunctionsMetadata()
    {
        var generator = new FunctionMetadataGenerator();
        var functions = generator.GenerateFunctionMetadata(Assembly.GetExecutingAssembly().Location, Array.Empty<string>());
        return functions.Select(Map).ToArray();
    }

    private static RpcFunctionMetadata Map(SdkFunctionMetadata metadata)
    {
        try
        {
            var rpc = new RpcFunctionMetadata
            {
                EntryPoint = metadata.EntryPoint,
                FunctionId = Guid.NewGuid().ToString(),
                Name = metadata.Name,
                ScriptFile = metadata.ScriptFile
            };
            foreach (var property in metadata.Properties)
            {
                rpc.Properties.Add(property.Key, property.Value.ToString());
            }


            foreach (var binding in metadata.Bindings)
            {
                rpc.RawBindings.Add(JsonSerializer.Serialize(binding, _serializerOptions));
            }

            return rpc;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
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
                if (_functions.TryGetValue(functionId, out var item))
                {
                    item.isLoaded = true;
                    _functions[functionId] = item;
                    FunctionLoaded.Invoke(this, new FunctionLoadedEventArgs(functionId));
                }
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
                request.FunctionEnvironmentReloadResponse.Result = StatusResult.Success;
                break;
            case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadResponse:
                break;
            case StreamingMessage.ContentOneofCase.CloseSharedMemoryResourcesRequest:
                break;
            case StreamingMessage.ContentOneofCase.CloseSharedMemoryResourcesResponse:
                break;
            case StreamingMessage.ContentOneofCase.FunctionsMetadataRequest:
                request.FunctionMetadataResponse.UseDefaultMetadataIndexing = true;
                request.FunctionMetadataResponse.Result = StatusResult.Success;
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

        //await _outputWriter.WriteAsync(request);

        //_outputWriter.TryComplete();
    }

    private async Task LoadFunctions(StreamingMessage request)
    {
        async Task LoadFunction(RpcFunctionMetadata metadata)
        {
            request.FunctionLoadRequest = new FunctionLoadRequest
            {
                FunctionId = metadata.FunctionId,
                ManagedDependencyEnabled = true,
                Metadata = metadata
            };
            await _serverStreamWriter.WriteAsync(request);
        }

        foreach (var (_, (_, metadata)) in _functions)
        {
            await LoadFunction(metadata);
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
        await _serverStreamWriter.WriteAsync(request);
    }

    public async Task HttpCall(string name, HttpContext context)
    {
        var function = _functions.Single(a => a.Value.metadata.Name == name).Value.metadata;
           
        var invocationRequest = new InvocationRequest
        {
            FunctionId = function.FunctionId,
            InvocationId = Guid.NewGuid().ToString(),
            InputData = { new ParameterBinding
            {
                Name = "req",
                Data = new TypedData
                {
                    Http = new RpcHttp
                    {
                        Method = context.Request.Method,
                        Url = context.Request.GetEncodedUrl(),
                            
                    }
                }
            } },
            TraceContext = new RpcTraceContext { },
        };
        var response = await ExecuteInvocation(invocationRequest);
        var http = response.ReturnValue.Http;
        context.Response.StatusCode = int.Parse(http.StatusCode);
        await context.Response.Body.WriteAsync(http.Body.Bytes.ToByteArray());
        //http.WriteTo(context.Response.Body);
    }

    private int GetHttpStatus(StatusResult.Types.Status resultStatus)
    {
        switch (resultStatus)
        {
            case StatusResult.Types.Status.Failure:
                return StatusCodes.Status500InternalServerError;
                break;
            case StatusResult.Types.Status.Success:
                return StatusCodes.Status200OK;
                break;
            case StatusResult.Types.Status.Cancelled:
            default:
                throw new ArgumentOutOfRangeException(nameof(resultStatus), resultStatus, null);
        }
    }
}
