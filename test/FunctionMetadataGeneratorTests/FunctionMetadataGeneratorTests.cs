using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Sdk;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Xunit;

namespace SdkTests
{
    public class FunctionMetadataGeneratorTests
    {
        private static Assembly _thisAssembly = typeof(FunctionMetadataGeneratorTests).Assembly;

        [Fact]
        public void BasicHttpFunction()
        {
            var generator = new FunctionMetadataGenerator();
            var typeDef = TestUtility.GetTypeDefinition(typeof(BasicHttp));
            var functions = generator.GenerateFunctionMetadata(typeDef);

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
            var typeDef = TestUtility.GetTypeDefinition(typeof(Storage));
            var functions = generator.GenerateFunctionMetadata(typeDef);

            Assert.Equal(2, functions.Count());

            var queueToBlob = functions.Single(p => p.Name == "QueueToBlobFunction");
            var blobToQueue = functions.Single(p => p.Name == "BlobToQueueFunction");

            ValidateFunction(queueToBlob, "QueueToBlobFunction", GetEntryPoint(nameof(Storage), nameof(Storage.QueueToBlob)),
                b => ValidateQueueTrigger(b),
                b => ValidateBlobOutput(b));

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
                    { "Connection", "MyOtherConnection" },
                    { "access", "ReadWrite" }
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
                    { "Name", "queueOutput" },
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
            var typeDef = TestUtility.GetTypeDefinition(typeof(Timer));
            var functions = generator.GenerateFunctionMetadata(typeDef);

            ValidateFunction(functions.Single(), "TimerFunction", GetEntryPoint(nameof(Timer), nameof(Timer.RunTimer)),
                b => ValidateTrigger(b));

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

        private static string GetEntryPoint(string className, string methodName) => $"{nameof(SdkTests)}.{nameof(FunctionMetadataGeneratorTests)}+{className}.{methodName}";

        private void ValidateFunction(SdkFunctionMetadata sdkFunctionMetadata, string name, string entryPoint, params Action<ExpandoObject>[] bindingValidations)
        {
            Assert.Equal(name, sdkFunctionMetadata.Name);
            Assert.Equal($"bin/{_thisAssembly.GetName().Name}.dll", sdkFunctionMetadata.ScriptFile);
            Assert.Equal("dotnet-isolated", sdkFunctionMetadata.Language);
            Assert.Equal(sdkFunctionMetadata.EntryPoint, entryPoint);
            Assert.Null(sdkFunctionMetadata.FunctionDirectory);

            Assert.Single(sdkFunctionMetadata.Properties, p => p.Key == "IsCodeless" && p.Value.ToString() == "False");

            Assert.Collection(sdkFunctionMetadata.Bindings, bindingValidations);
        }

        private static void AssertExpandoObject(ExpandoObject expando, IDictionary<string, object> expected)
        {
            var dict = (IDictionary<string, object>)expando;

            Assert.Equal(expected.Count, dict.Count);

            foreach (var kvp in expected)
            {
                Assert.Equal(kvp.Value, dict[kvp.Key]);
            }
        }

        private class BasicHttp
        {
            public const string FunctionName = "BasicHttpFunction";

            [FunctionName(FunctionName)]
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

            [FunctionName("QueueToBlobFunction")]
            public void QueueToBlob(
                [QueueTrigger("queueName", Connection = "MyConnection")] string queuePayload,
                [Blob("container1/hello.txt", FileAccess.ReadWrite, Connection = "MyOtherConnection")] OutputBinding<string> blobOutput)
            {
                throw new NotImplementedException();
            }

            [FunctionName("BlobToQueueFunction")]
            public void BlobToQueue(
                [BlobTrigger("container2/%file%")] string blob,
                [Queue("queue2")] OutputBinding<string> queueOutput)

            {
                throw new NotImplementedException();
            }
        }

        private class Timer
        {
            [FunctionName("TimerFunction")]
            public Task RunTimer([TimerTrigger("0 0 0 * * *", RunOnStartup = false)] object timer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
