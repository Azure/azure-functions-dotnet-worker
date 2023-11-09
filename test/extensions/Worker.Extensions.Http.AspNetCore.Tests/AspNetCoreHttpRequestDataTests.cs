// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Tests;

namespace Worker.Extensions.Http.AspNetCore.Tests
{
    public class AspNetCoreHttpRequestDataTests
    {
        [Fact]
        public void RequestData_ExposesRequestProperties()
        {
            var uri = "http://localhost:808/test/123?query=test";
            var request = CreateRequest(uri, "POST", "Hello World");
            
            var testIdentity = new ClaimsIdentity();
            var testUser = new ClaimsPrincipal(testIdentity);
            request.HttpContext.User = testUser;

            var requestData = new AspNetCoreHttpRequestData(request, new TestFunctionContext());

            Assert.Equal(uri, requestData.Url.AbsoluteUri);
            Assert.Same(request.Body, requestData.Body);
            Assert.Same(testIdentity, requestData.Identities.Single());
            Assert.Equal(request.Method, requestData.Method);
        }

        [Fact]
        public void RequestData_ExposesRequestCookies()
        {
            var uri = "http://localhost:808/test/123?query=test";
            var request = CreateRequest(uri, "POST", "Hello World");
            request.Cookies = new CookiesCollection
            {
                { "cookie1", "value1" },
                { "cookie2", "value2" }
            };

            var requestData = new AspNetCoreHttpRequestData(request, new TestFunctionContext());

            Assert.Collection(requestData.Cookies,
                c => Assert.Equal("cookie1:value1", $"{c.Name}:{c.Value}"),
                c => Assert.Equal("cookie2:value2", $"{c.Name}:{c.Value}"));
        }

        private static HttpRequest CreateRequest(string uri, string method, string body)
        {
            var request = new DefaultHttpContext().Request;
            var uriBuilder = new UriBuilder(uri);

            request.Scheme = uriBuilder.Scheme;
            request.Host = new HostString(uriBuilder.Host, uriBuilder.Port);
            request.Path = uriBuilder.Path;
            request.Method = method;
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            request.QueryString = new QueryString(uriBuilder.Query);
            
            return request;
        }
    }
}
