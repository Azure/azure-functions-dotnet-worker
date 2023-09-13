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
        public class DependentAssemblyTest
        {
            private readonly Assembly[] _referencedExtensionAssemblies;

            public DependentAssemblyTest()
            {
                // load all extensions used in tests (match extensions tested on E2E app? Or include ALL extensions?)
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var hostingExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.dll");
                var diExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.dll");
                var hostingAbExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.Abstractions.dll");
                var diAbExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.Abstractions.dll");
                var dependentAssembly = Assembly.LoadFrom("DependentAssemblyWithFunctions.dll");

                _referencedExtensionAssemblies = new[]
                {
                    abstractionsExtension,
                    httpExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension,
                    dependentAssembly
                };
            }

            [Fact]
            public async Task SimpleFunctionInDependentAssemblyTest()
            {
                string inputCode = """
                using System;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {
                    public static class HttpTriggerSimple
                    {
                        [Function(nameof(HttpTriggerSimple))]
                        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, FunctionContext executionContext)
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
                            Function0RawBindings.Add(@"{""name"":""req"",""type"":""HttpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "HttpTriggerSimple",
                                EntryPoint = "FunctionApp.HttpTriggerSimple.Run",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "TestProject.dll"
                            };
                            metadataList.Add(Function0);
                            var Function1RawBindings = new List<string>();
                            Function1RawBindings.Add(@"{""name"":""req"",""type"":""HttpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function1RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");
                
                            var Function1 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "DependencyFunc",
                                EntryPoint = "DependentAssemblyWithFunctions.DependencyFunc.Run",
                                RawBindings = Function1RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function1);

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
        }
    }
}
