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
        /// Gets or sets the allowed hosts to receive the cookie.
        /// </summary>
        string? Domain { get; }

        /// <summary>
        /// Gets or sets experation date of the cookie. An experation date sets the 
        /// cookie to expire at a specific date instead of when the client closes.
        /// NOTE: It is generally recommended that you use MaxAge over Expires.
        /// </summary>
        DateTimeOffset? Expires { get; }

        /// <summary>
        /// Gets or sets the HttpOnly attributes on the cookie. A value of true will make
        /// the cookie inaccessible to JavaScript's Document.cookie API.
        /// </summary>
        bool? HttpOnly { get; }

        /// <summary>
        /// Gets or sets the number of seconds until the cookie expires. A zero or negative
        /// number will expire the cookie immediately.
        /// </summary>
        double? MaxAge { get; }

        /// <summary>
        /// Gets or sets the cookie name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the URL path that must exist in the requested URL.
        /// </summary>
        string? Path { get; }

        /// <summary>
        /// Gets or sets an option to restrict the cookie to not be sent with cross-site requests.
        /// </summary>
        SameSite SameSite { get; }

        /// <summary>
        /// Gets or sets the Secure attribute on the cookie. A value of true will ensure that the cookie
        /// is only sent with encrypted requests over HTTPS.
        /// </summary>
        bool? Secure { get; }

        /// <summary>
        /// Gets or sets the cookie value.
        /// </summary>
        string Value { get; }
    }
}
