// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Sdk.Generator.Tests;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Functions.Sdk.Generator.FunctionMetadataProvider.Tests
{
    public partial class FunctionMetadataProviderGeneratorTests
    {
        public sealed class NotGeneratedTests
        {
            private readonly Assembly[] _referencedExtensionAssemblies = new[]
            {
                typeof(HttpTriggerAttribute).Assembly, typeof(FunctionAttribute).Assembly,
                typeof(LoggingServiceCollectionExtensions).Assembly,
                typeof(ServiceProviderServiceExtensions).Assembly,
                typeof(ILogger).Assembly, typeof(IConfiguration).Assembly, typeof(HostBuilder).Assembly,
                typeof(IHostBuilder).Assembly
            };

            [Theory]
            [InlineData(LanguageVersion.CSharp7_3)]
            [InlineData(LanguageVersion.CSharp8)]
            [InlineData(LanguageVersion.CSharp9)]
            [InlineData(LanguageVersion.CSharp10)]
            [InlineData(LanguageVersion.CSharp11)]
            [InlineData(LanguageVersion.Latest)]
            public async Task NotGeneratedWhenNotRunningInAnAzureFunctionsProject(LanguageVersion languageVersion)
            {
                string inputCode = """
                                   using System;
                                   using System.Collections.Generic;
                                   using Microsoft.Azure.Functions.Worker;
                                   using Microsoft.Azure.Functions.Worker.Http;

                                   namespace MyCompany.MyApp.Functions
                                   {
                                       public static class HttpTriggerSimple
                                       {
                                           [Function(nameof(HttpTriggerSimple))]
                                           public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
                                           {
                                               throw new NotImplementedException();
                                           }
                                       }
                                   }
                                   """;

                string? expectedGeneratedFileName = null;
                string? expectedOutput = null;

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    _referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    languageVersion: languageVersion,
                    runInsideAzureFunctionProject: false);
            }
        }
    }
}
