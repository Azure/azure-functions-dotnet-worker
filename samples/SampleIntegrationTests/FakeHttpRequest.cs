using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace SampleIntegrationTests;

class FakeHttpRequest : HttpRequest
{
    public FakeHttpRequest(Uri url, string method = "GET", Stream body = default, IHeaderDictionary headers = default,
        IReadOnlyCollection<IHttpCookie> cookies = default, IReadOnlyCollection<ClaimsIdentity> identities = default)
    {
        Method = method;
        Body = body;
        Host = HostString.FromUriComponent(url);
        Path =  PathString.FromUriComponent(url);
        QueryString = QueryString.FromUriComponent(url);
            
        Headers = headers;
        // Cookies = cookies ?? Array.Empty<IHttpCookie>();
        // Identities = identities ?? Array.Empty<ClaimsIdentity>();
            
    }

    /// <inheritdoc />
    public override async Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override HttpContext HttpContext { get; }

    /// <inheritdoc />
    public override string Method { get; set; }

    /// <inheritdoc />
    public override string Scheme { get; set; }

    /// <inheritdoc />
    public override bool IsHttps { get; set; }

    /// <inheritdoc />
    public override HostString Host { get; set; }

    /// <inheritdoc />
    public override PathString PathBase { get; set; }

    /// <inheritdoc />
    public override PathString Path { get; set; }

    /// <inheritdoc />
    public override QueryString QueryString { get; set; }

    /// <inheritdoc />
    public override IQueryCollection Query { get; set; }

    /// <inheritdoc />
    public override string Protocol { get; set; }

    /// <inheritdoc />
    public override IHeaderDictionary Headers { get; }

    /// <inheritdoc />
    public override IRequestCookieCollection Cookies { get; set; }

    /// <inheritdoc />
    public override long? ContentLength { get; set; }

    /// <inheritdoc />
    public override string? ContentType { get; set; }

    /// <inheritdoc />
    public override Stream Body { get; set; }

    /// <inheritdoc />
    public override bool HasFormContentType { get; }

    /// <inheritdoc />
    public override IFormCollection Form { get; set; }
}
