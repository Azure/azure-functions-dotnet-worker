// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcHttpCookies : HttpCookies
    {
        private RepeatedField<RpcHttpCookie> _cookies;

        public GrpcHttpCookies(RepeatedField<RpcHttpCookie> cookies)
        {
            _cookies = cookies;
        }

        public override void Append(string name, string value)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Append(new HttpCookie(name, value));
        }

        public override void Append(IHttpCookie cookie)
        {
            if (cookie is null)
            {
                throw new ArgumentNullException(nameof(cookie));
            }

            if (cookie is not RpcHttpCookie rpcCookie)
            {
                rpcCookie = new RpcHttpCookie(cookie);
            }

            _cookies.Add(rpcCookie);
        }

        public override IHttpCookie CreateNew() => new RpcHttpCookie();

        internal RepeatedField<RpcHttpCookie> GetCookies() => _cookies;
    }
}
