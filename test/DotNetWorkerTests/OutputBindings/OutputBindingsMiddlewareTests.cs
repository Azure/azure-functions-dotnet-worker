// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Tests.Shared;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.OutputBindings
{
    public class OutputBindingsMiddlewareTests
    {
        [Fact]
        public void AddsStorageOutput_AndHttpReturn_FromReturnType()
        {
            FunctionContext context = GetContextWithOutputBindings(nameof(HttpAndStorage.MyQueueOutput), nameof(HttpAndStorage.MyBlobOutput),
                nameof(HttpAndStorage.MyHttpResponseData));
            var emptyHttp = new TestHttpResponseData(context, HttpStatusCode.OK);

            HttpAndStorage result = new HttpAndStorage()
            {
                MyQueueOutput = "queueStuff",
                MyBlobOutput = "blobStuff",
                MyRandomValue = "ShouldNotAppear",
                MyHttpResponseData = emptyHttp
            };

            context.GetBindings().InvocationResult = result;

            Assert.Empty(context.GetBindings().OutputBindingData);

            OutputBindingsMiddleware.AddOutputBindings(context);
            object returnedVal = context.GetBindings().InvocationResult;

            Assert.Null(returnedVal);
            Assert.Equal(3, context.GetBindings().OutputBindingData.Count);

            AssertDictionary(context.GetBindings().OutputBindingData, new Dictionary<string, object>()
            {
                { "MyQueueOutput", "queueStuff" },
                { "MyBlobOutput", "blobStuff" },
                { "MyHttpResponseData", emptyHttp }
            });
        }

        [Fact]
        public void AddsStorageOutput_FromReturnType()
        {
            FunctionContext context = GetContextWithOutputBindings(nameof(JustStorage.MyQueueOutput), nameof(JustStorage.MyBlobOutput));

            JustStorage result = new JustStorage()
            {
                MyQueueOutput = "queueStuff",
                MyBlobOutput = "blobStuff"
            };

            context.GetBindings().InvocationResult = result;

            Assert.Empty(context.GetBindings().OutputBindingData);

            OutputBindingsMiddleware.AddOutputBindings(context);
            object returnedVal = context.GetBindings().InvocationResult;

            Assert.Null(returnedVal);
            Assert.Equal(2, context.GetBindings().OutputBindingData.Count);

            AssertDictionary(context.GetBindings().OutputBindingData, new Dictionary<string, object>()
            {
                { "MyQueueOutput", "queueStuff" },
                { "MyBlobOutput", "blobStuff" }
            });
        }

        [Fact]
        public void AddsSingleOutput_FromMethodReturn()
        {
            // Special binding name indiciating return
            FunctionContext context = GetContextWithOutputBindings("$return");
            string result = "MyStorageData";

            context.GetBindings().InvocationResult = result;

            Assert.Empty(context.GetBindings().OutputBindingData);

            OutputBindingsMiddleware.AddOutputBindings(context);
            object returnedVal = context.GetBindings().InvocationResult;

            Assert.Equal(returnedVal, result);
            Assert.Empty(context.GetBindings().OutputBindingData);

            AssertDictionary(context.GetBindings().OutputBindingData, new Dictionary<string, object>());
        }

        [Fact]
        public void SetsOutput_FromMethodBinding()
        {
            // special binding to indicate the return value is set as an output binding
            FunctionContext context = GetContextWithOutputBindings("$return");
            var emptyHttp = new TestHttpResponseData(context, HttpStatusCode.OK);

            context.GetBindings().InvocationResult = emptyHttp;

            Assert.Empty(context.GetBindings().OutputBindingData);

            OutputBindingsMiddleware.AddOutputBindings(context);
            object returnedVal = context.GetBindings().InvocationResult;

            Assert.Equal(returnedVal, emptyHttp);
            Assert.Empty(context.GetBindings().OutputBindingData);

            AssertDictionary(context.GetBindings().OutputBindingData, new Dictionary<string, object>());
        }

        [Fact]
        public void SetsResult_NoBinding()
        {
            // No binding, should leave invocation result untouched
            FunctionContext context = GetContextWithOutputBindings();
            string myData = "abc";

            context.GetBindings().InvocationResult = myData;

            Assert.Empty(context.GetBindings().OutputBindingData);

            OutputBindingsMiddleware.AddOutputBindings(context);
            object returnedVal = context.GetBindings().InvocationResult;

            Assert.Equal("abc", returnedVal);
            Assert.Empty(context.GetBindings().OutputBindingData);
        }

        private static void AssertDictionary<K, V>(IDictionary<K, V> dict, IDictionary<K, V> expected)
        {
            Assert.Equal(expected.Count, dict.Count);

            foreach (var kvp in expected)
            {
                Assert.Equal(kvp.Value, dict[kvp.Key]);
            }
        }

        private FunctionContext GetContextWithOutputBindings(params string[] outputBindings)
        {
            var testOutputBindings = new Dictionary<string, BindingMetadata>();

            foreach (string bindingName in outputBindings)
            {
                testOutputBindings[bindingName] = new TestBindingMetadata("foo", $"SomeOutput{bindingName}", BindingDirection.Out);
            }

            var definition = new TestFunctionDefinition(outputBindings: testOutputBindings);
            var context = new TestFunctionContext(definition, null);

            return context;
        }

        public class JustStorage
        {
            public object MyQueueOutput { get; set; }

            public object MyBlobOutput { get; set; }
        }

        public class HttpAndStorage
        {
            public object MyQueueOutput { get; set; }

            public object MyBlobOutput { get; set; }

            public object MyRandomValue { get; set; }

            public HttpResponseData MyHttpResponseData { get; set; }
        }
    }

    public class TestHttpResponseData : HttpResponseData
    {
        public TestHttpResponseData(FunctionContext functionContext, HttpStatusCode status)
            : base(functionContext)
        {
            StatusCode = status;
        }

        public override HttpStatusCode StatusCode { get; set; }
        public override HttpHeadersCollection Headers { get; set; }
        public override Stream Body { get; set; }
        public override HttpCookies Cookies { get; }
    }

    public class TestHttpRequestData : HttpRequestData
    {
        public TestHttpRequestData(FunctionContext functionContext, Stream body = null, string method = "GET", string url = null)
            : base(functionContext)
        {
            Body = body ?? new MemoryStream();
            Headers = new HttpHeadersCollection();
            Cookies = new List<IHttpCookie>();
            Url = new Uri(url ?? "https://localhost");
            Identities = new List<ClaimsIdentity>();
            Method = method;
        }

        public override Stream Body { get; }

        public override HttpHeadersCollection Headers { get; }

        public override IReadOnlyCollection<IHttpCookie> Cookies { get; }

        public override Uri Url { get; }

        public override IEnumerable<ClaimsIdentity> Identities { get; }

        public override string Method { get; }

        public override HttpResponseData CreateResponse()
        {
            throw new NotImplementedException();
        }
    }
}
