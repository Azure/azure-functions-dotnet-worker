using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestHttpRequestData : HttpRequestData
    {
        public TestHttpRequestData(string method, Uri url, Stream? body, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
            IReadOnlyCollection<IHttpCookie> cookies, IEnumerable<ClaimsIdentity> identities, FunctionContext context) : base(context)
        {
            Method = method;
            Url = url;
            Body = body ?? Stream.Null;
            Headers = new HttpHeadersCollection(headers);
            Cookies = cookies;
            Identities = identities;
        }

        public override Stream Body { get; }

        public override HttpHeadersCollection Headers { get; }

        public override IReadOnlyCollection<IHttpCookie> Cookies { get; }

        public override Uri Url { get; }

        public override IEnumerable<ClaimsIdentity> Identities { get; }

        public override string Method { get; }

        public override HttpResponseData CreateResponse()
        {
            return new TestHttpResponseData(FunctionContext);
        }
    }
}
