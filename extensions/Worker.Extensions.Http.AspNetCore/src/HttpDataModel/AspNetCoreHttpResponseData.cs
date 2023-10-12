// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal sealed class AspNetCoreHttpResponseData : HttpResponseData
    {
        private readonly HttpResponse _httpResponse;
        private readonly Lazy<AspNetCoreResponseCookies> _cookies;
        private Lazy<AspNetCoreHttpResponseHeadersCollection> _headers;

        public AspNetCoreHttpResponseData(HttpResponse httpResponse, FunctionContext context)
            : base(context)
        {
            _httpResponse = httpResponse;
            _cookies = new(() => new AspNetCoreResponseCookies(_httpResponse));
            _headers = new(() => new AspNetCoreHttpResponseHeadersCollection(_httpResponse));
        }

        public override HttpHeadersCollection Headers
        {
            get => _headers.Value;
            set => _headers = new(new AspNetCoreHttpResponseHeadersCollection(_httpResponse, value));
        }

        public override Stream Body
        {
            get => _httpResponse.Body;
            set => _httpResponse.Body = value;
        }
        public override HttpStatusCode StatusCode
        {
            get => (HttpStatusCode)_httpResponse.StatusCode;
            set => _httpResponse.StatusCode = (int)value;
        }

        public override HttpCookies Cookies => _cookies.Value;
    }
}
