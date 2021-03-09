// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// Provides functionality to help interact with HTTP cookies.
    /// </summary>
    public abstract class HttpCookies
    {
        /// <summary>
        /// Adds an HTTP cookie with the provided name and value.
        /// </summary>
        /// <param name="name">The cookie name</param>
        /// <param name="value">The cookie value.</param>
        public abstract void Append(string name, string value);

        /// <summary>
        /// Adds the provided <see cref="IHttpCookie"/>.
        /// To create a cookie, you can use the <see cref="CreateNew"/> method.
        /// </summary>
        /// <param name="cookie">The <see cref="IHttpCookie"/> to add.</param>
        public abstract void Append(IHttpCookie cookie);

        /// <summary>
        /// Creates an <see cref="IHttpCookie"/> instance for the current environment.
        /// </summary>
        /// <returns></returns>
        public abstract IHttpCookie CreateNew();
    }
}
