// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal sealed class AspNetCoreHttpRequestData : HttpRequestData
    {
        private readonly HttpRequest _httpRequest;
        private readonly Uri _uri;
        private readonly Lazy<IReadOnlyCollection<IHttpCookie>> _cookies;
        private readonly Lazy<HttpHeadersCollection> _headers;

        public AspNetCoreHttpRequestData(HttpRequest request, FunctionContext context)
            : base(context)
        {
            _httpRequest = request ?? throw new ArgumentNullException(nameof(request));

            _uri = new Uri(_httpRequest.GetEncodedUrl());

            // Currently, this is a one way sync.
            // Further changes to the cookies collection will not be reflected in the request.
            // We can revisit this and inject a feature to enable sync in the future, if needed.
            _cookies = new(CreateCookiesCollection);
            _headers = new(() => new AspNetCoreHttpRequestHeadersCollection(_httpRequest));
        }

        public override Stream Body => _httpRequest.Body;

        public override HttpHeadersCollection Headers => _headers.Value;

        public override IReadOnlyCollection<IHttpCookie> Cookies => _cookies.Value;

        public override Uri Url => _uri;

        public override IEnumerable<ClaimsIdentity> Identities => _httpRequest.HttpContext.User.Identities;

        public override string Method => _httpRequest.Method;

        public override HttpResponseData CreateResponse()
        {
            return new AspNetCoreHttpResponseData(_httpRequest.HttpContext.Response, FunctionContext);
        }

        private IReadOnlyCollection<IHttpCookie> CreateCookiesCollection()
        {
            var cookies = new List<IHttpCookie>();
            foreach (var item in _httpRequest.Cookies)
            {
                var cookie = new HttpCookie(item.Key, item.Value);
                cookies.Add(cookie);
            }

            return cookies;
        }
    }
}
