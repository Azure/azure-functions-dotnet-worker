// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// A representation of an HTTP cookie
    /// </summary>
    public interface IHttpCookie
    {
        /// <summary>
        /// Specifies allowed hosts to receive the cookie.
        /// </summary>
        string? Domain { get; }

        /// <summary>
        /// Sets the cookie to expire at a specific date instead of when the client closes.
        /// NOTE: It is generally recommended that you use MaxAge over Expires.
        /// </summary>
        DateTimeOffset? Expires { get; }

        /// <summary>
        /// Sets the cookie to be inaccessible to JavaScript's Document.cookie API.
        /// </summary>
        bool? HttpOnly { get; }

        /// <summary>
        /// Number of seconds until the cookie expires. A zero or negative number will expire the cookie immediately.
        /// </summary>
        double? MaxAge { get; }

        /// <summary>
        /// Cookie name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Specifies URL path that must exist in the requested URL.
        /// </summary>
        string? Path { get; }

        /// <summary>
        /// Options to restrict the cookie to not be sent with cross-site requests.
        /// </summary>
        SameSite SameSite { get; }

        /// <summary>
        /// Sets the cookie to only be sent with an encrypted request.
        /// </summary>
        bool? Secure { get; }

        /// <summary>
        /// Cookie value.
        /// </summary>
        string Value { get; }
    }
}
