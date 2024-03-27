﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                var hostingExtension = typeof(HostBuilder).Assembly;
                var diExtension = typeof(DefaultServiceProviderFactory).Assembly;
                var hostingAbExtension = typeof(IHost).Assembly;
                var diAbExtension = typeof(IServiceCollection).Assembly;
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
            public async Task FunctionsFromFunctionAppAndDependentAssembly()
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
                                Name = "HttpTriggerSimple",
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
                                Name = "DependencyFunc",
                                EntryPoint = "DependentAssemblyWithFunctions.DependencyFunction.Run",
                                RawBindings = Function1RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function1);
                            var Function2RawBindings = new List<string>();
                            Function2RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function2RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");
                
                            var Function2 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "InternalFunction",
                                EntryPoint = "DependentAssemblyWithFunctions.InternalFunction.Run",
                                RawBindings = Function2RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function2);
                            var Function3RawBindings = new List<string>();
                            Function3RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function3RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");
                
                            var Function3 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "StaticFunction",
                                EntryPoint = "DependentAssemblyWithFunctions.StaticFunction.Run",
                                RawBindings = Function3RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function3);
                            var Function4RawBindings = new List<string>();
                            Function4RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function4RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");
                
                            var Function4 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "NestedNamespaceFunc1",
                                EntryPoint = "MyCompany.MyProduct.MyApp.HttpFunctions.Run",
                                RawBindings = Function4RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function4);
                            var Function5RawBindings = new List<string>();
                            Function5RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function5RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                            var Function5 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "NestedTypeFunc",
                                EntryPoint = "MyCompany.MyProduct.MyApp.Foo.Bar.Run",
                                RawBindings = Function5RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function5);

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
            public async Task FunctionsFromDependentAssemblyOnly()
            {
                string inputCode = """
                using System;
                namespace FunctionApp
                {
                    public class Program
                    {
                        public static void Main()
                        {
                            Console.WriteLine("App main starting");
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
                                Name = "DependencyFunc",
                                EntryPoint = "DependentAssemblyWithFunctions.DependencyFunction.Run",
                                RawBindings = Function0RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function0);
                            var Function1RawBindings = new List<string>();
                            Function1RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function1RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");
                
                            var Function1 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "InternalFunction",
                                EntryPoint = "DependentAssemblyWithFunctions.InternalFunction.Run",
                                RawBindings = Function1RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function1);
                            var Function2RawBindings = new List<string>();
                            Function2RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function2RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");
                
                            var Function2 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "StaticFunction",
                                EntryPoint = "DependentAssemblyWithFunctions.StaticFunction.Run",
                                RawBindings = Function2RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function2);
                            var Function3RawBindings = new List<string>();
                            Function3RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function3RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");
                
                            var Function3 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "NestedNamespaceFunc1",
                                EntryPoint = "MyCompany.MyProduct.MyApp.HttpFunctions.Run",
                                RawBindings = Function3RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function3);
                            var Function4RawBindings = new List<string>();
                            Function4RawBindings.Add(@"{""name"":""req"",""type"":""httpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                            Function4RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                            var Function4 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "NestedTypeFunc",
                                EntryPoint = "MyCompany.MyProduct.MyApp.Foo.Bar.Run",
                                RawBindings = Function4RawBindings,
                                ScriptFile = "DependentAssemblyWithFunctions.dll"
                            };
                            metadataList.Add(Function4);

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
