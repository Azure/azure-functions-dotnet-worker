using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Http;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class HttpContextConverterTests
    {
        private readonly HttpContextConverter _converter = new();

        [Fact]
        public async Task ConversionSuccessfulForHttpContextAsync()
        {
            var httpContext = new DefaultHttpContext();
            var context = new TestConverterContext(typeof(HttpContext), null)
            {
                FunctionContext = { Items = new Dictionary<object, object>{ [Constants.HttpContextKey] = httpContext } }
            };

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            var convertedHttpContext = TestUtility.AssertIsTypeAndConvert<HttpContext>(conversionResult.Value);
            Assert.Same(httpContext, convertedHttpContext);
        }

        [Fact]
        public async Task ConversionSuccessfulForHttpRequestAsync()
        {
            var httpContext = new DefaultHttpContext();
            var context = new TestConverterContext(typeof(HttpRequest), null)
            {
                FunctionContext = { Items = new Dictionary<object, object>{ [Constants.HttpContextKey] = httpContext } }
            };

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            var convertedRequest = TestUtility.AssertIsTypeAndConvert<HttpRequest>(conversionResult.Value);
            Assert.Same(httpContext.Request, convertedRequest);
        }

        [Fact]
        public async Task ConversionSuccessfulForHttpRequestDataAsync()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "http",
                    Host = new HostString("localhost", 8080),
                    QueryString = QueryString.Create("key", "value"),
                }
            };
            var context = new TestConverterContext(typeof(HttpRequestData), null)
            {
                FunctionContext = { Items = new Dictionary<object, object>{ [Constants.HttpContextKey] = httpContext } }
            };

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            var convertedRequestData = TestUtility.AssertIsTypeAndConvert<HttpRequestData>(conversionResult.Value);
            Assert.Equal(new Uri("http://localhost:8080?key=value"), convertedRequestData.Url);
        }
    }
}
