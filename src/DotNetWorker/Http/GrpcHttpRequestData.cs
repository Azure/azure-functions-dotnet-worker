// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcHttpRequestData : HttpRequestData
    {
        private RpcHttp httpData;

        public GrpcHttpRequestData(RpcHttp httpData)
        {
            this.httpData = httpData;
        }

        public override IImmutableDictionary<string, string> Headers => httpData.Headers.ToImmutableDictionary<string, string>();

        // TODO: Custom body type (BodyContent)
        public override string Body => httpData.Body.ToString();

        public override IImmutableDictionary<string, string> Params => httpData.Params.ToImmutableDictionary<string, string>();

        public override IImmutableDictionary<string, string> Query => httpData.Query.ToImmutableDictionary<string, string>();
    }
}
