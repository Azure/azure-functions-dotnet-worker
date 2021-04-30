// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

// IMPORTANT: Do not modify this file directly with major changes
// This file is a copy from this project (with minor updates) -- https://github.com/Azure/azure-functions-vs-build-sdk/blob/b0e54a832a92119e00a2b1796258fcf88e0d6109/src/Microsoft.NET.Sdk.Functions.MSBuild/Microsoft.NET.Sdk.Functions.MSBuild.csproj
// Please make any changes upstream first.

namespace Microsoft.NET.Sdk.Functions.Http
{
    internal class DefaultHttpClient : IHttpClient, IDisposable
    {
        private readonly HttpClient _httpClient = new HttpClient()
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        public HttpRequestHeaders DefaultRequestHeaders => _httpClient.DefaultRequestHeaders;

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public Task<HttpResponseMessage> PostAsync(Uri uri, StreamContent content)
        {
            return _httpClient.PostAsync(uri, content);
        }

        public Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken)
        {
            return _httpClient.GetAsync(uri, cancellationToken);
        }
    }
}
