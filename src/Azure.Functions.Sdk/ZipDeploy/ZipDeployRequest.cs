// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

namespace Azure.Functions.Sdk.ZipDeploy;

public record ZipDeployRequest(string UserName, string Password, Stream Content)
{
    private const string AzureADUserName = "00000000-0000-0000-0000-000000000000";
    private const string BearerAuthenticationScheme = "Bearer";
    private const string BasicAuthenticationScheme = "Basic";

    internal static readonly Uri PublishPath = new("api/publish?RemoteBuild=false", UriKind.Relative);
    internal static readonly Uri ZipDeployPath = new("api/zipdeploy?isAsync=true", UriKind.Relative);

    private static readonly MediaTypeHeaderValue ZipContentHeader = new(MediaTypeNames.Application.Zip)
    {
        CharSet = Encoding.UTF8.WebName
    };

    public bool UseBlobContainer { get; init; }

    public Uri GetUri(Uri? baseUri = null)
    {
        Uri path = UseBlobContainer ? PublishPath : ZipDeployPath;
        return baseUri is null
            ? path
            : new Uri(baseUri, path);
    }

    public HttpRequestMessage CreateRequestMessage(Uri? baseUri = null)
    {
        HttpRequestMessage request = new(HttpMethod.Post, GetUri(baseUri))
        {
            Content = GetContent(),
        };

        AddAuthenticationHeader(request);
        return request;
    }

    private StreamContent GetContent()
    {
        return new StreamContent(Content)
        {
            Headers =
            {
                ContentType = ZipContentHeader,
                ContentEncoding = { Encoding.UTF8.WebName },
            },
        };
    }

    private void AddAuthenticationHeader(HttpRequestMessage request)
    {
        if (!string.Equals(UserName, AzureADUserName, StringComparison.Ordinal))
        {
            string plainAuth = $"{UserName}:{Password}";
            byte[] plainAuthBytes = Encoding.ASCII.GetBytes(plainAuth);
            string base64 = Convert.ToBase64String(plainAuthBytes);
            request.Headers.Authorization = new AuthenticationHeaderValue(BasicAuthenticationScheme, base64);
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(BearerAuthenticationScheme, Password);
        }
    }
}
