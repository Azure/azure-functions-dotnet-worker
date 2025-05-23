﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Sdk.Generator.Tests;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Azure.Functions.Sdk.Generator.FunctionMetadataProvider.Tests
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
                var queueExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues.dll");
                var hostingExtension = typeof(HostBuilder).Assembly;
                var diExtension = typeof(DefaultServiceProviderFactory).Assembly;
                var hostingAbExtension = typeof(IHost).Assembly;
                var diAbExtension = typeof(IServiceCollection).Assembly;
                var blobExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.dll");

                _referencedExtensionAssemblies = new[]
                {
                    abstractionsExtension,
                    blobExtension,
                    httpExtension,
                    queueExtension,
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
            public async Task TestQueueTriggerAndOutput(LanguageVersion languageVersion)
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
                string expectedOutput = $$"""
                // <auto-generated/>
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Text.Json;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace TestProject
                {
                    /// <summary>
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker.
                    /// </summary>
                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                    {{Constants.GeneratedCodeAttribute}}
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        /// <inheritdoc/>
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""queue"",""direction"":""Out"",""queueName"":""test-output-dotnet-isolated""}");
                            Function0RawBindings.Add(@"{""name"":""message"",""type"":""queueTrigger"",""direction"":""In"",""queueName"":""test-input-dotnet-isolated"",""dataType"":""String""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "QueueTriggerFunction",
                                EntryPoint = "FunctionApp.QueueTriggerAndOutput.QueueTriggerAndOutputFunction",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);

                            return global::System.Threading.Tasks.Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    /// <summary>
                    /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                    /// </summary>
                    {{Constants.GeneratedCodeAttribute}}
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
                    expectedOutput,
                    languageVersion: languageVersion);
            }

            [Theory]
            [InlineData(LanguageVersion.CSharp7_3)]
            [InlineData(LanguageVersion.CSharp8)]
            [InlineData(LanguageVersion.CSharp9)]
            [InlineData(LanguageVersion.CSharp10)]
            [InlineData(LanguageVersion.CSharp11)]
            [InlineData(LanguageVersion.Latest)]
            public async Task TestBlobAndQueueInputsAndOutputs(LanguageVersion languageVersion)
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
                            [BlobTrigger("container2/%file%", Source = BlobTriggerSource.EventGrid)] string blob)
                        {
                            throw new NotImplementedException();
                        }

                        [Function("BlobsToQueueFunction")]
                        [QueueOutput("queue2")]
                        public object BlobsToQueue(
                            [BlobInput("container2")] IEnumerable<string> blobs)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = $$"""
                // <auto-generated/>
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Text.Json;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace TestProject
                {
                    /// <summary>
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker.
                    /// </summary>
                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                    {{Constants.GeneratedCodeAttribute}}
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        /// <inheritdoc/>
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""blob"",""direction"":""Out"",""blobPath"":""container1/hello.txt"",""connection"":""MyOtherConnection""}");
                            Function0RawBindings.Add(@"{""name"":""queuePayload"",""type"":""queueTrigger"",""direction"":""In"",""queueName"":""queueName"",""connection"":""MyConnection"",""dataType"":""String""}");

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
                            Function1RawBindings.Add(@"{""name"":""$return"",""type"":""queue"",""direction"":""Out"",""queueName"":""queue2""}");
                            Function1RawBindings.Add(@"{""name"":""blob"",""type"":""blobTrigger"",""direction"":""In"",""properties"":{""supportsDeferredBinding"":""True""},""path"":""container2/%file%"",""source"":""EventGrid"",""dataType"":""String""}");

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
                            Function2RawBindings.Add(@"{""name"":""$return"",""type"":""queue"",""direction"":""Out"",""queueName"":""queue2""}");
                            Function2RawBindings.Add(@"{""name"":""blobs"",""type"":""blob"",""direction"":""In"",""properties"":{""supportsDeferredBinding"":""True""},""blobPath"":""container2""}");

                            var Function2 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "BlobsToQueueFunction",
                                EntryPoint = "FunctionApp.QueueTriggerAndOutput.BlobsToQueue",
                                RawBindings = Function2RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function2);

                            return global::System.Threading.Tasks.Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    /// <summary>
                    /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                    /// </summary>
                    {{Constants.GeneratedCodeAttribute}}
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
                    expectedOutput,
                    languageVersion: languageVersion);
            }

            [Theory]
            [InlineData(LanguageVersion.CSharp7_3)]
            [InlineData(LanguageVersion.CSharp8)]
            [InlineData(LanguageVersion.CSharp9)]
            [InlineData(LanguageVersion.CSharp10)]
            [InlineData(LanguageVersion.CSharp11)]
            [InlineData(LanguageVersion.Latest)]
            public async Task TestQueueOutputWithHttpTrigger(LanguageVersion languageVersion)
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
                    public class HttpTriggerQueueOutput
                    {
                        [Function("HttpWithQueueOutput")]
                        [QueueOutput("myqueue", Connection = "Con")]
                        public string Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = $$"""
                // <auto-generated/>
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Text.Json;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace TestProject
                {
                    /// <summary>
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker.
                    /// </summary>
                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                    {{Constants.GeneratedCodeAttribute}}
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        /// <inheritdoc/>
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""queue"",""direction"":""Out"",""queueName"":""myqueue"",""connection"":""Con""}");
                            Function0RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Function"",""methods"":[""get"",""post""]}");
                
                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "HttpWithQueueOutput",
                                EntryPoint = "FunctionApp.HttpTriggerQueueOutput.Run",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);
                
                            return global::System.Threading.Tasks.Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    /// <summary>
                    /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                    /// </summary>
                    {{Constants.GeneratedCodeAttribute}}
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
                    expectedOutput,
                    languageVersion: languageVersion);
            }
        }
    }
}
