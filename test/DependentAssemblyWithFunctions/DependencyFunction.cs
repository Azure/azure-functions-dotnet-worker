
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DependentAssemblyWithFunctions
{
    public class DependencyFunction
    {
        [Function("DependencyFunc")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            throw new NotImplementedException();
        }
    }
}
