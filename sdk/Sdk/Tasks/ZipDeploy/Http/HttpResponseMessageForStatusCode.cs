// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

// IMPORTANT: Do not modify this file directly with major changes
// This file is a copy from this project (with minor updates) -- https://github.com/Azure/azure-functions-vs-build-sdk/blob/b0e54a832a92119e00a2b1796258fcf88e0d6109/src/Microsoft.NET.Sdk.Functions.MSBuild/Microsoft.NET.Sdk.Functions.MSBuild.csproj
// Please make any changes upstream first.

namespace Microsoft.NET.Sdk.Functions.Http
{
    internal class HttpResponseMessageForStatusCode : IHttpResponse
    {
        public HttpResponseMessageForStatusCode(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; private set; }

        public Task<Stream> GetResponseBodyAsync()
        {
            return Task.FromResult<Stream>(new MemoryStream());
        }

        public IEnumerable<string> GetHeader(string name)
        {
            return new string[0];
        }
    }
}
