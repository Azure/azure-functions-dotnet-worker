﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public partial class FunctionMetadataProviderGeneratorTests
    {
        public class AutoConfigureStartupTypeTests
        {
            private readonly Assembly[] _referencedExtensionAssemblies;

            public AutoConfigureStartupTypeTests()
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

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task VerifyAutoGeneratedCodeAttributesAreEmitted(bool includeAutoStartupType)
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
                        [Function("HttpTrigger")]
                        public static void HttpTrigger([HttpTrigger(AuthorizationLevel.Admin, "get", "post", Route = "/api2")] HttpRequestData req)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = @$"// <auto-generated/>
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
{{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {{
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {{
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            Function0RawBindings.Add(@""{{""""name"""":""""req"""",""""type"""":""""httpTrigger"""",""""direction"""":""""In"""",""""authLevel"""":""""Admin"""",""""methods"""":[""""get"""",""""post""""],""""route"""":""""/api2""""}}"");

            var Function0 = new DefaultFunctionMetadata
            {{
                Language = ""dotnet-isolated"",
                Name = ""HttpTrigger"",
                EntryPoint = ""FunctionApp.HttpTriggerSimple.HttpTrigger"",
                RawBindings = Function0RawBindings,
                ScriptFile = ""TestProject.dll""
            }};
            metadataList.Add(Function0);

            return Task.FromResult(metadataList.ToImmutableArray());
        }}
    }}

{GetExpectedExtensionMethodCode(includeAutoStartupType: includeAutoStartupType)}
}}";
                var buildPropertiesDict = new Dictionary<string, string>()
                {
                    {  Constants.BuildProperties.AutoRegisterGeneratedMetadataProvider, includeAutoStartupType.ToString()}
                };
                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    _referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    buildPropertiesDictionary: buildPropertiesDict);
            }
        }

        private static string GetExpectedExtensionMethodCode(bool includeAutoStartupType = false)
        {
            if (includeAutoStartupType)
            {
                return """
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
                            public class FunctionMetadataProviderAutoStartup : IAutoConfigureStartup
                            {
                                public void Configure(IHostBuilder hostBuilder)
                                {
                                    hostBuilder.ConfigureGeneratedFunctionMetadataProvider();
                                }
                            }
                        """;
            }

            return """
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
                    """;
        }
    }
}
