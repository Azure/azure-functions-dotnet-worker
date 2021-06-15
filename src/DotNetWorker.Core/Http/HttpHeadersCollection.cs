// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// A collection of HTTP Headers
    /// </summary>
    public sealed class HttpHeadersCollection : HttpHeaders
    {
        /// <summary>
        /// Initializes an empty collection of HTTP Headers
        /// </summary>
        public HttpHeadersCollection()
        {
        }

        /// <summary>
        /// Initializes a collection of HTTP headers from an IEnumerable of key value pairs, 
        /// where each key (HTTP header name) can have multiple header values.
        /// </summary>
        /// <param name="headers">A collection of key value pairs representing HTTP headers names and values.</param>
        public HttpHeadersCollection(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (var header in headers)
            {
                base.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        /// <summary>
        /// Initializes a collection of HTTP headers from an IEnumerable of key value pairs.
        /// </summary>
        /// <param name="headers">A collection of key value pairs representing HTTP header names and values.</param>
        public HttpHeadersCollection(IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var header in headers)
            {
                base.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }
}
