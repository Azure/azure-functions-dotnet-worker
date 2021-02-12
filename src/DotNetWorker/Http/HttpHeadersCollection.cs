// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Functions.Worker.Http
{
    public sealed class HttpHeadersCollection : HttpHeaders
    {
        public HttpHeadersCollection()
        {
        }

        public HttpHeadersCollection(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (var header in headers)
            {
                base.Add(header.Key, header.Value);
            }
        }

        public HttpHeadersCollection(IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var header in headers)
            {
                base.Add(header.Key, header.Value);
            }
        }
    }
}
