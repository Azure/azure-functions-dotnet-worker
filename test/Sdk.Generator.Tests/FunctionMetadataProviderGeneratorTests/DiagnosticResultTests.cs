// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public partial class FunctionMetadataProviderGeneratorTests
    {
        public class DiagnosticResultTests
        {
            private Assembly[] referencedExtensionAssemblies;

            public DiagnosticResultTests()
            {
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var storageExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.dll");
                var blobExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.dll");
                var queueExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues.dll");
                var loggerExtension = Assembly.LoadFrom("Microsoft.Extensions.Logging.Abstractions.dll");
                var hostingExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.dll");
                var diExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.dll");
                var hostingAbExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.Abstractions.dll");
                var diAbExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.Abstractions.dll");

                referencedExtensionAssemblies = new[]
                {
                    abstractionsExtension,
                    httpExtension,
                    storageExtension,
                    blobExtension,
                    queueExtension,
                    loggerExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension
                };
            }

            [Fact]
            public async void MultipleOutputBindingsOnMethodFails()
            {
                var inputCode = @"using System;
                using System.Net;
                using System.Collections;
                using System.Collections.Generic;
                using System.Linq;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class StorageInputs
                    {
                        [Function(""QueueToBlobFunction"")]
                        [BlobOutput(""container1/hello.txt"", Connection = ""MyOtherConnection"")]
                        [QueueOutput(""queue2"")]
                        public string QueueToBlob(
                            [QueueTrigger(""queueName"", Connection = ""MyConnection"")] string queuePayload)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }";

                string? expectedGeneratedFileName = null;
                string? expectedOutput = null;

                var expectedDiagnosticResults = new List<DiagnosticResult>
            {
                new DiagnosticResult(DiagnosticDescriptors.MultipleBindingsGroupedTogether)
                .WithSpan(17, 39, 17, 50)
                // these arguments are the values we pass as the message format parameters when creating the DiagnosticDescriptor instance.
                .WithArguments("Method", "QueueToBlob")
            };

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    expectedDiagnosticResults);
            }

            [Fact]
            public async void MultipleOutputBindingsOnPropertyFails()
            {
                var inputCode = @"using System.Net;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public static class HttpTriggerWithMultipleOutputBindings
                    {
                        [Function(nameof(HttpTriggerWithMultipleOutputBindings))]
                        public static MyOutputType Run([HttpTrigger(AuthorizationLevel.Anonymous, ""get"", ""post"", Route = null)] HttpRequestData req,
                            FunctionContext context)
                        {
                            var response = req.CreateResponse(HttpStatusCode.OK);
                            response.WriteString(""Success!"");

                            return new MyOutputType()
                            {
                                Name = ""some name"",
                                HttpResponse = response
                            };
                        }
                    }

                    public class MyOutputType
                    {
                        [BlobOutput(""container1/hello.txt"", Connection = ""MyOtherConnection"")]
                        [QueueOutput(""functionstesting2"", Connection = ""AzureWebJobsStorage"")]
                        public string Name { get; set; }

                        public HttpResponseData HttpResponse { get; set; }
                    }
                }";

                string? expectedGeneratedFileName = null;
                string? expectedOutput = null;

                var expectedDiagnosticResults = new List<DiagnosticResult>
            {
                new DiagnosticResult(DiagnosticDescriptors.MultipleBindingsGroupedTogether)
                .WithSpan(28, 39, 28, 43)
                // these arguments are the values we pass as the message format parameters when creating the DiagnosticDescriptor instance.
                .WithArguments("Property", "Name")
            };

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    expectedDiagnosticResults);
            }

            [Fact]
            public async void MultipleHttpResponseBindingsFails()
            {
                var inputCode = @"using System;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class MultiOutputReturnTypeHttp
                    {
                        [Function(""HttpAndBlob"")]
                        public MultiReturnHttp HttpAndBlobFunction(
                            [HttpTrigger(""get"")] string req)
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public class MultiReturnHttp
                    {
                        [BlobOutput(""container1/hello.txt"", Connection = ""MyOtherConnection"")]
                        public string Name { get; set; }

                        public HttpResponseData HttpResponse { get; set; }

                        public HttpResponseData HttpResult { get; set; }
                    }
                }";

                string? expectedGeneratedFileName = null;
                string? expectedOutput = null;

                var expectedDiagnosticResults = new List<DiagnosticResult>
                {
                    new DiagnosticResult(DiagnosticDescriptors.MultipleHttpResponseTypes)
                    .WithSpan(11, 32, 11, 47)
                    // these arguments are the values we pass as the message format parameters when creating the DiagnosticDescriptor instance.
                    .WithArguments("MultiReturnHttp")
                };

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    expectedDiagnosticResults);
            }
        }
    }
}
