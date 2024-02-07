// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class GrpcHttpRequestDataTests
    {
        Mock<FunctionContext> mockFunctionContext;

        public GrpcHttpRequestDataTests()
        {
            mockFunctionContext = new Mock<FunctionContext>();
        }

        [Fact]
        public void Headers_Property_is_Populated()
        {
            var headerDict = new Dictionary<string, string>
            {
                { "accept-encoding","gzip, deflate, br" },
                { "cookie","foo=bar; theme=dark" }
            };

            var httpRequest = CreateGrpcHttpRequestData(headerDict);

            Assert.Equal(2, httpRequest.Headers.Count());
            Assert.Equal("foo=bar; theme=dark", httpRequest.Headers.GetValues("cookie").First());
            Assert.True(httpRequest.Headers.Contains("accept-encoding"));
            Assert.False(httpRequest.Headers.Contains("cache-control"));
        }

        [Fact]
        public void Cookie_Property_is_Populated()
        {
            var headerDict = new Dictionary<string, string>
            {
                { "accept-encoding","gzip, deflate, br" },
                // cookie string with missing value, trailing '='
                { "cookie","foo=bar; theme=dark; tz=; token=a5a2Qo=" }
            };

            var cookies = CreateGrpcHttpRequestData(headerDict).Cookies;

            Assert.Equal(4, cookies.Count);
            Assert.Equal("bar", cookies.Single(c => c.Name == "foo").Value);
            Assert.Equal("dark", cookies.Single(c => c.Name == "theme").Value);
            Assert.Equal("a5a2Qo=", cookies.Single(c => c.Name == "token").Value);
            Assert.Equal(string.Empty, cookies.Single(c => c.Name == "tz").Value);
        }

        [Fact]
        public void Empty_Cookie_Header_Value_is_Handled()
        {
            var headerDict = new Dictionary<string, string>
            {
                { "accept-encoding","gzip, deflate, br" },
                { "cookie","" }
            };

            var request = CreateGrpcHttpRequestData(headerDict);

            Assert.Empty(request.Cookies);
            // Header should still be populated with raw value(empty string)
            Assert.Equal(2, request.Headers.Count());
            Assert.Equal("", request.Headers.GetValues("cookie").First());
        }

        private GrpcHttpRequestData CreateGrpcHttpRequestData(Dictionary<string, string> headerDict = null)
        {
            var rpcHttp = new RpcHttp
            {
                Url = "https://m.sn"
            };

            if (headerDict != null)
            {
                foreach (var header in headerDict)
                {
                    rpcHttp.NullableHeaders[header.Key] = new NullableString() { Value = header.Value };
                }
            }
            return new GrpcHttpRequestData(rpcHttp, mockFunctionContext.Object);
        }
    }
}
