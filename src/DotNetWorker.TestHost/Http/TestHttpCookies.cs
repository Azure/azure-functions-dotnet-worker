using System;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Azure.Functions.Worker.TestWorker.Http
{
    internal class TestHttpCookies : HttpCookies
    {
        private readonly HttpHeadersCollection _headers;

        public TestHttpCookies(HttpHeadersCollection headers)
        {
            _headers = headers;
        }

        public override void Append(string name, string value)
        {
            Append(new HttpCookie(name, value));
        }

        public override void Append(IHttpCookie cookie)
        {
            var header = new SetCookieHeaderValue(cookie.Name, cookie.Value)
            {
                Domain = cookie.Domain,
                Expires = cookie.Expires,
                HttpOnly = cookie.HttpOnly ?? false,
                Path = cookie.Path,
                SameSite = Enum.Parse<SameSiteMode>(cookie.SameSite.ToString()),
                Secure = cookie.Secure ?? false,
            };

            if (cookie.MaxAge is not null)
            {
                header.MaxAge = TimeSpan.FromSeconds(cookie.MaxAge.Value);
            }

            _headers.Add("Set-Cookie", header.ToString());
        }

        public override IHttpCookie CreateNew()
        {
            return new HttpCookie(string.Empty, string.Empty);
        }
    }
}
