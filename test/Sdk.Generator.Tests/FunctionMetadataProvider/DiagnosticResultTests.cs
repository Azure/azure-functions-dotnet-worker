// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Sdk.Generator.Tests;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Functions.Sdk.Generator.FunctionMetadataProvider.Tests
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
                var blobExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.dll");
                var queueExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues.dll");
                var loggerExtension = typeof(NullLogger).Assembly;
                var hostingExtension = typeof(HostBuilder).Assembly;
                var diExtension = typeof(DefaultServiceProviderFactory).Assembly;
                var hostingAbExtension = typeof(IHost).Assembly;
                var diAbExtension = typeof(IServiceCollection).Assembly;

                referencedExtensionAssemblies = new[]
                {
                    abstractionsExtension,
                    httpExtension,
                    blobExtension,
                    queueExtension,
                    loggerExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension
                };
            }

            [Theory]
            [InlineData(LanguageVersion.CSharp7_3)]
            [InlineData(LanguageVersion.CSharp8)]
            [InlineData(LanguageVersion.CSharp9)]
            [InlineData(LanguageVersion.CSharp10)]
            [InlineData(LanguageVersion.CSharp11)]
            [InlineData(LanguageVersion.Latest)]
            public async Task MultipleOutputBindingsOnMethodFails(LanguageVersion languageVersion)
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
                // these arguments are the values we pass as the message format parameters when creating the DiagnosticDescriptor instance.
                .WithArguments("Method", "QueueToBlob")
            };

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    expectedDiagnosticResults,
                    languageVersion: languageVersion);
            }

            [Theory]
            [InlineData(LanguageVersion.CSharp7_3)]
            [InlineData(LanguageVersion.CSharp8)]
            [InlineData(LanguageVersion.CSharp9)]
            [InlineData(LanguageVersion.CSharp10)]
            [InlineData(LanguageVersion.CSharp11)]
            [InlineData(LanguageVersion.Latest)]
            public async Task MultipleOutputBindingsOnPropertyFails(LanguageVersion languageVersion)
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
                // these arguments are the values we pass as the message format parameters when creating the DiagnosticDescriptor instance.
                .WithArguments("Property", "Name")
            };

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    expectedDiagnosticResults,
                    languageVersion: languageVersion);
            }

            [Theory]
            [InlineData(LanguageVersion.CSharp7_3)]
            [InlineData(LanguageVersion.CSharp8)]
            [InlineData(LanguageVersion.CSharp9)]
            [InlineData(LanguageVersion.CSharp10)]
            [InlineData(LanguageVersion.CSharp11)]
            [InlineData(LanguageVersion.Latest)]
            public async Task MultipleHttpResponseBindingsFails(LanguageVersion languageVersion)
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
                    // these arguments are the values we pass as the message format parameters when creating the DiagnosticDescriptor instance.
                    .WithArguments("MultiReturnHttp")
                };

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    expectedDiagnosticResults,
                    languageVersion: languageVersion);
            }

            [Theory]
            [InlineData(LanguageVersion.CSharp7_3)]
            [InlineData(LanguageVersion.CSharp8)]
            [InlineData(LanguageVersion.CSharp9)]
            [InlineData(LanguageVersion.CSharp10)]
            [InlineData(LanguageVersion.CSharp11)]
            [InlineData(LanguageVersion.Latest)]
            public async Task InvalidRetryOptionsFailure(LanguageVersion languageVersion)
            {
                var inputCode = @"using System;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class HttpFunction
                    {
                        [Function(""HttpFunction"")]
                        [FixedDelayRetry(5, ""00:00:10"")]
                        public string Run([HttpTrigger(""get"")] string req)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }";

                string? expectedGeneratedFileName = null;
                string? expectedOutput = null;

                var expectedDiagnosticResults = new List<DiagnosticResult>
                {
                    new DiagnosticResult(DiagnosticDescriptors.InvalidRetryOptions)
                };

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    expectedDiagnosticResults,
                    languageVersion: languageVersion);
            }
        }
    }
}
