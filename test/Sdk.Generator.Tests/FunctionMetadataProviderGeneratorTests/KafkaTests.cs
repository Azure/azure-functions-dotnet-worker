﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public partial class FunctionMetadataProviderGeneratorTests
    {
        public class KafkaTests
        {
            private readonly Assembly[] _referencedExtensionAssemblies;

            public KafkaTests()
            {
                // load all extensions used in tests (match extensions tested on E2E app? Or include ALL extensions?)
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var hostingExtension = typeof(HostBuilder).Assembly;
                var diExtension = typeof(DefaultServiceProviderFactory).Assembly;
                var hostingAbExtension = typeof(IHost).Assembly;
                var diAbExtension = typeof(IServiceCollection).Assembly;
                var kafkaExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Kafka.dll");

                _referencedExtensionAssemblies = new[]
                {
                    abstractionsExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension,
                    kafkaExtension
                };
            }

            [Theory]
            [InlineData(LanguageVersion.CSharp7_3)]
            [InlineData(LanguageVersion.CSharp8)]
            [InlineData(LanguageVersion.CSharp9)]
            [InlineData(LanguageVersion.CSharp10)]
            [InlineData(LanguageVersion.CSharp11)]
            [InlineData(LanguageVersion.Latest)]
            public async Task GenerateSimpleKafkaTriggerTest(LanguageVersion languageVersion)
            {
                string inputCode = """
                using System;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class KafkaFunction
                    {
                        [Function(nameof(KafkaFunction))]
                        [FixedDelayRetry(5, "00:00:10")]
                        [KafkaOutput("LocalBroker", "stringTopicTenPartitions")]
                        public static string Run([KafkaTrigger("LocalBroker", "stringTopicTenPartitions",
                            ConsumerGroup = "$Default", AuthenticationMode = BrokerAuthenticationMode.Plain)] string input,
                            FunctionContext context)
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
                    /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker."/>
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
                            Function0RawBindings.Add(@"{""name"":""$return"",""type"":""kafka"",""direction"":""Out"",""brokerList"":""LocalBroker"",""topic"":""stringTopicTenPartitions""}");
                            Function0RawBindings.Add(@"{""name"":""input"",""type"":""kafkaTrigger"",""direction"":""In"",""brokerList"":""LocalBroker"",""topic"":""stringTopicTenPartitions"",""consumerGroup"":""$Default"",""authenticationMode"":""Plain"",""cardinality"":""One"",""dataType"":""String""}");

                            var Function0 = new DefaultFunctionMetadata
                            {
                                Language = "dotnet-isolated",
                                Name = "KafkaFunction",
                                EntryPoint = "FunctionApp.KafkaFunction.Run",
                                RawBindings = Function0RawBindings,
                                Retry = new DefaultRetryOptions
                                {
                                    MaxRetryCount = 5,
                                    DelayInterval = TimeSpan.Parse("00:00:10")
                                },
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
