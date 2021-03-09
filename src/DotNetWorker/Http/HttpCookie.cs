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
        /// Gets or sets the allowed hosts to receive the cookie.
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// Gets or sets experation date of the cookie. An experation date sets the 
        /// cookie to expire at a specific date instead of when the client closes.
        /// NOTE: It is generally recommended that you use MaxAge over Expires.
        /// </summary>
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Gets or sets the HttpOnly attributes on the cookie. A value of true will make
        /// the cookie inaccessible to JavaScript's Document.cookie API.
        /// </summary>
        public bool? HttpOnly { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds until the cookie expires. A zero or negative
        /// number will expire the cookie immediately.
        /// </summary>
        public double? MaxAge { get; set; }

        /// <summary>
        /// Gets or sets the cookie name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URL path that must exist in the requested URL.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets an option to restrict the cookie to not be sent with cross-site requests.
        /// </summary>
        public SameSite SameSite { get; set; }

        /// <summary>
        /// Gets or sets the Secure attribute on the cookie. A value of true will ensure that the cookie
        /// is only sent with encrypted requests over HTTPS.
        /// </summary>
        public bool? Secure { get; set; }

        /// <summary>
        /// Gets or sets the cookie value.
        /// </summary>
        public string Value { get; set; }
    }
}
