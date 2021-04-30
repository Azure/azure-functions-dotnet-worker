// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

// IMPORTANT: Do not modify this file directly with major changes
// This file is a copy from this project (with minor updates) -- https://github.com/Azure/azure-functions-vs-build-sdk/blob/b0e54a832a92119e00a2b1796258fcf88e0d6109/src/Microsoft.NET.Sdk.Functions.MSBuild/Microsoft.NET.Sdk.Functions.MSBuild.csproj
// Please make any changes upstream first.

namespace Microsoft.NET.Sdk.Functions.Http
{ 
    public class HttpResponseMessageWrapper : IHttpResponse
    {
        private readonly HttpResponseMessage _message;
        private readonly Lazy<Task<Stream>> _responseBodyTask;

        public HttpResponseMessageWrapper(HttpResponseMessage message)
        {
            _message = message;
            StatusCode = message.StatusCode;
            _responseBodyTask = new Lazy<Task<Stream>>(GetResponseStream);
        }

        public HttpStatusCode StatusCode { get; private set; }

        public async Task<Stream> GetResponseBodyAsync()
        {
            return await _responseBodyTask.Value;
        }

        private Task<Stream> GetResponseStream()
        {
            return _message.Content.ReadAsStreamAsync();
        }

        public IEnumerable<string> GetHeader(string name)
        {
            IEnumerable<string> values;
            if (_message.Headers.TryGetValues(name, out values))
            {
                return values;
            }

            return new string[0];
        }
    }
}
