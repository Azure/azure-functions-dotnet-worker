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
        public class NestedTypesTests
        {
            private readonly Assembly[] _referencedExtensionAssemblies;

            public NestedTypesTests()
            {
                // load all extensions used in tests (match extensions tested on E2E app? Or include ALL extensions?)
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var hostingExtension = typeof(HostBuilder).Assembly;
                var diExtension = typeof(DefaultServiceProviderFactory).Assembly;
                var hostingAbExtension = typeof(IHost).Assembly;
                var diAbExtension = typeof(IServiceCollection).Assembly;

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

            [Theory]
            [InlineData(LanguageVersion.CSharp7_3)]
            [InlineData(LanguageVersion.CSharp8)]
            [InlineData(LanguageVersion.CSharp9)]
            [InlineData(LanguageVersion.CSharp10)]
            [InlineData(LanguageVersion.CSharp11)]
            [InlineData(LanguageVersion.Latest)]
            public async Task NestedNamespace(LanguageVersion languageVersion)
            {
                string inputCode = """
                using System;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace MyCompany
                {
                    namespace MyProject
                    {
                        public static class HttpTriggerSimple
                        {
                            [Function(nameof(HttpTriggerSimple))]
                            public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.User, "get")] HttpRequestData req, FunctionContext c)
                            {
                                return Run(req);
                            }

                            public static HttpResponseData Run(HttpRequestData req)
                                => req.CreateResponse(System.Net.HttpStatusCode.OK);
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
                            Function0RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""User"",""methods"":[""get""]}");
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "HttpTriggerSimple",
                                EntryPoint = "MyCompany.MyProject.HttpTriggerSimple.Run",
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
            public async Task NestedClass(LanguageVersion languageVersion)
            {
                string inputCode = """
                using System;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace MyCompany
                {
                    public class  WebApis
                    {
                        public static class HttpTriggerSimple
                        {
                            [Function(nameof(HttpTriggerSimple))]
                            public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.User, "get")] HttpRequestData req)
                            {
                                throw new NotImplementedException();
                            }
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
                            Function0RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""User"",""methods"":[""get""]}");
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "HttpTriggerSimple",
                                EntryPoint = "MyCompany.WebApis.HttpTriggerSimple.Run",
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
