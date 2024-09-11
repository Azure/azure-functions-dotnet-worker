// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.SdkGeneratorTests.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public partial class FunctionExecutorGeneratorTests
    {
        [Theory]
        [InlineData(LanguageVersion.CSharp7_3)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.CSharp9)]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.Latest)]
        public async Task FunctionsFromMultipleClasses(LanguageVersion languageVersion)
        {
            await Test(
                @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
namespace MyCompany
{
    public class MyHttpTriggers
    {
        [Function(""FunctionA"")]
        public HttpResponseData Foo([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r, FunctionContext c)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }
        
        private int Foo(int x) => x * x;
    }
    public class MyHttpTriggers2
    {
        [Function(""FunctionB"")]
        public HttpResponseData Bar([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }
        
        private int Foo(int x) => x * x;
    }
    public static class Foo
    {
        [Function(""ProcessOrder2"")]
        public static async Task<string> MyAsyncStaticMethod([QueueTrigger(""foo"")] string q) => q;
    }

    public class QueueTriggers
    {
        private readonly ILogger<QueueTriggers> _logger;

        public QueueTriggers(ILogger<QueueTriggers> logger)
        {
            _logger = logger;
        }

        [Function(nameof(QueueTriggers))]
        public void Run([QueueTrigger(""myqueue-items"")] QueueMessage message)
        {
            _logger.LogInformation($""Queue message: {message.MessageText}"");
        }

        [Function(""Run2"")]
        public void Run2([QueueTrigger(""myqueue-items"")] string message)
        {
            _logger.LogInformation($""Queue message: {message}"");
        }
    }
}
", languageVersion);
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp7_3)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.CSharp9)]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.Latest)]
        public async Task MultipleFunctionsDependencyInjection(LanguageVersion languageVersion)
        {
            await Test(@"
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyCompany
{
    public class MyHttpTriggers
    {
        private readonly ILogger _logger;
        public MyHttpTriggers(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MyHttpTriggers>();
        }

        [Function(""Function1"")]
        public HttpResponseData Run1([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r)
            => r.CreateResponse(System.Net.HttpStatusCode.OK);

        [Function(""Function2"")]
        public HttpResponseData Run2([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r, FunctionContext c)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        private int Foo(int x) => x * x;
    }
}
", languageVersion,
generatedCodeNamespace: "MyCompany.MyProject.MyApp");
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp7_3)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.CSharp9)]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.Latest)]
        public async Task StaticMethods(LanguageVersion languageVersion)
        {
            await Test("""
                using System;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Azure.Messaging.EventHubs;
                using System.IO;
                
                namespace FunctionApp26
                {
                    public static class MyQTriggers
                    {
                        [Function("ProcessOrder1")]
                        public static Task MyTaskStaticMethod([QueueTrigger("foo")] string q)
                        {
                            return Task.CompletedTask;
                        }
                        [Function("ProcessOrder2")]
                        public static async Task<string> MyAsyncStaticMethod([QueueTrigger("foo")] string q) => q;
                
                        [Function("ProcessOrder3")]
                        public static void MyVoidStaticMethod([QueueTrigger("foo")] string q)
                        {
                        }
                        [Function("ProcessOrder4")]
                        public static async Task<int> MyAsyncStaticMethodWithReturn(
                                    [QueueTrigger("foo")] string q,
                                    [BlobInput("test-samples/sample1.txt")] string myBlob)
                        {
                            return q.Length + myBlob.Length;
                        }
                        [Function("ProcessOrder5")]
                        public static async ValueTask<string> MyValueTaskOfTStaticAsyncMethod([QueueTrigger("foo")] string q)
                        {
                            return q;
                        }
                        [Function("ProcessOrder6")]
                        public static ValueTask MyValueTaskStaticAsyncMethod2([QueueTrigger("foo")] string q)
                        {
                            return ValueTask.CompletedTask;
                        }
                    }
                    public class BlobTriggers
                    {
                        [Function(nameof(BlobTriggers))]
                        public static async Task Run([BlobTrigger("items/{name}", Connection = "ConStr")] Stream stream, string name)
                        {
                            using (var blobStreamReader = new StreamReader(stream))
                            {
                                var content = await blobStreamReader.ReadToEndAsync();
                            }
                        }
                    }
                    public class EventHubTriggers
                    {
                        [Function("Run1")]
                        public static void Run1([EventHubTrigger("items", Connection = "EventHubConnection")] EventData[] data)
                        {
                        }
                        [Function(nameof(Run2))]
                        [EventHubOutput("dest", Connection = "EHConnection")]
                        public static string Run2([EventHubTrigger("queue", Connection = "EventHubConnection", IsBatched = false)] EventData eventData)
                        {
                            return eventData.MessageId;
                        }
                        [Function("RunAsync1")]
                        public static Task RunAsync1([EventHubTrigger("items", Connection = "Con")] EventData[] data)
                        {
                            return Task.CompletedTask;
                        }
                        [Function("RunAsync2")]
                        public static async Task RunAsync2([EventHubTrigger("items", Connection = "Con")] EventData[] data) => await Task.Delay(10);
                    }
                }
                """,
                languageVersion);
        }

        [Theory]
        [InlineData(true, LanguageVersion.CSharp7_3)]
        [InlineData(true, LanguageVersion.CSharp8)]
        [InlineData(true, LanguageVersion.CSharp9)]
        [InlineData(true, LanguageVersion.CSharp10)]
        [InlineData(true, LanguageVersion.CSharp11)]
        [InlineData(true, LanguageVersion.Latest)]
        [InlineData(false, LanguageVersion.CSharp7_3)]
        [InlineData(false, LanguageVersion.CSharp8)]
        [InlineData(false, LanguageVersion.CSharp9)]
        [InlineData(false, LanguageVersion.CSharp10)]
        [InlineData(false, LanguageVersion.CSharp11)]
        [InlineData(false, LanguageVersion.Latest)]
        public async Task VerifyAutoConfigureStartupTypeEmitted(bool includeAutoStartupType, LanguageVersion languageVersion)
        {
            await Test("""
                using System.Net;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.Logging;
                
                namespace MyCompany
                {
                    public class MyHttpTriggers
                    {
                        [Function("Function1")]
                        public HttpResponseData Run1([HttpTrigger(AuthorizationLevel.User, "get")] HttpRequestData r)
                        {
                            return r.CreateResponse(System.Net.HttpStatusCode.OK);
                        }
                    }
                }
                """,
                languageVersion);
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp7_3)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.CSharp9)]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.Latest)]
        public async Task ClassWithSameNameAsNamespace(LanguageVersion languageVersion)
        {
            await Test("""
                using System;
                using System.Threading.Tasks;
                using Microsoft.Extensions.Hosting;
                using Azure.Storage.Queues.Models;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;
                using Microsoft.Extensions.Logging;
                namespace TestProject
                {
                    public class TestProject
                    {
                        [Function("FunctionA")]
                        public HttpResponseData Foo([HttpTrigger(AuthorizationLevel.User, "get")] HttpRequestData r, FunctionContext c)
                        {
                            return r.CreateResponse(System.Net.HttpStatusCode.OK);
                        }
                
                        [Function("FunctionB")]
                        public static HttpResponseData FooStatic([HttpTrigger(AuthorizationLevel.User, "get")] HttpRequestData r, FunctionContext c)
                        {
                            return r.CreateResponse(System.Net.HttpStatusCode.OK);
                        }
                    }
                }
                """,
                languageVersion);
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp7_3)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.CSharp9)]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.Latest)]
        public async Task FunctionsWithSameNameExceptForCasing(LanguageVersion languageVersion)
        {
            await Test("""
                using System;
                using System.Threading.Tasks;
                using Microsoft.Extensions.Hosting;
                using Azure.Storage.Queues.Models;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;
                using Microsoft.Extensions.Logging;
                namespace MyCompany
                {
                    public class MyHttpTriggers
                    {
                        [Function("FunctionA")]
                        public HttpResponseData Hello([HttpTrigger(AuthorizationLevel.User, "get")] HttpRequestData r, FunctionContext c)
                        {
                            return r.CreateResponse(System.Net.HttpStatusCode.OK);
                        }
                
                        [Function("FunctionB")]
                        public static HttpResponseData HELLO([HttpTrigger(AuthorizationLevel.User, "get")] HttpRequestData r, FunctionContext c)
                        {
                            return r.CreateResponse(System.Net.HttpStatusCode.OK);
                        }
                    }
                }
                """,
                languageVersion);
        }

        private async Task Test(
            string sourceCode,
            LanguageVersion languageVersion,
            string? generatedCodeNamespace = null,
            [CallerMemberName] string callerName = "")
        {
            await new FunctionExecutorGenerator()
                .RunAndVerify(
                    sourceCode,
                    new[]
                    {
                        typeof(HttpTriggerAttribute).Assembly,
                        typeof(FunctionAttribute).Assembly,
                        typeof(QueueTriggerAttribute).Assembly,
                        typeof(EventHubTriggerAttribute).Assembly,
                        typeof(QueueMessage).Assembly,
                        typeof(EventData).Assembly,
                        typeof(BlobInputAttribute).Assembly,
                        typeof(ServiceProviderServiceExtensions).Assembly,
                        typeof(ILogger).Assembly,
                        typeof(IConfiguration).Assembly,
                        typeof(HostBuilder).Assembly,
                        typeof(IHostBuilder).Assembly
                    },
                    languageVersion: languageVersion,
                    generatedCodeNamespace: generatedCodeNamespace,
                    callerName: callerName);
        }
    }
}
