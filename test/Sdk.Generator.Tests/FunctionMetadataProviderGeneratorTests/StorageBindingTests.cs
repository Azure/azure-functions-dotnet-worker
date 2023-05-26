﻿// Copyright (c) .NET Foundation. All rights reserved.
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
        public class StorageBindingTests
        {
            private readonly Assembly[] _referencedExtensionAssemblies;

            public StorageBindingTests()
            {
                // load all extensions used in tests (match extensions tested on E2E app? Or include ALL extensions?)
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var storageExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.dll");
                var queueExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues.dll");
                var hostingExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.dll");
                var diExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.dll");
                var hostingAbExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.Abstractions.dll");
                var diAbExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.Abstractions.dll");
                var blobExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.dll");

                _referencedExtensionAssemblies = new[]
                {
                    abstractionsExtension,
                    blobExtension,
                    httpExtension,
                    storageExtension,
                    queueExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension
                };
            }

            [Fact]
            public async void TestQueueTriggerAndOutput()
            {
                string inputCode = """
                using System.Collections.Generic;
                using System.Linq;
                using System.Net;
                using System.Text.Json.Serialization;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class QueueTriggerAndOutput
                    {
                        [Function("QueueTriggerFunction")]
                        [QueueOutput("test-output-dotnet-isolated")]
                        public string QueueTriggerAndOutputFunction([QueueTrigger("test-input-dotnet-isolated")] string message, FunctionContext context)
                        {
                            return message;
                        }
                    }
                }
                """;

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = """
                // <auto-generated/>
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Text.Json;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace Microsoft.Azure.Functions.Worker
                {
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""Queue"",""direction"":""Out"",""queueName"":""test-output-dotnet-isolated""}");
                            Function0RawBindings.Add(@"{""name"":""message"",""type"":""QueueTrigger"",""direction"":""In"",""queueName"":""test-input-dotnet-isolated"",""dataType"":""String""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "QueueTriggerFunction",
                                EntryPoint = "FunctionApp.QueueTriggerAndOutput.QueueTriggerAndOutputFunction",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);

                            return Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    public static class WorkerHostBuilderFunctionMetadataProviderExtension
                    {
                        ///<summary>
                        /// Adds the GeneratedFunctionMetadataProvider to the service collection.
                        /// During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
                        ///</summary>
                        public static IHostBuilder ConfigureGeneratedFunctionMetadataProvider(this IHostBuilder builder)
                        {
                            builder.ConfigureServices(s => 
                            {
                                s.AddSingleton<IFunctionMetadataProvider, GeneratedFunctionMetadataProvider>();
                            });
                            return builder;
                        }
                    }
                }
                """;

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    _referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void TestBlobAndQueueInputsAndOutputs()
            {
                string inputCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Net;
                using System.Text.Json.Serialization;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class QueueTriggerAndOutput
                    {
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

                        [Function("BlobsToQueueFunction")]
                        [QueueOutput("queue2")]
                        public object BlobsToQueue(
                            [BlobInput("container2", IsBatched = true)] IEnumerable<string> blobs)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = """
                // <auto-generated/>
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Text.Json;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace Microsoft.Azure.Functions.Worker
                {
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""Blob"",""direction"":""Out"",""blobPath"":""container1/hello.txt"",""connection"":""MyOtherConnection""}");
                            Function0RawBindings.Add(@"{""name"":""queuePayload"",""type"":""QueueTrigger"",""direction"":""In"",""queueName"":""queueName"",""connection"":""MyConnection"",""dataType"":""String""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "QueueToBlobFunction",
                                EntryPoint = "FunctionApp.QueueTriggerAndOutput.QueueToBlob",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);
                            var Function1RawBindings = new List<string>();
                            Function1RawBindings.Add(@"{""name"":""$return"",""type"":""Queue"",""direction"":""Out"",""queueName"":""queue2""}");
                            Function1RawBindings.Add(@"{""name"":""blob"",""type"":""BlobTrigger"",""direction"":""In"",""path"":""container2/%file%"",""dataType"":""String""}");

                            var Function1 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "BlobToQueueFunction",
                                EntryPoint = "FunctionApp.QueueTriggerAndOutput.BlobToQueue",
                                RawBindings = Function1RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function1);
                            var Function2RawBindings = new List<string>();
                            Function2RawBindings.Add(@"{""name"":""$return"",""type"":""Queue"",""direction"":""Out"",""queueName"":""queue2""}");
                            Function2RawBindings.Add(@"{""name"":""blobs"",""type"":""Blob"",""direction"":""In"",""blobPath"":""container2"",""cardinality"":""Many"",""dataType"":""String""}");
                
                            var Function2 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "BlobsToQueueFunction",
                                EntryPoint = "FunctionApp.QueueTriggerAndOutput.BlobsToQueue",
                                RawBindings = Function2RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function2);

                            return Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    public static class WorkerHostBuilderFunctionMetadataProviderExtension
                    {
                        ///<summary>
                        /// Adds the GeneratedFunctionMetadataProvider to the service collection.
                        /// During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
                        ///</summary>
                        public static IHostBuilder ConfigureGeneratedFunctionMetadataProvider(this IHostBuilder builder)
                        {
                            builder.ConfigureServices(s => 
                            {
                                s.AddSingleton<IFunctionMetadataProvider, GeneratedFunctionMetadataProvider>();
                            });
                            return builder;
                        }
                    }
                }
                """;

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    _referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void TestInvalidBlobCardinalityMany()
            {
                string inputCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Net;
                using System.Text.Json.Serialization;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class BlobTest
                    {                
                        [Function("Function1")]
                        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
                            [BlobInput("input-container", Connection = "AzureWebJobsStorage", IsBatched = true)] string blobs)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;

                string? expectedGeneratedFileName = null;
                string? expectedOutput = null;

                var expectedDiagnosticResults = new List<DiagnosticResult>
                {
                    new DiagnosticResult(DiagnosticDescriptors.InvalidCardinality)
                    .WithSpan(15, 105, 15, 110)
                    // these arguments are the values we pass as the message format parameters when creating the DiagnosticDescriptor instance.
                    .WithArguments("blobs")
                };

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    _referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    expectedDiagnosticResults);
            }
        }
    }
}
