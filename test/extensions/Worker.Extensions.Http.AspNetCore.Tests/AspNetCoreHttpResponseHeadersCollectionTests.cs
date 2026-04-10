// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Tests
{
    public class AspNetCoreHttpResponseHeadersCollectionTests
    {
        /// <summary>
        /// Verifies that the IHeaderDictionary indexer setter replaces values
        /// rather than appending, which is the root cause of cookie duplication
        /// reported in https://github.com/Azure/azure-functions-dotnet-worker/issues/3353.
        /// </summary>
        [Fact]
        public void IndexerSetter_ShouldReplaceValues_NotAppend()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var headers = new AspNetCoreHttpResponseHeadersCollection(httpContext.Response);
            IHeaderDictionary headerDict = headers;

            // Act - set a value, then set it again with a different value
            headerDict["X-Test"] = new StringValues("value1");
            headerDict["X-Test"] = new StringValues("value2");

            // Assert - should be replaced, not appended
            var result = headerDict["X-Test"];
            Assert.Equal(1, result.Count);
            Assert.Equal("value2", result[0]);
        }

        /// <summary>
        /// Reproduces the exact cookie duplication scenario from issue #3353.
        /// When Cookies.Append is called multiple times, ASP.NET Core's
        /// ResponseCookies reads existing Set-Cookie values and writes them
        /// back along with the new cookie. If the indexer appends instead
        /// of replacing, values are duplicated exponentially.
        /// </summary>
        [Fact]
        public void CookiesAppend_ShouldNotDuplicateSetCookieHeaders()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var response = httpContext.Response;

            // Create the headers collection wrapper (this replaces IHttpResponseFeature.Headers)
            _ = new AspNetCoreHttpResponseHeadersCollection(response);

            // Act - add a manual Set-Cookie header, then append cookies via ASP.NET Core
            response.Headers.Append("Set-Cookie", "ManualCookie=AlsoDuplicated");
            response.Cookies.Append("Cookie1", "one");
            response.Cookies.Append("Cookie2", "two");
            response.Cookies.Append("Cookie3", "three");

            // Assert - should have exactly 4 Set-Cookie values (1 manual + 3 cookies)
            var setCookieValues = response.Headers["Set-Cookie"];
            Assert.Equal(4, setCookieValues.Count);
        }

        /// <summary>
        /// Verifies that appending multiple cookies without a manual header
        /// still produces the correct number of Set-Cookie entries.
        /// </summary>
        [Fact]
        public void MultipleCookiesAppend_ShouldNotDuplicate()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var response = httpContext.Response;
            _ = new AspNetCoreHttpResponseHeadersCollection(response);

            // Act
            response.Cookies.Append("Cookie1", "one");
            response.Cookies.Append("Cookie2", "two");
            response.Cookies.Append("Cookie3", "three");

            // Assert - should have exactly 3 Set-Cookie values
            var setCookieValues = response.Headers["Set-Cookie"];
            Assert.Equal(3, setCookieValues.Count);
        }
    }
}
