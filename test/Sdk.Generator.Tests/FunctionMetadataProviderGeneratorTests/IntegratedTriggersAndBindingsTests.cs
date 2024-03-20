﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public partial class FunctionMetadataProviderGeneratorTests
    {
        public class IntegratedTriggersAndBindingsTests
        {
            private readonly Assembly[] _referencedExtensionAssemblies;

            public IntegratedTriggersAndBindingsTests()
            {
                // load all extensions used in tests (match extensions tested on E2E app? Or include ALL extensions?)
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var storageExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.dll");
                var timerExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Timer.dll");
                var blobExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.dll");
                var queueExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues.dll");
                var loggerExtension = typeof(NullLogger).Assembly;
                var hostingExtension = typeof(HostBuilder).Assembly;
                var diExtension = typeof(DefaultServiceProviderFactory).Assembly;
                var hostingAbExtension = typeof(IHost).Assembly;
                var diAbExtension = typeof(IServiceCollection).Assembly;
                var actionResult = typeof(IActionResult).Assembly;
                var aspnetHtpp = typeof(HttpContextAccessor).Assembly;
                var httpRequest = typeof(HttpRequest).Assembly;

                _referencedExtensionAssemblies = new[]
                {
                    abstractionsExtension,
                    httpExtension,
                    storageExtension,
                    timerExtension,
                    blobExtension,
                    queueExtension,
                    loggerExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension,
                    actionResult,
                    aspnetHtpp,
                    httpRequest
                };
            }

            [Fact]
            public async Task FunctionsWhereOutputBindingIsInTheReturnType()
            {
                // test generating function metadata for a simple HttpTrigger
                string inputCode = """
                using System;
                using System.Net;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public static class HttpTriggerWithMultipleOutputBindings
                    {
                        [Function(nameof(HttpTriggerWithMultipleOutputBindings))]
                        public static MyOutputType Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }

                        [Function("OutputTypeNoHttpProp")]
                        public static MyOutputTypeNoHttpProp Test([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public class MyOutputType
                    {
                        [QueueOutput("functionstesting2", Connection = "AzureWebJobsStorage")]
                        public string Name { get; set; }

                        public HttpResponseData HttpResponse { get; set; }
                    }

                    public class MyOutputTypeNoHttpProp
                    {
                        [QueueOutput("functionstesting2", Connection = "AzureWebJobsStorage")]
                        public string Name { get; set; }
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
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace TestProject
                {
                    /// <summary>
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker."/>
                    /// </summary>
                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        /// <inheritdoc/>
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function0RawBindings.Add(@"{""name"":""Name"",""type"":""queue"",""direction"":""Out"",""queueName"":""functionstesting2"",""connection"":""AzureWebJobsStorage""}");
                            Function0RawBindings.Add(@"{""name"":""HttpResponse"",""type"":""http"",""direction"":""Out""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "HttpTriggerWithMultipleOutputBindings",
                                EntryPoint = "FunctionApp.HttpTriggerWithMultipleOutputBindings.Run",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);
                            var Function1RawBindings = new List<string>();
                            Function1RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function1RawBindings.Add(@"{""name"":""Name"",""type"":""queue"",""direction"":""Out"",""queueName"":""functionstesting2"",""connection"":""AzureWebJobsStorage""}");
                
                            var Function1 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "OutputTypeNoHttpProp",
                                EntryPoint = "FunctionApp.HttpTriggerWithMultipleOutputBindings.Test",
                                RawBindings = Function1RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function1);

                            return Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    /// <summary>
                    /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                    /// </summary>
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

            [Theory]
            [InlineData(LanguageVersion.Latest)]
            public async void FunctionsMultipleOutputBindingWithActionResult(LanguageVersion languageVersion)
            {
                string inputCode = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                using System.Net;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;
                using Microsoft.AspNetCore.Http;
                using Microsoft.AspNetCore.Mvc;

                namespace FunctionApp
                {
                    public static class FunctionsMultipleOutputBindingWithActionResult
                    {
                        [Function(nameof(FunctionsMultipleOutputBindingWithActionResult))]
                        public static MyOutputType Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }

                        [Function("OutputTypeHttpHasTwoAttributes")]
                        public static MyOutputType2 Test([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public class MyOutputType
                    {
                        [QueueOutput("functionstesting2", Connection = "AzureWebJobsStorage")]
                        public string Name { get; set; }

                        [HttpResult]
                        public IActionResult HttpResponse { get; set; }
                    }

                    public class MyOutputType2
                    {
                        [QueueOutput("functionstesting2", Connection = "AzureWebJobsStorage")]
                        public string Name { get; set; }
                
                        [SuppressMessage("Microsoft.Naming", "Foo", Justification = "Bar")]
                        [HttpResult]
                        public IActionResult HttpResponse { get; set; }
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
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace TestProject
                {
                    /// <summary>
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker."/>
                    /// </summary>
                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        /// <inheritdoc/>
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function0RawBindings.Add(@"{""name"":""Name"",""type"":""queue"",""direction"":""Out"",""queueName"":""functionstesting2"",""connection"":""AzureWebJobsStorage""}");
                            Function0RawBindings.Add(@"{""name"":""HttpResponse"",""type"":""http"",""direction"":""Out""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "FunctionsMultipleOutputBindingWithActionResult",
                                EntryPoint = "FunctionApp.FunctionsMultipleOutputBindingWithActionResult.Run",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);
                            var Function1RawBindings = new List<string>();
                            Function1RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function1RawBindings.Add(@"{""name"":""Name"",""type"":""queue"",""direction"":""Out"",""queueName"":""functionstesting2"",""connection"":""AzureWebJobsStorage""}");
                            Function1RawBindings.Add(@"{""name"":""HttpResponse"",""type"":""http"",""direction"":""Out""}");
                
                            var Function1 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "OutputTypeHttpHasTwoAttributes",
                                EntryPoint = "FunctionApp.FunctionsMultipleOutputBindingWithActionResult.Test",
                                RawBindings = Function1RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function1);

                            return Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    /// <summary>
                    /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                    /// </summary>
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
                    expectedOutput, languageVersion: languageVersion);
            }

            [Fact]
            public async Task FunctionWithStringDataTypeInputBinding()
            {
                string inputCode = """
                using System.Net;
                using System.Text.Json;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public static class HttpTriggerWithBlobInput
                    {
                        [Function(nameof(HttpTriggerWithBlobInput))]
                        public static MyOutputType Run(
                            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
                            [BlobInput("test-samples/sample1.txt", Connection = "AzureWebJobsStorage")] string myBlob, FunctionContext context)
                        {
                            var bookVal = (Book)JsonSerializer.Deserialize(myBlob, typeof(Book));

                            var response = req.CreateResponse(HttpStatusCode.OK);

                            response.Headers.Add("Date", "Mon, 18 Jul 2016 16:06:00 GMT");
                            response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                            response.WriteString("Book Sent to Queue!");

                            return new MyOutputType()
                            {
                                Book = bookVal,
                                HttpResponse = response
                            };
                        }

                        public class MyOutputType
                        {
                            [QueueOutput("functionstesting2", Connection = "AzureWebJobsStorage")]
                            public Book Book { get; set; }

                            public HttpResponseData HttpResponse { get; set; }
                        }

                        public class Book
                        {
                            public string name { get; set; }
                            public string id { get; set; }
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
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace TestProject
                {
                    /// <summary>
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker."/>
                    /// </summary>
                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        /// <inheritdoc/>
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function0RawBindings.Add(@"{""name"":""myBlob"",""type"":""blob"",""direction"":""In"",""properties"":{""supportsDeferredBinding"":""True""},""blobPath"":""test-samples/sample1.txt"",""connection"":""AzureWebJobsStorage"",""dataType"":""String""}");
                            Function0RawBindings.Add(@"{""name"":""Book"",""type"":""queue"",""direction"":""Out"",""queueName"":""functionstesting2"",""connection"":""AzureWebJobsStorage""}");
                            Function0RawBindings.Add(@"{""name"":""HttpResponse"",""type"":""http"",""direction"":""Out""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "HttpTriggerWithBlobInput",
                                EntryPoint = "FunctionApp.HttpTriggerWithBlobInput.Run",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);

                            return Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    /// <summary>
                    /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                    /// </summary>
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
            public async Task FunctionWithNonFunctionsRelatedAttribute()
            {
                string inputCode = """
                using System;
                using System.Net;
                using System.Text.Json;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class HttpTriggerWithBlobInput
                    {
                        [Function("Products")]
                        public HttpResponseData Run(
                                       [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
                                       [FakeAttribute("hi")] string someString)
                        {
                            var response = req.CreateResponse(HttpStatusCode.OK);
                            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                            return response;
                        }
                    }

                    public class FakeAttribute : Attribute
                    {
                        public FakeAttribute(string name)
                        {
                            Name = name;
                        }

                        public string Name { get; }
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
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace TestProject
                {
                    /// <summary>
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker."/>
                    /// </summary>
                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        /// <inheritdoc/>
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get""]}");
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "Products",
                                EntryPoint = "FunctionApp.HttpTriggerWithBlobInput.Run",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);

                            return Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    /// <summary>
                    /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                    /// </summary>
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
            public async void FunctionWithTaskReturnType()
            {
                string inputCode = """
                using System;
                using System.Net;
                using System.Text.Json;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class Timer
                    {
                        [Function("TimerFunction")]
                        public Task RunTimer([TimerTrigger("0 0 0 * * *", RunOnStartup = false)] object timer)
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
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace TestProject
                {
                    /// <summary>
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker."/>
                    /// </summary>
                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        /// <inheritdoc/>
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""timer"",""type"":""timerTrigger"",""direction"":""In"",""schedule"":""0 0 0 * * *"",""runOnStartup"":false}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "TimerFunction",
                                EntryPoint = "FunctionApp.Timer.RunTimer",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);

                            return Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    /// <summary>
                    /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                    /// </summary>
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
            public async void FunctionWithGenericTaskReturnType()
            {
                string inputCode = """
                using System;
                using System.Net;
                using System.Text.Json;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class BasicHttp
                    {
                        [Function("FunctionName")]
                        public Task<HttpResponseData> Http([HttpTrigger(AuthorizationLevel.Admin, "get", "Post", Route = "/api2")] HttpRequestData myReq)
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
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace TestProject
                {
                    /// <summary>
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker."/>
                    /// </summary>
                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        /// <inheritdoc/>
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""myReq"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Admin"",""methods"":[""get"",""Post""],""route"":""/api2""}");
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "FunctionName",
                                EntryPoint = "FunctionApp.BasicHttp.Http",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);

                            return Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    /// <summary>
                    /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                    /// </summary>
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
            public async Task GenerateMultipleFunctionsMetadataTest()
            {
                string inputCode = """
                using System;
                using System.Threading.Tasks;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class HttpTriggerSimple
                    {
                        [Function(nameof(Run))]
                        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, FunctionContext executionContext)
                        {
                            throw new NotImplementedException();
                        }
                        [Function(nameof(RunAsync))]
                        public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
                        {
                            throw new NotImplementedException();
                        }
                        [Function(nameof(RunAsync2))]
                        public async Task<HttpResponseData> RunAsync2([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
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
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace TestProject
                {
                    /// <summary>
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker."/>
                    /// </summary>
                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
                    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
                    {
                        /// <inheritdoc/>
                        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                        {
                            var metadataList = new List<IFunctionMetadata>();
                            var Function0RawBindings = new List<string>();
                            Function0RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "Run",
                                EntryPoint = "FunctionApp.HttpTriggerSimple.Run",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);
                            var Function1RawBindings = new List<string>();
                            Function1RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function1RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                            var Function1 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "RunAsync",
                                EntryPoint = "FunctionApp.HttpTriggerSimple.RunAsync",
                                RawBindings = Function1RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function1);
                            var Function2RawBindings = new List<string>();
                            Function2RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function2RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                            var Function2 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "RunAsync2",
                                EntryPoint = "FunctionApp.HttpTriggerSimple.RunAsync2",
                                RawBindings = Function2RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function2);

                            return Task.FromResult(metadataList.ToImmutableArray());
                        }
                    }

                    /// <summary>
                    /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                    /// </summary>
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
        }
    }
}
