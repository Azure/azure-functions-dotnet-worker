// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// IMPORTANT: Do not modify this file directly with major changes
// This file is a copy from this project (with minor updates) -- https://github.com/Azure/azure-functions-vs-build-sdk/blob/b0e54a832a92119e00a2b1796258fcf88e0d6109/src/Microsoft.NET.Sdk.Functions.MSBuild/Microsoft.NET.Sdk.Functions.MSBuild.csproj
// Please make any changes upstream first.

namespace Microsoft.NET.Sdk.Functions.Http
{
    internal static class HttpClientHelpers
    {
        public static async Task<IHttpResponse> PostWithBasicAuthAsync(this IHttpClient client, Uri uri, string username, string password, string contentType, string userAgent, Encoding encoding, Stream messageBody)
        {
            AddBasicAuthToClient(username, password, client);
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            StreamContent content = new StreamContent(messageBody ?? new MemoryStream())
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue(contentType)
                    {
                        CharSet = encoding.WebName
                    },
                    ContentEncoding =
                    {
                        encoding.WebName
                    }
                }
            };

            try
            {
                HttpResponseMessage responseMessage = await client.PostAsync(uri, content);
                return new HttpResponseMessageWrapper(responseMessage);
            }
            catch (TaskCanceledException)
            {
                return new HttpResponseMessageForStatusCode(HttpStatusCode.RequestTimeout);
            }
        }

        public static async Task<IHttpResponse> GetWithBasicAuthAsync(this IHttpClient client, Uri uri, string username, string password, string userAgent, CancellationToken cancellationToken)
        {
            AddBasicAuthToClient(username, password, client);
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            try
            {
                HttpResponseMessage responseMessage = await client.GetAsync(uri, cancellationToken);
                return new HttpResponseMessageWrapper(responseMessage);
            }
            catch (TaskCanceledException)
            {
                return new HttpResponseMessageForStatusCode(HttpStatusCode.RequestTimeout);
            }
        }

        private static void AddBasicAuthToClient(string username, string password, IHttpClient client)
        {
            client.DefaultRequestHeaders.Remove("Connection");

            string plainAuth = string.Format("{0}:{1}", username, password);
            byte[] plainAuthBytes = Encoding.ASCII.GetBytes(plainAuth);
            string base64 = Convert.ToBase64String(plainAuthBytes);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }
    }
}
