// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    [Collection(Constants.FunctionAppCollectionName)]
    public class HttpEndToEndTests
    {
        private readonly FunctionAppFixture _fixture;

        public HttpEndToEndTests(FunctionAppFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [InlineData("HelloFromQuery", "?name=Test", HttpStatusCode.OK, "Hello Test")]
        [InlineData("HelloFromQuery", "?name=John&lastName=Doe", HttpStatusCode.OK, "Hello John")]
        [InlineData("HelloFromQuery", "?emptyProperty=&name=Jane", HttpStatusCode.OK, "Hello Jane")]
        [InlineData("HelloFromQuery", "?name=John&name=Jane", HttpStatusCode.OK, "Hello John,Jane")]
        [InlineData("ExceptionFunction", "", HttpStatusCode.InternalServerError, "")]
        [InlineData("HelloFromQuery", "", HttpStatusCode.BadRequest, "")]
        public async Task HttpTriggerTests(string functionName, string queryString, HttpStatusCode expectedStatusCode, string expectedMessage)
        {
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionName, queryString);
            string actualMessage = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedStatusCode, response.StatusCode);

            if (!string.IsNullOrEmpty(expectedMessage))
            {
                Assert.False(string.IsNullOrEmpty(actualMessage));
                Assert.Contains(expectedMessage, actualMessage);
            }
        }

        [Theory]
        [InlineData("HelloFromJsonBody", "{\"Name\": \"Whitney\"}", "application/json", HttpStatusCode.OK, "Hello Whitney")]
        [InlineData("HelloFromJsonBody", "{\"Name\": \"麵🍜\"}", "application/json", HttpStatusCode.OK, "Hello 麵🍜")]
        [InlineData("HelloFromJsonBody", "{\"Name\": \"Bob\"}", "application/octet-stream", HttpStatusCode.OK, "Hello Bob")]
        public async Task HttpTriggerTestsMediaTypeDoNotMatter(string functionName, string body, string mediaType, HttpStatusCode expectedStatusCode, string expectedBody)
        {
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTriggerWithBody(functionName, body, mediaType);
            string responseBody = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedBody, responseBody);
        }

        [Theory]
        [InlineData("POCOAndHttpRequestWithQueryString", "?name=Test", "{ \"Title\": \"b\" }", "application/json", HttpStatusCode.OK, "Book b")]
        public async Task HttpTriggerTests_RequestBodyAndQueryString(string functionName, string queryString, string body, string mediaType, HttpStatusCode expectedStatusCode, string expectedBody)
        {
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTriggerWithBody(functionName, body, mediaType, queryString);
            string responseBody = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedBody, responseBody);
        }

        [Theory]
        [InlineData("POCOAndHttpRequest", "", "{ \"Title\": \"b\" }", "application/json", HttpStatusCode.OK, "Book b")]
        [InlineData("POCOAndHttpRequest", "", "{ \"Title\": \"b\" }", "application/octet-stream", HttpStatusCode.OK, "Book b")]
        [InlineData("VoidHttpTriggerWithPOCO", "", "{ \"Title\": \"b\" }", "application/json", HttpStatusCode.NoContent, "")]
        [InlineData("RequestDataAfterRouteParameters", "eu/books/", "{ \"Title\": \"b\" }", "application/json", HttpStatusCode.OK, "eu books b")]
        [InlineData("CreatingResponseFromDuplicateHttpRequestDataParameter", "", "body", "application/json", HttpStatusCode.InternalServerError, "")]
        public async Task HttpTriggerTests_RequestBody(string functionName, string route, string body, string mediaType, HttpStatusCode expectedStatusCode, string expectedBody)
        {
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTriggerWithBody($"{route}{functionName}", body, mediaType);
            string responseBody = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedBody, responseBody);
        }

        [Fact]
        public async Task HttpTriggerTestsPocoResult()
        {
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTriggerWithBody("HelloUsingPoco", string.Empty, "application/json");
            string responseBody = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"{{{Environment.NewLine}  \"Name\": \"Test\"{Environment.NewLine}}}", responseBody);
        }

        [Fact(Skip = "Proxies not currently supported in V4 but will be coming back.")]
        public async Task HttpProxy()
        {
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTriggerWithBody("proxytest", string.Empty, "application/json");
            string responseBody = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Proxy response", responseBody);
        }

        [Fact(Skip = "TODO: https://github.com/Azure/azure-functions-dotnet-worker/issues/133")]
        public async Task HttpTriggerWithCookieTests()
        {
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("HttpTriggerSetsCookie");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<string> cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value.ToList();
        }

        [Fact(Skip = "TODO: https://github.com/Azure/azure-functions-dotnet-worker/issues/134")]
        public Task HttpTriggerBindingDataTests()
        {
            return Task.CompletedTask;
        }
    }
}
