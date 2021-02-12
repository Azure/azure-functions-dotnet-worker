// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// A representation of the HTTP request sent by the host.
    /// </summary>
    public abstract class HttpRequestData
    {
        /// <summary>
        /// A representation of the HTTP request sent by the host.
        /// </summary>
        public abstract ReadOnlyMemory<byte>? Body { get; }

        /// <summary>
        /// Gets an <see cref="IImmutableDictionary{string, string}"/> containing the request headers.
        /// </summary>
        public abstract HttpHeadersCollection Headers { get; }

        /// <summary>
        /// Gets an <see cref="IReadOnlyCollection{IHttpCookie}"/> containing the request cookies.
        /// </summary>
        public abstract IReadOnlyCollection<IHttpCookie> Cookies { get; }

        /// <summary>
        /// Gets the <see cref="Uri"/> for this request.
        /// </summary>
        public abstract Uri Url { get; }

        /// <summary>
        /// Gets an <see cref="IEnumerable{ClaimsIdentity}"/> containing the request identities.
        /// </summary>
        public abstract IEnumerable<ClaimsIdentity> Identities { get; }

        /// <summary>
        /// Gets the HTTP method for this request.
        /// </summary>
        public abstract string Method { get; }
    }
}
