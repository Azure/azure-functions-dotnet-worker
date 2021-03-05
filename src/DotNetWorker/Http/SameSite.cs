// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// Options to restrict the cookie to not be sent with cross-site requests
    /// </summary>
    public enum SameSite
    {
        /// <summary>
        /// Default cookie-sending behavior option for cross-site requests. The current default behavior is "Lax".
        /// </summary>
        None = 0,

        /// <summary>
        /// Option to not send cookie on normal cross-site subrequests (example: loading images into a third party site),
        /// but to send when a user is navigating to the origin site (i.e. when following a link).
        /// </summary>
        Lax = 1,

        /// <summary>
        /// Option to send cookie in a first-party context and not send along with requests initiated by third party websites.
        /// </summary>
        Strict = 2,

        /// <summary>
        /// Option to send cookie in all contexts (i.e in responses to both first-party and cross-origin requests).
        /// If this property is set on a cookie, the cookie's Secure attribute must also be set (or the cookie will be blocked).
        /// </summary>
        ExplicitNone = 3,
    }
}
