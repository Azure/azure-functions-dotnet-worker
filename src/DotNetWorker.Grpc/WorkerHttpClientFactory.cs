using System.Net.Http;

namespace Microsoft.Azure.Functions.Worker.Grpc;

internal class WorkerHttpClientFactory : IWorkerHttpClientFactory
{
    internal const string GrpcWorkerHttClientName = "grpc_worker_client";

#if  NET5_0_OR_GREATER
    private readonly IHttpClientFactory _httpClientFactory;
    
    public WorkerHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient CreateClient()
    {
        return _httpClientFactory.CreateClient(GrpcWorkerHttClientName);
    }

#else

    public HttpClient CreateClient()
    {
        return new HttpClient();
    }

#endif
    
}
