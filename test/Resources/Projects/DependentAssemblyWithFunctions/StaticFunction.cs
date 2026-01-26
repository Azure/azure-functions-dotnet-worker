using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace DependentAssemblyWithFunctions
{
    public static class StaticFunction
    {
        [Function(nameof(StaticFunction))]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            throw new NotImplementedException();
        }
    }
}
