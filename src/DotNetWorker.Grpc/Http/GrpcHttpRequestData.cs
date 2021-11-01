// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcHttpRequestData : HttpRequestData, IAsyncDisposable, IDisposable
    {
        private readonly RpcHttp _httpData;
        private Uri? _url;
        private IEnumerable<ClaimsIdentity>? _identities;
        private HttpHeadersCollection? _headers;
        private Stream? _bodyStream;
        private bool _disposed;
        private readonly Lazy<IReadOnlyCollection<IHttpCookie>> _cookies;

        public GrpcHttpRequestData(RpcHttp httpData, FunctionContext functionContext)
            : base(functionContext)
        {
            _httpData = httpData ?? throw new ArgumentNullException(nameof(httpData));
            _cookies = new Lazy<IReadOnlyCollection<IHttpCookie>>(() =>
            {
                if (Headers is null)
                {
                    return Array.Empty<IHttpCookie>();
                }

                var cookieString = Headers.FirstOrDefault(item => item.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)).Value;

                if (cookieString != null && cookieString.Any())
                {
                    return ToHttpCookies(cookieString.First());
                }

                return Array.Empty<IHttpCookie>();
            });
        }

        public override Stream Body
        {
            get
            {
                if (_bodyStream is null)
                {
                    if (_httpData.Body is null)
                    {
                        _bodyStream = Stream.Null;
                    }
                    else
                    {
                        // Based on the advertised worker capabilities, the payload should always be binary data
                        if (_httpData.Body.DataCase != TypedData.DataOneofCase.Bytes)
                        {
                            throw new NotSupportedException($"{nameof(GrpcHttpRequestData)} expects binary data only. The provided data type was '{_httpData.Body.DataCase}'.");
                        }

                        ReadOnlyMemory<byte> memory = _httpData.Body.Bytes.Memory;

                        if (memory.IsEmpty)
                        {
                            _bodyStream = Stream.Null;
                        }

                        var stream = new MemoryStream(memory.Length);
                        stream.Write(memory.Span);
                        stream.Position = 0;

                        _bodyStream = stream;
                    }
                }

                return _bodyStream;
            }
        }

        public override HttpHeadersCollection Headers => _headers ??= new HttpHeadersCollection(_httpData.NullableHeaders.Select(h => new KeyValuePair<string, string>(h.Key, h.Value.Value)));

        public override IReadOnlyCollection<IHttpCookie> Cookies
        {
            get
            {
                return _cookies.Value;
            }
        }

        public override Uri Url => _url ??= new Uri(_httpData.Url);

        public override IEnumerable<ClaimsIdentity> Identities
        {
            get
            {
                if (_identities is null)
                {
                    _identities = _httpData.Identities?.Select(id =>
                    {
                        var identity = new ClaimsIdentity(id.AuthenticationType?.Value, id.NameClaimType.Value, id.RoleClaimType.Value);
                        identity.AddClaims(id.Claims.Select(c => new Claim(c.Type, c.Value)));

                        return identity;
                    }) ?? Enumerable.Empty<ClaimsIdentity>();

                }

                return _identities;
            }
        }

        public override string Method => _httpData.Method;

        public override HttpResponseData CreateResponse()
        {
            return new GrpcHttpResponseData(FunctionContext, System.Net.HttpStatusCode.OK);
        }

        public ValueTask DisposeAsync()
        {
            return _bodyStream?.DisposeAsync() ?? ValueTask.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _bodyStream?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private IReadOnlyCollection<IHttpCookie> ToHttpCookies(string cookieString)
        {
            var separateCookies = cookieString.Split(";");

            List<IHttpCookie> httpCookiesList = new List<IHttpCookie>(separateCookies.Length);

            for (int c = 0; c < separateCookies.Length; c++)
            {
                var splitArray = separateCookies[c].Split("=", StringSplitOptions.RemoveEmptyEntries);
                var name = splitArray[0].Trim();
                var value = splitArray[1];
                httpCookiesList.Add(new HttpCookie(name, value));
            }

            return httpCookiesList;
        }

    }
}
