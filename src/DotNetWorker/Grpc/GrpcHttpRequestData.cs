// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcHttpRequestData : HttpRequestData
    {
        private readonly RpcHttp _httpData;
        private Uri? _url;
        private IEnumerable<ClaimsIdentity>? _identities;
        private HttpHeadersCollection? _headers;

        public GrpcHttpRequestData(RpcHttp httpData)
        {
            _httpData = httpData ?? throw new ArgumentNullException(nameof(httpData));
        }

        public override ReadOnlyMemory<byte>? Body
        {
            get
            {
                if (_httpData.Body is null)
                {
                    return null;
                }

                // Based on the advertised worker capabilities, the payload should always be binary data
                if (_httpData.Body.DataCase != TypedData.DataOneofCase.Bytes)
                {
                    throw new NotSupportedException($"{nameof(GrpcHttpRequestData)} expects binary data only. The provided data type was '{_httpData.Body.DataCase}'.");
                }

                return _httpData.Body.Bytes.Memory;
            }
        }

        public override HttpHeadersCollection Headers => _headers ??= new HttpHeadersCollection(_httpData.NullableHeaders.Select(h => new KeyValuePair<string, string>(h.Key, h.Value.Value)));

        public override IReadOnlyCollection<IHttpCookie> Cookies => _httpData.Cookies;

        public override Uri Url => _url ??= new Uri(_httpData.Url);

        public override IEnumerable<ClaimsIdentity> Identities
        {
            get
            {
                if (_identities is null)
                {
                    _identities = _httpData.Identities?.Select(id =>
                    {
                        var identity = new ClaimsIdentity(id.AuthenticationType.Value, id.NameClaimType.Value, id.RoleClaimType.Value);
                        identity.AddClaims(id.Claims.Select(c => new Claim(c.Type, c.Value)));

                        return identity;
                    }) ?? Enumerable.Empty<ClaimsIdentity>();

                }

                return _identities;
            }
        }

        public override string Method => _httpData.Method;
    }
}
