// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal sealed class AspNetCoreResponseCookies : HttpCookies
    {
        private readonly HttpResponse _httpResponse;

        public AspNetCoreResponseCookies(HttpResponse httpResponse)
        {
            _httpResponse = httpResponse;
        }

        public override void Append(string name, string value)
        {
            _httpResponse.Cookies.Append(name, value);
        }

        public override void Append(IHttpCookie cookie)
        {
            if (cookie is null)
            {
                throw new ArgumentNullException(nameof(cookie));
            }

            var cookieOptions = new CookieOptions
            {
                Domain = cookie.Domain,
                Path = cookie.Path,
                Expires = cookie.Expires,
                HttpOnly = cookie.HttpOnly ?? false,
                MaxAge = cookie.MaxAge is null ? null : TimeSpan.FromSeconds(cookie.MaxAge.Value),
                SameSite = ConvertSameSite(cookie.SameSite),
                Secure = cookie.Secure ?? false
            };

            _httpResponse.Cookies.Append(cookie.Name, cookie.Value, cookieOptions);
        }

        private static SameSiteMode ConvertSameSite(SameSite sameSite)
        {
            return sameSite switch
            {
                SameSite.ExplicitNone or SameSite.None => SameSiteMode.None,
                SameSite.Lax => SameSiteMode.Lax,
                SameSite.Strict => SameSiteMode.Strict,
                _ => default,
            };
        }

        public override IHttpCookie CreateNew() => throw new NotSupportedException();
    }
}
