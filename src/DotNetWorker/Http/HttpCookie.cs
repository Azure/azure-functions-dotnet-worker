// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// Http response cookie object
    /// </summary>
    public sealed class HttpCookie : IHttpCookie
    {
        /// <summary>
        /// Creates a cookie with name and value.
        /// </summary>
        /// <param name="name">Cookie name</param>
        /// <param name="value">Cookie value</param>
        public HttpCookie(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Specifies allowed hosts to receive the cookie.
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// Sets the cookie to expire at a specific date instead of when the client closes.
        /// NOTE: It is generally recommended that you use MaxAge over Expires.
        /// </summary>
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Sets the cookie to be inaccessible to JavaScript's Document.cookie API.
        /// </summary>
        public bool? HttpOnly { get; set; }

        /// <summary>
        /// Number of seconds until the cookie expires. A zero or negative number will expire the cookie immediately.
        /// </summary>
        public double? MaxAge { get; set; }

        /// <summary>
        /// Cookie name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Specifies URL path that must exist in the requested URL.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Options to restrict the cookie to not be sent with cross-site requests.
        /// </summary>
        public SameSite SameSite { get; set; }

        /// <summary>
        /// Sets the cookie to only be sent with an encrypted request.
        /// </summary>
        public bool? Secure { get; set; }

        /// <summary>
        /// Cookie value.
        /// </summary>
        public string Value { get; set; }
    }
}
