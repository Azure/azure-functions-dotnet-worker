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
        public class HttpTriggerTests
        {
            private Assembly[] _referencedExtensionAssemblies;

            public HttpTriggerTests()
            {
                // load all extensions used in tests (match extensions tested on E2E app? Or include ALL extensions?)
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var hostingExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.dll");
                var diExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.dll");
                var hostingAbExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.Abstractions.dll");
                var diAbExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.Abstractions.dll");

                _referencedExtensionAssemblies = new[]
                {
                    abstractionsExtension,
                    httpExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension
                };
            }

            [Fact]
            public async Task GenerateSimpleHttpTriggerMetadataTest()
            {
                string inputCode = @"
            using System;
            using System.Collections.Generic;
            using System.Net;
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Azure.Functions.Worker.Http;

            namespace FunctionApp
            {
                public static class HttpTriggerSimple
                {
                    [Function(nameof(HttpTriggerSimple))]
                    public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, 'get', 'post', Route = null)] HttpRequestData req, FunctionContext executionContext)
                    {
                        throw new NotImplementedException();
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
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            var Function0binding0 = new {
                Name = 'req',
                Type = 'HttpTrigger',
                Direction = 'In',
                AuthLevel = (AuthorizationLevel)0,
                Methods = new List<string> { 'get','post' },
            };
            var Function0binding0JSON = JsonSerializer.Serialize(Function0binding0);
            Function0RawBindings.Add(Function0binding0JSON);
            var Function0binding1 = new {
                Name = '$return',
                Type = 'http',
                Direction = 'Out',
            };
            var Function0binding1JSON = JsonSerializer.Serialize(Function0binding1);
            Function0RawBindings.Add(Function0binding1JSON);
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'HttpTriggerSimple',
                EntryPoint = 'TestProject.HttpTriggerSimple.Run',
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
                    _referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void BasicHttpFunctionWithNoResponse()
            {
                string inputCode = @"
                using System;
                using System.Collections.Generic;
                using System.Net;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public static class HttpTriggerSimple
                    {
                        [Function(""HttpTrigger"")]
                        public static void HttpTrigger([HttpTrigger(AuthorizationLevel.Admin, ""get"", ""post"", Route = ""/api2"")] HttpRequestData req)
                        {
                            throw new NotImplementedException();
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
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            var Function0binding0 = new {
                Name = 'req',
                Type = 'HttpTrigger',
                Direction = 'In',
                AuthLevel = (AuthorizationLevel)4,
                Methods = new List<string> { 'get','post' },
                Route = ""/api2"",
            };
            var Function0binding0JSON = JsonSerializer.Serialize(Function0binding0);
            Function0RawBindings.Add(Function0binding0JSON);
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'HttpTrigger',
                EntryPoint = 'TestProject.HttpTriggerSimple.HttpTrigger',
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
                    _referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void ReturnTypeJustHttp()
            {
                string inputCode = @"
                using System;
                using System.Collections.Generic;
                using System.Net;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public class HttpTriggerSimple
                    {
                        [Function(""JustHtt"")]
                        public JustHttp Justhtt([HttpTrigger(""get"")] string req)
                        {
                            throw new NotImplementedException();
                        }

                        public class JustHttp
                        {
                            public HttpResponseData httpResponseProp { get; set; }
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
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            var Function0binding0 = new {
                Name = 'req',
                Type = 'HttpTrigger',
                Direction = 'In',
                Methods = new List<string> { 'get' },
                DataType = 'String',
            };
            var Function0binding0JSON = JsonSerializer.Serialize(Function0binding0);
            Function0RawBindings.Add(Function0binding0JSON);
            var Function0binding1 = new {
                Name = ""httpResponseProp"",
                Type = ""http"",
                Direction = ""Out"",
            };
            var Function0binding1JSON = JsonSerializer.Serialize(Function0binding1);
            Function0RawBindings.Add(Function0binding1JSON);
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'JustHtt',
                EntryPoint = 'TestProject.HttpTriggerSimple.Justhtt',
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
                    _referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }
        }
    }
}
