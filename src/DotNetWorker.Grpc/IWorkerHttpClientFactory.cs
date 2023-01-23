using System.Net.Http;

namespace Microsoft.Azure.Functions.Worker.Grpc;

internal interface IWorkerHttpClientFactory
{
    HttpClient CreateClient();
}
