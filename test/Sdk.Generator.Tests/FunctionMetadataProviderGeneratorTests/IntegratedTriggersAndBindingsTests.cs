﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public partial class FunctionMetadataProviderGeneratorTests
    {
        public class IntegratedTriggersAndBindingsTests
        {
            private Assembly[] referencedExtensionAssemblies;

            public IntegratedTriggersAndBindingsTests()
            {
                // load all extensions used in tests (match extensions tested on E2E app? Or include ALL extensions?)
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var storageExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.dll");
                var cosmosDBExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.CosmosDB.dll");
                var timerExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Timer.dll");
                var eventHubsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.EventHubs.dll");
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
                    cosmosDBExtension,
                    timerExtension,
                    eventHubsExtension,
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
            public async Task FunctionWhereOutputBindingIsInTheReturnType()
            {
                // test generating function metadata for a simple HttpTrigger
                string inputCode = @"
            using System;
            using System.Net;
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Azure.Functions.Worker.Http;

            namespace FunctionApp
            {
                public static class HttpTriggerWithMultipleOutputBindings
                {
                    [Function(nameof(HttpTriggerWithMultipleOutputBindings))]
                    public static MyOutputType Run([HttpTrigger(AuthorizationLevel.Anonymous, 'get', 'post', Route = null)] HttpRequestData req,
                        FunctionContext context)
                    {
                        throw new NotImplementedException();
                    }
                }

                public class MyOutputType
                {
                    [QueueOutput('functionstesting2', Connection = 'AzureWebJobsStorage')]
                    public string Name { get; set; }

                    public HttpResponseData HttpResponse { get; set; }
                }
            }".Replace("'", "\"");


                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Core;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Http;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            var Function0binding0 = new {
                Name = 'req',
                Type = 'HttpTrigger',
                Direction = 'In',
                AuthLevel = (AuthorizationLevel)0,
                Methods = new List<string> { 'get','post' },
            };
            var Function0binding0JSON = JsonSerializer.Serialize(Function0binding0, jsonOptions);
            Function0RawBindings.Add(Function0binding0JSON);
            var Function0binding1 = new {
                Name = 'Name',
                Type = 'Queue',
                Direction = 'Out',
                QueueName = 'functionstesting2',
                Connection = 'AzureWebJobsStorage',
            };
            var Function0binding1JSON = JsonSerializer.Serialize(Function0binding1, jsonOptions);
            Function0RawBindings.Add(Function0binding1JSON);
            var Function0binding2 = new {
                Name = 'HttpResponse',
                Type = 'http',
                Direction = 'Out',
            };
            var Function0binding2JSON = JsonSerializer.Serialize(Function0binding2, jsonOptions);
            Function0RawBindings.Add(Function0binding2JSON);
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'HttpTriggerWithMultipleOutputBindings',
                EntryPoint = 'TestProject.HttpTriggerWithMultipleOutputBindings.Run',
                RawBindings = Function0RawBindings,
                ScriptFile = 'TestProject.dll'
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
".Replace("'", "\"");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async Task FunctionWithStringDataTypeInputBinding()
            {
                string inputCode = @"
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
                        [HttpTrigger(AuthorizationLevel.Anonymous, 'get', 'post', Route = null)] HttpRequestData req,
                        [BlobInput('test-samples/sample1.txt', Connection = 'AzureWebJobsStorage')] string myBlob, FunctionContext context)
                    {
                        var bookVal = (Book)JsonSerializer.Deserialize(myBlob, typeof(Book));

                        var response = req.CreateResponse(HttpStatusCode.OK);

                        response.Headers.Add('Date', 'Mon, 18 Jul 2016 16:06:00 GMT');
                        response.Headers.Add('Content-Type', 'text/html; charset=utf-8');
                        response.WriteString('Book Sent to Queue!');

                        return new MyOutputType()
                        {
                            Book = bookVal,
                            HttpResponse = response
                        };
                    }

                    public class MyOutputType
                    {
                        [QueueOutput('functionstesting2', Connection = 'AzureWebJobsStorage')]
                        public Book Book { get; set; }

                        public HttpResponseData HttpResponse { get; set; }
                    }

                    public class Book
                    {
                        public string name { get; set; }
                        public string id { get; set; }
                    }
                }
            }".Replace("'", "\"");

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Core;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Http;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            var Function0binding0 = new {
                Name = 'req',
                Type = 'HttpTrigger',
                Direction = 'In',
                AuthLevel = (AuthorizationLevel)0,
                Methods = new List<string> { 'get','post' },
            };
            var Function0binding0JSON = JsonSerializer.Serialize(Function0binding0, jsonOptions);
            Function0RawBindings.Add(Function0binding0JSON);
            var Function0binding1 = new {
                Name = 'myBlob',
                Type = 'Blob',
                Direction = 'In',
                BlobPath = 'test-samples/sample1.txt',
                Connection = 'AzureWebJobsStorage',
                DataType = 'String',
            };
            var Function0binding1JSON = JsonSerializer.Serialize(Function0binding1, jsonOptions);
            Function0RawBindings.Add(Function0binding1JSON);
            var Function0binding2 = new {
                Name = 'Book',
                Type = 'Queue',
                Direction = 'Out',
                QueueName = 'functionstesting2',
                Connection = 'AzureWebJobsStorage',
            };
            var Function0binding2JSON = JsonSerializer.Serialize(Function0binding2, jsonOptions);
            Function0RawBindings.Add(Function0binding2JSON);
            var Function0binding3 = new {
                Name = 'HttpResponse',
                Type = 'http',
                Direction = 'Out',
            };
            var Function0binding3JSON = JsonSerializer.Serialize(Function0binding3, jsonOptions);
            Function0RawBindings.Add(Function0binding3JSON);
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'HttpTriggerWithBlobInput',
                EntryPoint = 'TestProject.HttpTriggerWithBlobInput.Run',
                RawBindings = Function0RawBindings,
                ScriptFile = 'TestProject.dll'
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
".Replace("'", "\"");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async Task FunctionWithNonFunctionsRelatedAttribute()
            {
                string inputCode = @"
            using System;
            using System.Net;
            using System.Text.Json;
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Azure.Functions.Worker.Http;

            namespace FunctionApp
            {
                public class HttpTriggerWithBlobInput
                {
                    [Function('Products')]
                    public HttpResponseData Run(
                                   [HttpTrigger(AuthorizationLevel.Anonymous, 'get', Route = null)] HttpRequestData req,
                                   [FakeAttribute('hi')] string someString)
                    {
                        var response = req.CreateResponse(HttpStatusCode.OK);
                        response.Headers.Add('Content-Type', 'text/plain; charset=utf-8');
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
            }".Replace("'", "\"");

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Core;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Http;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            var Function0binding0 = new {
                Name = 'req',
                Type = 'HttpTrigger',
                Direction = 'In',
                AuthLevel = (AuthorizationLevel)0,
                Methods = new List<string> { 'get' },
            };
            var Function0binding0JSON = JsonSerializer.Serialize(Function0binding0, jsonOptions);
            Function0RawBindings.Add(Function0binding0JSON);
            var Function0binding1 = new {
                Name = '$return',
                Type = 'http',
                Direction = 'Out',
            };
            var Function0binding1JSON = JsonSerializer.Serialize(Function0binding1, jsonOptions);
            Function0RawBindings.Add(Function0binding1JSON);
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'Products',
                EntryPoint = 'TestProject.HttpTriggerWithBlobInput.Run',
                RawBindings = Function0RawBindings,
                ScriptFile = 'TestProject.dll'
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
".Replace("'", "\"");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void FunctionWithTaskReturnType()
            {
                string inputCode = @"
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
                        [Function('TimerFunction')]
                        public Task RunTimer([TimerTrigger('0 0 0 * * *', RunOnStartup = false)] object timer)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                ".Replace("'", "\"");

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Core;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            var Function0binding0 = new {
                Name = 'timer',
                Type = 'TimerTrigger',
                Direction = 'In',
                Schedule = '0 0 0 * * *',
                RunOnStartup = 'False',
            };
            var Function0binding0JSON = JsonSerializer.Serialize(Function0binding0, jsonOptions);
            Function0RawBindings.Add(Function0binding0JSON);
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'TimerFunction',
                EntryPoint = 'TestProject.Timer.RunTimer',
                RawBindings = Function0RawBindings,
                ScriptFile = 'TestProject.dll'
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
".Replace("'", "\"");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void FunctionWithGenericTaskReturnType()
            {
                string inputCode = @"
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
                        [Function('FunctionName')]
                        public Task<HttpResponseData> Http([HttpTrigger(AuthorizationLevel.Admin, 'get', 'Post', Route = '/api2')] HttpRequestData myReq)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                ".Replace("'", "\"");

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Core;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Http;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            var Function0binding0 = new {
                Name = 'myReq',
                Type = 'HttpTrigger',
                Direction = 'In',
                AuthLevel = (AuthorizationLevel)4,
                Methods = new List<string> { 'get','Post' },
                Route = '/api2',
            };
            var Function0binding0JSON = JsonSerializer.Serialize(Function0binding0, jsonOptions);
            Function0RawBindings.Add(Function0binding0JSON);
            var Function0binding1 = new {
                Name = 'Result',
                Type = 'http',
                Direction = 'Out',
            };
            var Function0binding1JSON = JsonSerializer.Serialize(Function0binding1, jsonOptions);
            Function0RawBindings.Add(Function0binding1JSON);
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'FunctionName',
                EntryPoint = 'TestProject.BasicHttp.Http',
                RawBindings = Function0RawBindings,
                ScriptFile = 'TestProject.dll'
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
".Replace("'", "\"");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void MultipleOutputOnMethodFails()
            {
                var inputCode = @"using System;
                using System.Net;
                using System.Collections;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;
                using System.Linq;
                using System.Threading.Tasks;

                namespace FunctionApp
                {
                    public class EventHubsInput
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

                await TestHelpers.RunTestAsync<ExtensionStartupRunnerGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }
        }
    }
}
