using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;

namespace SampleIntegrationTests;

public class TestHttpRequest 
{
    public Stream Body { get; set; }

    public HttpHeadersCollection Headers { get; set; }

    public IReadOnlyCollection<IHttpCookie> Cookies { get; set; }

    public Uri Url { get; set; }

    public IEnumerable<ClaimsIdentity> Identities { get; }

    public string Method { get; }

    public TestHttpRequest(Uri url, string method = "GET", Stream body = default, HttpHeadersCollection headers = default,
        IReadOnlyCollection<IHttpCookie> cookies = default, IReadOnlyCollection<ClaimsIdentity> identities = default)
    {
        Body = body;
        Headers = headers;
        Cookies = cookies;
        Url = url;
        Identities = identities;
        Method = method;
    }
}
