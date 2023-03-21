// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Tests.OutputBindings;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class HttpResponseDataExtensionsTests
    {

        [Fact]
        public async Task WriteAsJsonAsync_SimpleOverload_AppliesDefaults()
        {
            FunctionContext context = CreateContext();
            var response = CreateResponse(context);

            var poco = new ResponsePoco
            {
                Name = "Test",
                SomeInt = 42
            };

            await HttpResponseDataExtensions.WriteAsJsonAsync(response, poco);

            string result = ReadResponseBody(response);

            Assert.Equal("application/json; charset=utf-8", response.Headers.GetValues("content-type").FirstOrDefault());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"textjsonname\":\"Test\",\"textjsonint\":42}", result);
        }

        [Fact]
        public async Task WriteAsJsonAsync_UsesRegisteredSerializer()
        {
            FunctionContext context = CreateContext(new NewtonsoftJsonObjectSerializer());
            var response = CreateResponse(context);

            var poco = new ResponsePoco
            {
                Name = "Test",
                SomeInt = 42
            };

            await HttpResponseDataExtensions.WriteAsJsonAsync(response, poco);

            string result = ReadResponseBody(response);

            Assert.Equal("application/json; charset=utf-8", response.Headers.GetValues("content-type").FirstOrDefault());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"jsonnetname\":\"Test\",\"jsonnetint\":42}", result);
        }

        [Fact]
        public async Task WriteAsJsonAsync_ContentTypeOverload_AppliesParameters()
        {
            FunctionContext context = CreateContext(new NewtonsoftJsonObjectSerializer());
            var response = CreateResponse(context);

            var poco = new ResponsePoco
            {
                Name = "Test",
                SomeInt = 42
            };

            await HttpResponseDataExtensions.WriteAsJsonAsync(response, poco, "application/json");

            string result = ReadResponseBody(response);

            Assert.Equal("application/json", response.Headers.GetValues("content-type").FirstOrDefault());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"jsonnetname\":\"Test\",\"jsonnetint\":42}", result);
        }

        [Fact]
        public async Task WriteAsJsonAsync_StatusCodeOverload_AppliesParameters()
        {
            FunctionContext context = CreateContext(new NewtonsoftJsonObjectSerializer());
            var response = CreateResponse(context);

            var poco = new ResponsePoco
            {
                Name = "Test",
                SomeInt = 42
            };

            await HttpResponseDataExtensions.WriteAsJsonAsync(response, poco, HttpStatusCode.BadRequest);

            string result = ReadResponseBody(response);
            
            Assert.Equal("application/json; charset=utf-8", response.Headers.GetValues("content-type").FirstOrDefault());
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("{\"jsonnetname\":\"Test\",\"jsonnetint\":42}", result);
        }

        [Fact]
        public async Task WriteAsJsonAsync_SerializerAndContentTypeOverload_AppliesParameters()
        {
            FunctionContext context = CreateContext();
            var response = CreateResponse(context);

            var poco = new ResponsePoco
            {
                Name = "Test",
                SomeInt = 42
            };

            await HttpResponseDataExtensions.WriteAsJsonAsync(response, poco, new NewtonsoftJsonObjectSerializer(), "application/json");

            string result = ReadResponseBody(response);

            Assert.Equal("application/json", response.Headers.GetValues("content-type").FirstOrDefault());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"jsonnetname\":\"Test\",\"jsonnetint\":42}", result);
        }

        private static TestHttpResponseData CreateResponse(FunctionContext context)
        {
            var response = new TestHttpResponseData(context, HttpStatusCode.Accepted);
            response.Body = new MemoryStream();
            response.Headers = new HttpHeadersCollection();
            return response;
        }

        private FunctionContext CreateContext(ObjectSerializer serializer = null)
        {
            var context = new TestFunctionContext();

            var services = new ServiceCollection();
            services.AddOptions();
            services.AddFunctionsWorkerCore();

            services.Configure<WorkerOptions>(c =>
            {
                c.Serializer = serializer;
            });

            context.InstanceServices = services.BuildServiceProvider();

            return context;
        }

        private string ReadResponseBody(HttpResponseData response)
        {
            if (response.Body is MemoryStream stream)
            {
                if (stream.Position != 0)
                {
                    stream.Position = 0;
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }

            return null;
        }

        public class ResponsePoco
        {
            [JsonProperty("jsonnetname")]
            [JsonPropertyName("textjsonname")]
            public string Name { get; set; }

            [JsonProperty("jsonnetint")]
            [JsonPropertyName("textjsonint")]
            public int SomeInt { get; set; }
        }
    }
}
