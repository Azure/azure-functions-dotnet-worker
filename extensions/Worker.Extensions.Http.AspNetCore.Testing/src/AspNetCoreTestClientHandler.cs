// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Testing;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Testing;

internal sealed class AspNetCoreTestClientHandler : DelegatingHandler
{
    private const int MaximumRedirects = 10;
    private readonly bool _allowAutoRedirect;
    private readonly bool _handleCookies;
    private readonly CookieContainer _cookies = new();

    internal AspNetCoreTestClientHandler(
        HttpMessageHandler innerHandler,
        FunctionsTestClientOptions options)
        : base(innerHandler)
    {
        _allowAutoRedirect = options.AllowAutoRedirect;
        _handleCookies = options.HandleCookies;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpRequestMessage current = request;
        bool ownsCurrent = false;
        try
        {
            for (int redirectCount = 0; ; redirectCount++)
            {
                AddCookies(current);
                HttpResponseMessage response = await base.SendAsync(current, cancellationToken);
                StoreCookies(current, response);
                if (!_allowAutoRedirect
                    || redirectCount == MaximumRedirects
                    || !IsRedirect(response.StatusCode)
                    || response.Headers.Location is null)
                {
                    if (ownsCurrent)
                    {
                        current.Dispose();
                    }

                    return response;
                }

                Uri nextUri = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(current.RequestUri!, response.Headers.Location);
                HttpRequestMessage next = await CreateRedirectRequestAsync(
                    current,
                    nextUri,
                    response.StatusCode,
                    cancellationToken);
                response.Dispose();
                if (ownsCurrent)
                {
                    current.Dispose();
                }

                current = next;
                ownsCurrent = true;
            }
        }
        catch
        {
            if (ownsCurrent)
            {
                current.Dispose();
            }

            throw;
        }
    }

    private void AddCookies(HttpRequestMessage request)
    {
        if (!_handleCookies || request.RequestUri is null)
        {
            return;
        }

        string header = _cookies.GetCookieHeader(request.RequestUri);
        if (header.Length > 0)
        {
            request.Headers.Remove("Cookie");
            request.Headers.TryAddWithoutValidation("Cookie", header);
        }
    }

    private void StoreCookies(HttpRequestMessage request, HttpResponseMessage response)
    {
        if (!_handleCookies
            || request.RequestUri is null
            || !response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return;
        }

        foreach (string value in values)
        {
            _cookies.SetCookies(request.RequestUri, value);
        }
    }

    private static async Task<HttpRequestMessage> CreateRedirectRequestAsync(
        HttpRequestMessage source,
        Uri uri,
        HttpStatusCode statusCode,
        CancellationToken cancellationToken)
    {
        bool switchToGet = statusCode == HttpStatusCode.SeeOther
            || ((statusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect)
                && source.Method != HttpMethod.Get
                && source.Method != HttpMethod.Head);
        var request = new HttpRequestMessage(switchToGet ? HttpMethod.Get : source.Method, uri)
        {
            Version = source.Version,
            VersionPolicy = source.VersionPolicy
        };
        foreach (var header in source.Headers.Where(header =>
                     !string.Equals(header.Key, "Cookie", StringComparison.OrdinalIgnoreCase)))
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (!switchToGet && source.Content is not null)
        {
            byte[] content = await source.Content.ReadAsByteArrayAsync(cancellationToken);
            request.Content = new ByteArrayContent(content);
            foreach (var header in source.Content.Headers)
            {
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return request;
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;
}
