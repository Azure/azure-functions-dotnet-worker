// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;

namespace Microsoft.Azure.Functions.Worker
{
    public class HttpResponseData
    {
        public HttpResponseData(HttpStatusCode statusCode, string? body = null)
        {
            StatusCode = statusCode;
            Body = body;
            Headers = new Dictionary<string, string>();
        }

        public HttpStatusCode StatusCode { get; set; }

        // TODO: Custom body type (BodyContent)
        public string? Body { get; set; }

        public Dictionary<string, string> Headers { get; set; }
    }
}
