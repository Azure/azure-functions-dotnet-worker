using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DependentAssemblyWithFunctions.NetStandard
{
    public sealed class NetStandardClassLibraryClass1
    {
        private readonly ILogger _logger;

        public NetStandardClassLibraryClass1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<NetStandardClassLibraryClass1>();
        }

        [Function("NetStandardClassLibraryClass1Function1")]
        public HttpResponseData Run1([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("NetStandardClassLibraryClass1Function1");
            throw new NotImplementedException();
        }

        [Function("NetStandardClassLibraryClass1Function2Async")]
        public Task<HttpResponseData> Run2([HttpTrigger(AuthorizationLevel.Admin, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("NetStandardClassLibraryClass1Function2Async");
            throw new NotImplementedException();
        }
    }
}
