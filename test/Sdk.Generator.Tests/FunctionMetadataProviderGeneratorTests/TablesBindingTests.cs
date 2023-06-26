// Copyright (c) .NET Foundation. All rights reserved.
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
        public class TablesBindingTests
        {
            private readonly Assembly[] _referencedExtensionAssemblies;

            public TablesBindingTests()
            {
                // load all extensions used in tests (match extensions tested on E2E app? Or include ALL extensions?)
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var storageExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Storage.dll");
                var tableExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Tables.dll");
                var hostingExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.dll");
                var diExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.dll");
                var hostingAbExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.Abstractions.dll");
                var diAbExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.Abstractions.dll");
                var AzureTableExtension = Assembly.LoadFrom("Azure.Data.Tables.dll");

                _referencedExtensionAssemblies = new[]
                {
                    abstractionsExtension,
                    httpExtension,
                    tableExtension,
                    storageExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension,
                    AzureTableExtension
                };
            }

            [Fact]
            public async void TestTableExtension_InputBinding_MetadataGenerated()
            {
                string inputCode = """
                    using System;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Net;
                    using System.Text.Json.Serialization;
                    using System.Threading.Tasks;
                    using Azure.Data.Tables;
                    using Microsoft.Azure.Functions.Worker;
                    using Microsoft.Azure.Functions.Worker.Http;

                    namespace FunctionApp
                    {
                        public class TableTest
                        {                
                            [Function("Function1")]
                            public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
                                [TableInput("TableName")] IEnumerable<TableEntity> table)
                            {
                                throw new NotImplementedException();
                            }

                            [Function(nameof(TableClientFunction))]
                            public async Task<HttpResponseData> TableClientFunction(
                                [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
                                [TableInput("TableName")] TableClient table)
                            {
                                throw new NotImplementedException();
                            }

                            [Function(nameof(ReadTableDataFunction))]
                            public async Task<HttpResponseData> ReadTableDataFunction(
                                [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "items/{partitionKey}/{rowKey}")] HttpRequestData req,
                                [TableInput("TableName", "{partitionKey}", "{rowKey}")] TableEntity table)
                            {
                                throw new NotImplementedException();
                            }

                            [Function("DoesNotSupportDeferredBinding")]
                            public static void TableInput(
                                [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
                                [TableInput("MyTable", "MyPartition", "yo")] MyPoco poco)
                            {
                                 throw new NotImplementedException();
                            }

                            public class MyPoco
                            {
                                public string PartitionKey { get; set; }
                                public string RowKey { get; set; }
                                public string Text { get; set; }
                            }
                        }
                    }
                    """;

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string? expectedOutput = """
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
                                Function0RawBindings.Add(@"{""name"":""table"",""type"":""Table"",""direction"":""In"",""properties"":{""supportsDeferredBinding"":""True""},""tableName"":""TableName""}");
                                Function0RawBindings.Add(@"{""name"":""$return"",""type"":""http"",""direction"":""Out""}");

                                var Function0 = new DefaultFunctionMetadata
                                {
                                    Language = "dotnet-isolated",
                                    Name = "Function1",
                                    EntryPoint = "FunctionApp.TableTest.Run",
                                    RawBindings = Function0RawBindings,
                                    ScriptFile = "TestProject.dll"
                                };
                                metadataList.Add(Function0);
                                var Function1RawBindings = new List<string>();
                                Function1RawBindings.Add(@"{""name"":""req"",""type"":""HttpTrigger"",""direction"":""In"",""authLevel"":""Function"",""methods"":[""get"",""post""]}");
                                Function1RawBindings.Add(@"{""name"":""table"",""type"":""Table"",""direction"":""In"",""properties"":{""supportsDeferredBinding"":""True""},""tableName"":""TableName""}");
                                Function1RawBindings.Add(@"{""name"":""Result"",""type"":""http"",""direction"":""Out""}");

                                var Function1 = new DefaultFunctionMetadata
                                {
                                    Language = "dotnet-isolated",
                                    Name = "TableClientFunction",
                                    EntryPoint = "FunctionApp.TableTest.TableClientFunction",
                                    RawBindings = Function1RawBindings,
                                    ScriptFile = "TestProject.dll"
                                };
                                metadataList.Add(Function1);
                                var Function2RawBindings = new List<string>();
                                Function2RawBindings.Add(@"{""name"":""req"",""type"":""HttpTrigger"",""direction"":""In"",""authLevel"":""Function"",""methods"":[""get"",""post""],""route"":""items/{partitionKey}/{rowKey}""}");
                                Function2RawBindings.Add(@"{""name"":""table"",""type"":""Table"",""direction"":""In"",""properties"":{""supportsDeferredBinding"":""True""},""tableName"":""TableName"",""partitionKey"":""{partitionKey}"",""rowKey"":""{rowKey}""}");
                                Function2RawBindings.Add(@"{""name"":""Result"",""type"":""http"",""direction"":""Out""}");
                    
                                var Function2 = new DefaultFunctionMetadata
                                {
                                    Language = "dotnet-isolated",
                                    Name = "ReadTableDataFunction",
                                    EntryPoint = "FunctionApp.TableTest.ReadTableDataFunction",
                                    RawBindings = Function2RawBindings,
                                    ScriptFile = "TestProject.dll"
                                };
                                metadataList.Add(Function2);
                                var Function3RawBindings = new List<string>();
                                Function3RawBindings.Add(@"{""name"":""req"",""type"":""HttpTrigger"",""direction"":""In"",""authLevel"":""Anonymous"",""methods"":[""get"",""post""]}");
                                Function3RawBindings.Add(@"{""name"":""poco"",""type"":""Table"",""direction"":""In"",""tableName"":""MyTable"",""partitionKey"":""MyPartition"",""rowKey"":""yo""}");
                    
                                var Function3 = new DefaultFunctionMetadata
                                {
                                    Language = "dotnet-isolated",
                                    Name = "DoesNotSupportDeferredBinding",
                                    EntryPoint = "FunctionApp.TableTest.TableInput",
                                    RawBindings = Function3RawBindings,
                                    ScriptFile = "TestProject.dll"
                                };
                                metadataList.Add(Function3);

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
