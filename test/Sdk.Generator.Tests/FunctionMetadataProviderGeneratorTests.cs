﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public class FunctionMetadataProviderGeneratorTests
    {
        private Assembly[] referencedExtensionAssemblies;

        public FunctionMetadataProviderGeneratorTests()
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
                loggerExtension
            };

        }

        [Fact]
        public async Task GenerateSimpleHttpTriggerMetadata()
        {
            // test generating function metadata for a simple HttpTrigger
            string inputCode = @"
            using System;
            using System.Collections.Generic;
            using System.Diagnostics;
            using System.Net;
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Azure.Functions.Worker.Http;
            using Microsoft.Extensions.Logging;

            namespace FunctionApp
            {
                public static class HttpTriggerSimple
                {
                    [Function(nameof(HttpTriggerSimple))]
                    public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, ""get"", ""post"", Route = null)] HttpRequestData req, FunctionContext executionContext)
                    {
                        var sw = new Stopwatch();
                    sw.Restart();

                        var logger = executionContext.GetLogger(""FunctionApp.HttpTriggerSimple"");
                    logger.LogInformation(""Message logged"");

                        var response = req.CreateResponse(HttpStatusCode.OK);

                    response.Headers.Add(""Date"", ""Mon, 18 Jul 2016 16:06:00 GMT"");
                        response.Headers.Add(""Content-Type"", ""text/html; charset=utf-8"");
                        response.WriteString(""Hello world!"");

                        logger.LogMetric(@""funcExecutionTimeMs"", sw.Elapsed.TotalMilliseconds,
                            new Dictionary<string, object> {
                                { ""foo"", ""bar""},
                                { ""baz"", 42 }
                            }
                        );

                        return response;
                    }
                }
            }";

  
            string expectedGeneratedFileName = $"SourceGeneratedFunctionMetadataProvider.g.cs";
            string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Core;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var metadataList = new List<IFunctionMetadata>();
            var HttpTriggerSimpleRawBindings = new List<string>();
            var HttpTriggerSimpleBinding0 = new {
                name = 'req',
                type = 'HttpTrigger',
                direction = 'In',
                authLevel = Enum.GetName(typeof(AuthorizationLevel), 0),
                methods = new List<string> { 'get','post' },
            };
            var HttpTriggerSimpleBinding0JSONstring = JsonSerializer.Serialize(HttpTriggerSimpleBinding0).ToString();
            HttpTriggerSimpleRawBindings.Add(HttpTriggerSimpleBinding0JSONstring);
            var HttpTriggerSimpleBinding1 = new {
                name = '$return',
                type = 'http',
                direction = 'Out',
            };
            var HttpTriggerSimpleBinding1JSONstring = JsonSerializer.Serialize(HttpTriggerSimpleBinding1).ToString();
            HttpTriggerSimpleRawBindings.Add(HttpTriggerSimpleBinding1JSONstring);
            var HttpTriggerSimple = new DefaultFunctionMetadata(Guid.NewGuid().ToString(), 'dotnet-isolated', 'HttpTriggerSimple', 'TestProject.HttpTriggerSimple.Run', HttpTriggerSimpleRawBindings, 'TestProject.dll');
            metadataList.Add(HttpTriggerSimple);
            return Task.FromResult(metadataList.ToImmutableArray());
        }
        public enum AuthorizationLevel
        {
            Anonymous,
            User,
            Function,
            System,
            Admin
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
        public void FormatStringObjectTest()
        {
            var stringObject = "\"get\"";
            var result = FunctionMetadataProviderGenerator.FormatObject(stringObject);
            // this method should not alter objects that are already strings
            Assert.Equal(stringObject, result);
        }

        [Fact]
        public void FormatEnumObjectTest()
        {
            var enumObjectString = "Enum.GetName(typeof(AuthorizationLevel), 0)";
            var result = FunctionMetadataProviderGenerator.FormatObject(enumObjectString);
            // this method shouldn't alter Enum parsing statements - we want them source generated as a method call, not as a string.
            Assert.Equal(enumObjectString, result);
        }

        [Fact]
        public void FormatNullObjectTest()
        {
            var result = FunctionMetadataProviderGenerator.FormatObject(null);
            Assert.Equal("null", result);
        }

        [Fact]
        public void FormatObjectToGeneratedStringTest()
        {
            var attributeObject = "HttpTrigger";
            var expected = "\"HttpTrigger\"";
            string result = FunctionMetadataProviderGenerator.FormatObject(attributeObject);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void FormatArrayTest()
        {
            var exampleEnumerable = new List<string> { "get", "post" };
            var expected = "new List<string> { \"get\",\"post\" }";
            var result = FunctionMetadataProviderGenerator.FormatArray(exampleEnumerable);
            Assert.Equal(expected, result);
        }

/*        [Fact]
        public void LoadConstructorArgumentsDifferentNumberOfArgsTest()
        {
// still wip for testing this method
            var mockMethodSymbol = new Mock<IMethodSymbol>();
            var mockAttrData = new Mock<AttributeData>();
            var mockParamSymbol = new Mock<IParameterSymbol>();

            mockMethodSymbol.Setup(m => m.Parameters).Returns(new ImmutableArray<IParameterSymbol> { mockParamSymbol.Object });
            mockAttrData.Setup(a => a.ConstructorArguments).Returns(new ImmutableArray<TypedConstant>());

            var emptyDict = new Dictionary<string, object>();

            Assert.Throws<InvalidOperationException>( () => FunctionMetadataProviderGenerator.LoadConstructorArguments(mockMethodSymbol.Object, mockAttrData.Object, emptyDict));
        }*/
    }
}
