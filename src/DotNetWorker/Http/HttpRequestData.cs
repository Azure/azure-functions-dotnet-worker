using System.Collections.Immutable;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public class HttpRequestData
    {
        private RpcHttp httpData;

        public HttpRequestData(RpcHttp httpData)
        {
            this.httpData = httpData;
        }

        public IImmutableDictionary<string, string> Headers => httpData.Headers.ToImmutableDictionary<string, string>();
        public string Body => httpData.Body.ToString();
        public IImmutableDictionary<string, string> Params => httpData.Params.ToImmutableDictionary<string, string>();
        public IImmutableDictionary<string, string> Query => httpData.Query.ToImmutableDictionary<string, string>();
    }
}
