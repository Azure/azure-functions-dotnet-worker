// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Storage;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Sdk;
using Mono.Cecil;
using Xunit;

namespace Microsoft.Azure.Functions.SdkTests
{
    public class FunctionMetadataGeneratorTests
    {
        private static Assembly _thisAssembly = typeof(FunctionMetadataGeneratorTests).Assembly;

        [Fact]
        public void BasicHttpFunction()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TestUtility.GetTypeDefinition(typeof(BasicHttp));
            var functions = generator.GenerateFunctionMetadata(module, typeDef);
            var extensions = generator.Extensions;

            AssertDictionary(extensions, new Dictionary<string, string>
            {
            });

            ValidateFunction(functions.Single(), BasicHttp.FunctionName, GetEntryPoint(nameof(BasicHttp), nameof(BasicHttp.Http)),
                b => ValidateTrigger(b),
                b => ValidateReturn(b));

            void ValidateTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "myReq" },
                    { "Type", "HttpTrigger" },
                    { "Direction", "In" },
                    { "authLevel", "Admin" },
                    { "methods", new[] { "get", "Post" } },
                    { "Route", "/api2" }
                });
            }

            void ValidateReturn(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "$return" },
                    { "Type", "http" },
                    { "Direction", "Out" }
                });
            }

            FunctionMetadataJsonWriter.WriteMetadata(functions, ".");
        }

        [Fact]
        public void StorageFunctions()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TestUtility.GetTypeDefinition(typeof(Storage));
            var functions = generator.GenerateFunctionMetadata(module, typeDef);
            var extensions = generator.Extensions;

            Assert.Equal(2, functions.Count());

            var queueToBlob = functions.Single(p => p.Name == "QueueToBlobFunction");
            var blobToQueue = functions.Single(p => p.Name == "BlobToQueueFunction");

            ValidateFunction(queueToBlob, "QueueToBlobFunction", GetEntryPoint(nameof(Storage), nameof(Storage.QueueToBlob)),
                b => ValidateQueueTrigger(b),
                b => ValidateBlobOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage", "4.0.4" }
            });

            void ValidateQueueTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "queuePayload" },
                    { "Type", "QueueTrigger" },
                    { "Direction", "In" },
                    { "Connection", "MyConnection" },
                    { "queueName", "queueName" }
                });
            }

            void ValidateBlobOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "$return" },
                    { "Type", "Blob" },
                    { "Direction", "Out" },
                    { "blobPath", "container1/hello.txt" },
                    { "Connection", "MyOtherConnection" }
                });
            }

            ValidateFunction(blobToQueue, "BlobToQueueFunction", GetEntryPoint(nameof(Storage), nameof(Storage.BlobToQueue)),
                b => ValidateBlobTrigger(b),
                b => ValidateQueueOutput(b));

            void ValidateBlobTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "blob" },
                    { "Type", "BlobTrigger" },
                    { "Direction", "In" },
                    { "blobPath", "container2/%file%" }
                });
            }

            void ValidateQueueOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "$return" },
                    { "Type", "Queue" },
                    { "Direction", "Out" },
                    { "queueName", "queue2" },
                });
            }
        }

        [Fact]
        public void TimerFunction()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TestUtility.GetTypeDefinition(typeof(Timer));
            var functions = generator.GenerateFunctionMetadata(module, typeDef);
            var extensions = generator.Extensions;

            ValidateFunction(functions.Single(), "TimerFunction", GetEntryPoint(nameof(Timer), nameof(Timer.RunTimer)),
                b => ValidateTrigger(b));

            AssertDictionary(extensions, new Dictionary<string, string>());

            void ValidateTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "timer" },
                    { "Type", "TimerTrigger" },
                    { "Direction", "In" },
                    { "schedule", "0 0 0 * * *" },
                    { "RunOnStartup", false }
                });
            }
        }

        [Fact]
        public void MultiOutput_OnReturnType()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TestUtility.GetTypeDefinition(typeof(MultiOutput_ReturnType));
            var functions = generator.GenerateFunctionMetadata(module, typeDef);
            var extensions = generator.Extensions;

            Assert.Single(functions);

            var queueToBlob = functions.Single(p => p.Name == "QueueToBlobFunction");

            ValidateFunction(queueToBlob, "QueueToBlobFunction", GetEntryPoint(nameof(MultiOutput_ReturnType), nameof(MultiOutput_ReturnType.QueueToBlob)),
                b => ValidateQueueTrigger(b),
                b => ValidateBlobOutput(b),
                b => ValidateQueueOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage", "4.0.4" }
            });

            void ValidateQueueTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "queuePayload" },
                    { "Type", "QueueTrigger" },
                    { "Direction", "In" },
                    { "Connection", "MyConnection" },
                    { "queueName", "queueName" }
                });
            }

            void ValidateBlobOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "blobOutput" },
                    { "Type", "Blob" },
                    { "Direction", "Out" },
                    { "blobPath", "container1/hello.txt" },
                    { "Connection", "MyOtherConnection" }
                });
            }

            void ValidateQueueOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "queueOutput" },
                    { "Type", "Queue" },
                    { "Direction", "Out" },
                    { "queueName", "queue2" },
                });
            }
        }

        [Fact]
        public void MultiOutput_OnReturnType_WithHttp()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TestUtility.GetTypeDefinition(typeof(MultiOutput_ReturnType_Http));
            var functions = generator.GenerateFunctionMetadata(module, typeDef);
            var extensions = generator.Extensions;

            Assert.Single(functions);

            var HttpAndQueue = functions.Single(p => p.Name == "HttpAndQueue");

            ValidateFunction(HttpAndQueue, "HttpAndQueue", GetEntryPoint(nameof(MultiOutput_ReturnType_Http), nameof(MultiOutput_ReturnType_Http.HttpAndQueue)),
                b => ValidateHttpTrigger(b),
                b => ValidateQueueOutput(b),
                b => ValidateHttpOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage", "4.0.4" }
            });

            void ValidateHttpTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "req" },
                    { "Type", "HttpTrigger" },
                    { "Direction", "In" },
                    { "methods", new[] { "get" } }
                });
            }

            void ValidateHttpOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "httpResponseProp" },
                    { "Type", "http" },
                    { "Direction", "Out" }
                });
            }

            void ValidateQueueOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "queueOutput" },
                    { "Type", "Queue" },
                    { "Direction", "Out" },
                    { "queueName", "queue2" },
                });
            }
        }

        [Fact]
        public void JustHttp_OnReturnTypeProperty()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TestUtility.GetTypeDefinition(typeof(ReturnType_JustHttp));
            var functions = generator.GenerateFunctionMetadata(module, typeDef);
            var extensions = generator.Extensions;

            Assert.Single(functions);

            var HttpAndQueue = functions.Single(p => p.Name == "JustHtt");

            ValidateFunction(HttpAndQueue, "JustHtt", GetEntryPoint(nameof(ReturnType_JustHttp), nameof(ReturnType_JustHttp.Justhtt)),
                b => ValidateHttpTrigger(b),
                b => ValidateHttpOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>());

            void ValidateHttpTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "req" },
                    { "Type", "HttpTrigger" },
                    { "Direction", "In" },
                    { "methods", new[] { "get" } }
                });
            }

            void ValidateHttpOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "httpResponseProp" },
                    { "Type", "http" },
                    { "Direction", "Out" }
                });
            }
        }

        [Fact]
        public void MultiOutput_OnMethod_Throws()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TestUtility.GetTypeDefinition(typeof(MultiOutput_Method));

            var exception = Assert.Throws<Exception>(() => generator.GenerateFunctionMetadata(module, typeDef));

            Assert.Contains($"Found multiple Output bindings on method", exception.Message);
        }

        private static string GetEntryPoint(string className, string methodName) => $"{typeof(FunctionMetadataGeneratorTests).FullName}+{className}.{methodName}";

        private void ValidateFunction(SdkFunctionMetadata sdkFunctionMetadata, string name, string entryPoint, params Action<ExpandoObject>[] bindingValidations)
        {
            Assert.Equal(name, sdkFunctionMetadata.Name);
            Assert.Equal($"{_thisAssembly.GetName().Name}.dll", sdkFunctionMetadata.ScriptFile);
            Assert.Equal("dotnet-isolated", sdkFunctionMetadata.Language);
            Assert.Equal(sdkFunctionMetadata.EntryPoint, entryPoint);
            Assert.Null(sdkFunctionMetadata.FunctionDirectory);

            Assert.Single(sdkFunctionMetadata.Properties, p => p.Key == "IsCodeless" && p.Value.ToString() == "False");

            Assert.Collection(sdkFunctionMetadata.Bindings, bindingValidations);
        }

        private static void AssertExpandoObject(ExpandoObject expando, IDictionary<string, object> expected)
        {
            var dict = (IDictionary<string, object>)expando;

            AssertDictionary(dict, expected);
        }

        private static void AssertDictionary<K,V>(IDictionary<K, V> dict, IDictionary<K, V> expected)
        {
            Assert.Equal(expected.Count, dict.Count);

            foreach (var kvp in expected)
            {
                Assert.Equal(kvp.Value, dict[kvp.Key]);
            }
        }

        private class BasicHttp
        {
            public const string FunctionName = "BasicHttpFunction";

            [Function(FunctionName)]
            public Task<HttpResponseData> Http([HttpTrigger(AuthorizationLevel.Admin, "get", "Post", Route = "/api2")] HttpRequestData myReq)
            {
                throw new NotImplementedException();
            }
        }

        private class Storage
        {
            public void AnotherMethod()
            {
            }

            [Function("QueueToBlobFunction")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public string QueueToBlob(
                [QueueTrigger("queueName", Connection = "MyConnection")] string queuePayload)
            {
                throw new NotImplementedException();
            }

            [Function("BlobToQueueFunction")]
            [QueueOutput("queue2")]
            public object BlobToQueue(
                [BlobTrigger("container2/%file%")] string blob)

            {
                throw new NotImplementedException();
            }
        }

        private class MultiOutput_Method
        {
            [Function("QueueToBlobFunction")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            [QueueOutput("queue2")]
            public string QueueToBlob(
                [QueueTrigger("queueName", Connection = "MyConnection")] string queuePayload)
            {
                throw new NotImplementedException();
            }
        }

        private class MultiOutput_ReturnType
        {
            [Function("QueueToBlobFunction")]
            public MultiReturn QueueToBlob(
                [QueueTrigger("queueName", Connection = "MyConnection")] string queuePayload)
            {
                throw new NotImplementedException();
            }
        }

        private class ReturnType_JustHttp
        {
            [Function("JustHtt")]
            public JustHttp Justhtt(
                [HttpTrigger("get")] string req)
            {
                throw new NotImplementedException();
            }
        }

        private class MultiOutput_ReturnType_Http
        {
            [Function("HttpAndQueue")]
            public MultiReturn_Http HttpAndQueue(
                [HttpTrigger("get")] string req)
            {
                throw new NotImplementedException();
            }
        }

        private class MultiReturn
        {
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public string blobOutput { get; set; }

            [QueueOutput("queue2")]
            public string queueOutput { get; set; }
        }

        private class MultiReturn_Http
        {
            [QueueOutput("queue2")]
            public string queueOutput { get; set; }

            public HttpResponseData httpResponseProp { get; set; }
        }

        private class JustHttp
        {
            public HttpResponseData httpResponseProp { get; set; }
        }

        private class Timer
        {
            [Function("TimerFunction")]
            public Task RunTimer([TimerTrigger("0 0 0 * * *", RunOnStartup = false)] object timer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
