using System.IO;
using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.TestWorker.Http;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestHttpResponseData : HttpResponseData
    {
        public TestHttpResponseData(FunctionContext context) : base(context)
        {
            StatusCode = HttpStatusCode.OK;
            Headers = new HttpHeadersCollection();
            Body = new MemoryStream();
            Cookies = new TestHttpCookies(Headers);
        }

        public override HttpStatusCode StatusCode { get; set; }

        public override HttpHeadersCollection Headers { get; set; }

        public override Stream Body { get; set; }

        public override HttpCookies Cookies { get; }
    }
}
