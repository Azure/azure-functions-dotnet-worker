// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    
    internal class GrpcHttpResponseData : HttpResponseData
    {
        private readonly RpcHttp _rpcHttp = new RpcHttp();

        private Stream _body;
        private HttpHeadersCollection _headers;
        private bool _rpcHttpConsumed;

        public GrpcHttpResponseData(FunctionContext functionContext, HttpStatusCode statusCode)
            : this(functionContext, statusCode, new MemoryStream()) { }

        public GrpcHttpResponseData(FunctionContext functionContext, HttpStatusCode statusCode, Stream body)
            : base(functionContext)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _headers = new HttpHeadersCollection();
            StatusCode = statusCode;
            Cookies = new GrpcHttpCookies(_rpcHttp.Cookies);
        }
 
        public override HttpStatusCode StatusCode { get; set; }

        public override HttpHeadersCollection Headers
        {
            get => _headers;
            set => _headers = value ?? throw new InvalidOperationException($"{nameof(Headers)} property cannot be null");
        }

        public override Stream Body
        {
            get => _body;
            set => _body = value ?? throw new InvalidOperationException($"{nameof(Body)} property cannot be null");
        }

        public override HttpCookies Cookies { get; }

        internal async Task<RpcHttp> GetRpcHttpAsync()
        {
            if (_rpcHttpConsumed)
            {
                throw new InvalidOperationException("The current gRPC request has been consumed and cannot be retrieved again.");
            }

            _rpcHttpConsumed = true;

            foreach (var header in _headers)
            {
                _rpcHttp.Headers.Add(header.Key, string.Join(",", header.Value));
            }

            _rpcHttp.StatusCode = StatusCode.ToString("d");

            if (Body.CanRead)
            {
                if (Body.CanSeek)
                {
                    Body.Position = 0;
                }

                ByteString byteString = await ByteString.FromStreamAsync(Body);
                _rpcHttp.Body = new TypedData { Bytes = byteString };
            }

            return _rpcHttp;
        }
    }
}
