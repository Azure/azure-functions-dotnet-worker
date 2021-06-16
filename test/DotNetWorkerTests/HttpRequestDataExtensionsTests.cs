// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Net;
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
    public class HttpRequestDataExtensionsTests
    {

        [Fact]
        public async Task ReadAsJsonAsync_SimpleOverload_AppliesDefaults()
        {
            FunctionContext context = CreateContext();

            var body = "{\"textjsonname\":\"Test\",\"textjsonint\":42}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var request = new TestHttpRequestData(context, body: stream);

            RequestPoco result = await request.ReadFromJsonAsync<RequestPoco>();

            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.SomeInt);
        }

        [Fact]
        public async Task ReadAsJsonAsync_SerializerOverload_AppliesSerializer()
        {
            FunctionContext context = CreateContext();

            var body = "{\"jsonnetname\":\"Test\",\"jsonnetint\":42}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var request = new TestHttpRequestData(context, body: stream);

            RequestPoco result = await request.ReadFromJsonAsync<RequestPoco>(new NewtonsoftJsonObjectSerializer());

            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.SomeInt);
        }

        private FunctionContext CreateContext(ObjectSerializer serializer = null)
        {
            var context = new TestFunctionContext();

            var services = new ServiceCollection();
            services.AddOptions();
            services.AddFunctionsWorkerDefaults();

            if (serializer != null)
            {
                services.Configure<WorkerOptions>(c =>
                {
                    c.Serializer = serializer;
                });
            }

            context.InstanceServices = services.BuildServiceProvider();

            return context;
        }

        public class RequestPoco
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

