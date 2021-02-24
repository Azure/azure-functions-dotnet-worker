// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.OutputBindings;
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

            context.InvocationResult = result;

            Assert.Equal(0, context.OutputBindings.Count);

            OutputBindingsMiddleware.AddOutputBindings(context);
            object returnedVal = context.InvocationResult;

            Assert.Null(returnedVal);
            Assert.Equal(3, context.OutputBindings.Count);

            AssertDictionary(context.OutputBindings, new Dictionary<string, object>()
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

            context.InvocationResult = result;

            Assert.Empty(context.OutputBindings);

            OutputBindingsMiddleware.AddOutputBindings(context);
            object returnedVal = context.InvocationResult;

            Assert.Null(returnedVal);
            Assert.Equal(2, context.OutputBindings.Count);

            AssertDictionary(context.OutputBindings, new Dictionary<string, object>()
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

            context.InvocationResult = result;

            Assert.Empty(context.OutputBindings);

            OutputBindingsMiddleware.AddOutputBindings(context);
            object returnedVal = context.InvocationResult;

            Assert.Equal(returnedVal, result);
            Assert.Empty(context.OutputBindings);

            AssertDictionary(context.OutputBindings, new Dictionary<string, object>());
        }

        [Fact]
        public void SetsOutput_FromMethodBinding()
        {
            // special binding to indicate the return value is set as an output binding
            FunctionContext context = GetContextWithOutputBindings("$return");
            var emptyHttp = new TestHttpResponseData(context, HttpStatusCode.OK);

            context.InvocationResult = emptyHttp;

            Assert.Empty(context.OutputBindings);

            OutputBindingsMiddleware.AddOutputBindings(context);
            object returnedVal = context.InvocationResult;

            Assert.Equal(returnedVal, emptyHttp);
            Assert.Empty(context.OutputBindings);

            AssertDictionary(context.OutputBindings, new Dictionary<string, object>());
        }

        [Fact]
        public void SetsResult_NoBinding()
        {
            // No binding, should leave invocation result untouched
            FunctionContext context = GetContextWithOutputBindings();
            string myData = "abc";

            context.InvocationResult = myData;

            Assert.Empty(context.OutputBindings);

            OutputBindingsMiddleware.AddOutputBindings(context);
            object returnedVal = context.InvocationResult;

            Assert.Equal("abc", returnedVal);
            Assert.Equal(0, context.OutputBindings.Count);
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
                testOutputBindings[bindingName] = new TestBindingMetadata()
                {
                    Direction = BindingDirection.Out,
                    Type = $"SomeOutput{bindingName}"
                };
            }

            var metadata = new TestFunctionMetadata()
            {
                OutputBindings = testOutputBindings.ToImmutableDictionary()
            };
            var defintion = new TestFunctionDefinition()
            {
                Metadata = metadata,
                OutputBindingsInfo = new DefaultOutputBindingsInfoProvider().GetBindingsInfo(metadata)
            };
            var context = new TestFunctionContext()
            {
                FunctionDefinition = defintion
            };

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
}
