using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    public class HttpRequestDataBuilder : ITriggerMetadataSetup, IInputDataSetup
    {
        private readonly string _method;
        private readonly Uri _url;
        private readonly Dictionary<string, IEnumerable<string>> _headers = new();
        private readonly List<IHttpCookie> _cookies = new();
        private readonly List<ClaimsIdentity> _identities = new();

        private Stream? _body;

        private HttpRequestDataBuilder(HttpMethod method, Uri url)
        {
            _method = method.ToString();
            _url = url;
        }

        public static HttpRequestDataBuilder Create(HttpMethod method, Uri url) => new(method, url);

        public HttpRequestDataBuilder WithBody(string body)
        {
            _body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            return this;
        }

        public HttpRequestDataBuilder WithBody(Stream body)
        {
            _body = body;
            return this;
        }

        public HttpRequestDataBuilder AddHeader(string name, string value)
        {
            if (_headers.TryGetValue(name, out IEnumerable<string>? values))
            {
                ((List<string>)values).Add(value);
            }
            else
            {
                _headers.Add(name, new List<string> { value });
            }

            return this;
        }

        public HttpRequestDataBuilder AddIdentity(ClaimsIdentity identity)
        {
            _identities.Add(identity);
            return this;
        }

        public HttpRequestDataBuilder AddCookie(IHttpCookie cookie)
        {
            _cookies.Add(cookie);
            return this;
        }

        private HttpRequestData Build(FunctionContext context)
        {
            return new TestHttpRequestData(_method, _url, _body, _headers, _cookies, _identities, context);
        }

        public IDictionary<string, object?> SetupTriggerMetadata(FunctionContext context)
        {
        }

        public IDictionary<string, object?> SetupInputData(FunctionContext context)
        {
        }
    }
}
