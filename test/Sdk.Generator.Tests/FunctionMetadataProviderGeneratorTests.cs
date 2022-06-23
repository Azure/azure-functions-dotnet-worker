﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public class FunctionMetadataProviderGeneratorTests
    {
        [Fact]
        public async Task GenerateSimpleHttpTriggerMetadata()
        {
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

            var workerAssembly = Assembly.LoadFrom("C:\\Users\\sarahvu\\source\\repos\\azure-functions-dotnet-worker\\test\\E2ETests\\E2EApps\\E2EApp\\bin\\Debug\\net5.0\\Microsoft.Azure.Functions.Worker.dll");
            var httpAssembly = Assembly.LoadFrom("C:\\Users\\sarahvu\\source\\repos\\azure-functions-dotnet-worker\\test\\E2ETests\\E2EApps\\E2EApp\\bin\\Debug\\net5.0\\Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
            var extensionsAbstractionsAssembly = Assembly.LoadFrom("C:\\Users\\sarahvu\\source\\repos\\azure-functions-dotnet-worker\\test\\E2ETests\\E2EApps\\E2EApp\\bin\\Debug\\net5.0\\Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");

            // Source generation is based on referenced assembly.
            var referencedExtensionAssemblies = new[]
            {
                workerAssembly,
                httpAssembly,
                extensionsAbstractionsAssembly
            };

            /*            var referencedExtensionAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            */

            string expectedGeneratedFileName = $"SourceGeneratedFunctionMetadataProvider.g.cs";
            string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
".Replace("'", "\"");

            await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                referencedExtensionAssemblies,
                inputCode,
                expectedGeneratedFileName,
                expectedOutput);
        }

        [Fact]
        public async Task GenerateHttpTriggerWithBlobInputMetadata()
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

            var workerAssembly = Assembly.LoadFrom("C:\\Users\\sarahvu\\source\\repos\\azure-functions-dotnet-worker\\test\\E2ETests\\E2EApps\\E2EApp\\bin\\Debug\\net5.0\\Microsoft.Azure.Functions.Worker.dll");
            var httpAssembly = Assembly.LoadFrom("C:\\Users\\sarahvu\\source\\repos\\azure-functions-dotnet-worker\\test\\E2ETests\\E2EApps\\E2EApp\\bin\\Debug\\net5.0\\Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
            var extensionsAbstractionsAssembly = Assembly.LoadFrom("C:\\Users\\sarahvu\\source\\repos\\azure-functions-dotnet-worker\\test\\E2ETests\\E2EApps\\E2EApp\\bin\\Debug\\net5.0\\Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
            var blobAssembly = Assembly.LoadFrom("C:\\Users\\sarahvu\\source\\repos\\azure-functions-dotnet-worker\\test\\E2ETests\\E2EApps\\E2EApp\\bin\\Debug\\net5.0\\Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.dll");
            var queueAssembly = Assembly.LoadFrom("C:\\Users\\sarahvu\\source\\repos\\azure-functions-dotnet-worker\\test\\E2ETests\\E2EApps\\E2EApp\\bin\\Debug\\net5.0\\Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues.dll");
            var storageAssembly = Assembly.LoadFrom("C:\\Users\\sarahvu\\source\\repos\\azure-functions-dotnet-worker\\test\\E2ETests\\E2EApps\\E2EApp\\bin\\Debug\\net5.0\\Microsoft.Azure.Functions.Worker.Extensions.Storage.dll");
            // Source generation is based on referenced assembly.
            var referencedExtensionAssemblies = new[]
            {
                workerAssembly,
                httpAssembly,
                extensionsAbstractionsAssembly,
                blobAssembly,
                queueAssembly,
                storageAssembly
            };

            /*            var referencedExtensionAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            */

            string expectedGeneratedFileName = $"SourceGeneratedFunctionMetadataProvider.g.cs";
            string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal class DefaultFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public virtual async Task<ImmutableArray<RpcFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var metadataList = new List<RpcFunctionMetadata>();
            var HttpTriggerSimple = new RpcFunctionMetadata();
            HttpTriggerSimple.Name = 'HttpTriggerSimple';
            HttpTriggerSimple.ScriptFile = '....somepath';
            HttpTriggerSimple.Language = 'dotnet-isolated';            
            HttpTriggerSimple.EntryPoint = 'some-entry';
            HttpTriggerSimple.IsProxy = false;
            HttpTriggerSimple.FunctionId = Guid.NewGuid().ToString();
    }
}
".Replace("'", "\"");

            await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                referencedExtensionAssemblies,
                inputCode,
                expectedGeneratedFileName,
                expectedOutput);
        }
    }
}
