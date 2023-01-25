using Azure.Storage.Blobs;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SampleIntegrationTests;

internal class FunctionRpcTestServer : FunctionRpc.FunctionRpcBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerOptions _options;
    private event EventHandler<InvocationRespondedEventArgs>? InvocationResponded;
    private class InvocationRespondedEventArgs : EventArgs
    {
        public InvocationResponse Response { get; }

        public InvocationRespondedEventArgs(InvocationResponse response)
        {
            Response = response;
        }
    }

    public FunctionRpcTestServer(IOptions<WorkerOptions> options, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }


    private IServerStreamWriter<StreamingMessage> _serverStreamWriter;
    private readonly FunctionsLookup _functionsLookup = new FunctionsLookup();

    public async Task Init() => await _functionsLookup.WaitForFunctionsLoading();

    /// <summary>Calls a function method by the function name.</summary>
    /// <param name="name">The name of the function to call.</param>
    /// <param name="arguments">The argument names and values to bind to parameters in the function. In addition to parameter values, these may also include binding data values. </param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> that will call the function.</returns>
    public Task CallAsync(string name, IDictionary<string, object> arguments,
        CancellationToken cancellationToken = default) => CallByNameAsync(name, arguments, cancellationToken);

    public Task<InvocationResponse> CallByNameAsync(string name, IDictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        var function = _functionsLookup.Lookup(name);

        return CallAsyncCore(function, arguments, cancellationToken);
    }

    private async Task<InvocationResponse> CallAsyncCore(FunctionsLookup.TestFunctionDefinition function, IDictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var request = await CreateInvocationRequest(function, arguments);
        return await ExecuteInvocationAsync(request, cancellationToken);
    }

    private async Task<InvocationRequest> CreateInvocationRequest(FunctionsLookup.TestFunctionDefinition function, IDictionary<string, object> arguments)
    {
        var invocationRequest = new InvocationRequest
        {
            FunctionId = function.Id,
            InvocationId = Guid.NewGuid().ToString(),

            TraceContext = new RpcTraceContext { },
        };

        foreach (var argument in arguments)
        {
            if (!function.InputBindings.TryGetValue(argument.Key, out var binding))
            {
                throw new ArgumentOutOfRangeException(
                    $"The function {function.Name} has no argument named {argument.Key}");
            }

            var data = await GetData(binding.Type, argument.Value);

            var parameterBinding = new ParameterBinding
            {
                Name = argument.Key,
                Data = data
            };
            invocationRequest.InputData.Add(parameterBinding);
        }
        return invocationRequest;
    }

    private async Task<TypedData> GetData(string bindingType, object argumentValue)
    {

        if (bindingType == "httpTrigger" && argumentValue is HttpRequest request)
        {
            var rpcHttp = new RpcHttp
            {
                Method = request.Method,
                Body = await request.Body.ToRpcAsync(_options.Serializer!),
                Url = request.GetDisplayUrl(),
            };
            var result = new TypedData
            {

                Http = rpcHttp
            };
            return result;
        }

        return await argumentValue.ToRpcAsync(_options.Serializer);
    }

    public async Task<InvocationResponse> Test(string name, InvocationRequest request, CancellationToken cancellationToken = default)
    {
        var function = _functionsLookup.Lookup(name);
        request.FunctionId = function.Id;

        return await ExecuteInvocationAsync(request, cancellationToken);
    }

    private async Task<InvocationResponse> ExecuteInvocationAsync(InvocationRequest request, CancellationToken cancellationToken)
    {
        await _functionsLookup.WaitForFunctionsLoading();


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
        await _serverStreamWriter.WriteAsync(message, cancellationToken);
        return await result.Task;
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

        foreach (var metadata in _functionsLookup.All)
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
        var function = _functionsLookup.Lookup(name);
        var requestParams = function.InputBindings.Single(a => a.Value.Type == "httpTrigger");
        var arguments = new Dictionary<string, object>
        {
            [requestParams.Key] = context.Request
        };
        foreach (var parameter in function.InputBindings.Values.Where(a => a.Type != "httpTrigger").Cast<FunctionsLookup.TestFunctionMetadata>())
        {
            arguments[parameter.Name] = await GetInput(parameter);
        }

        
        var response = await CallAsyncCore(function, arguments, context.RequestAborted);
        if(response.Result.Status == StatusResult.Types.Status.Success)
        {
            var http = response.ReturnValue?.Http ?? response.OutputData.Single(a => a.Data.Http is not null).Data.Http;
            context.Response.StatusCode = int.Parse(http.StatusCode);
            await context.Response.Body.WriteAsync(http.Body.Bytes.ToByteArray());
        }
        else
        {
            context.Response.StatusCode = 500;
            await context.Response.Body.WriteAsync(response.Result.Exception.ToByteArray());
        }
    }

    private async Task<object> GetInput(FunctionsLookup.TestFunctionMetadata parameter)
    {
        if (parameter.Type == "blob")
        {
            return await GetBlob(parameter);
        }

        throw new NotImplementedException($"{parameter.Type} is not supported");
    }

    private async Task<object> GetBlob(FunctionsLookup.TestFunctionMetadata parameterValue)
    {
        var path = parameterValue.RawBinding.TryGetProperty("blobPath", out var pathElement)
            ? pathElement.ToString()
            : throw new MissingMemberException("blobPath");
        var index = path.LastIndexOf('/');
        var container = path.Substring(0, index);
        var blobPath = path.Substring(index+1);

        var type = parameterValue.RawBinding.TryGetProperty("dataType", out var typeElement)
            ? typeElement.ToString()
            : throw new MissingMemberException("dataType");
        var blobServiceClient = _serviceProvider.GetRequiredService<BlobServiceClient>();
        var blob = await blobServiceClient.GetBlobContainerClient(container).GetBlobClient(blobPath).DownloadContentAsync();
        var binary = blob.Value.Content;
        if (type == "String")
        {
            return binary.ToString();
        }

        return "toto";

    }

    public IEnumerable<HttpRegistration> GetHttpRegistrations()
    {
        foreach (var function in _functionsLookup.All)
        {
            var definition = _functionsLookup.Lookup(function.Name);
            var httpTrigger = definition.InputBindings.Values.SingleOrDefault(a => a.Type == "httpTrigger");
            if (httpTrigger is FunctionsLookup.TestFunctionMetadata metadata)
            {
                if (!metadata.RawBinding.TryGetProperty("methods", out var element))

                    throw new ArgumentOutOfRangeException($"Missing methods property");
                var methods = element.EnumerateArray().ToArray().Select(i => i.GetString()!).ToArray();
                var route = metadata.RawBinding.TryGetProperty("route", out var routeValue) ? routeValue.ToString() : $"api/{function.Name}";
                yield return new HttpRegistration(methods, route!, function.Name);
            }
        }
    }
}

internal record HttpRegistration(IReadOnlyCollection<string> Methods, string Route, string FunctionName);
