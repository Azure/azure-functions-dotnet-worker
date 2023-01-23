using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FunctionApp;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Tests;

namespace SampleIntegrationTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1()
        {
            using var testVariables = new TestScopedEnvironmentVariable("AzureWebJobsScriptRoot", ".");
            var host = new HostBuilder()
                .ConfigureWebHost(a => a.UseTestServer().UseStartup<Startup>())
                .ConfigureLogging(a => a.AddProvider(new NUnitLoggerProvider()))
                    .ConfigureServices((context, services) =>
                {
                    IFunctionsWorkerApplicationBuilder appBuilder = services.AddFunctionsWorkerDefaults();
                    services.ConfigureGrpcClient(a =>
                        a.ConfigurePrimaryHttpMessageHandler(provider =>
                            ((TestServer)provider.GetRequiredService<IServer>()).CreateHandler()));
                    // Call the provided configuration prior to adding default middleware
                    //configure(context, appBuilder);
                    //appBuilder.Use(c =>
                    //{
                    //    return c;
                    //});
                    // Add default middleware
                    appBuilder.UseDefaultWorkerMiddleware();
                    services.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
                })
                .ConfigureHostConfiguration(a => a.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>( "Host", "localhost"),
                    new KeyValuePair<string, string?>( "Port", "0"),
                    new KeyValuePair<string, string?>( "WorkerId", "1"),
                    new KeyValuePair<string, string?>( "GrpcMaxMessageLength", "134217728"),

                }))
                .Build();

            await host.StartAsync();

            //var functionId = Guid.NewGuid().ToString();
            //var rpcFunctionMetadata = new RpcFunctionMetadata
            //{
            //    Name = "HttpTriggerSimple",
            //    ScriptFile = "FunctionApp.dll",
            //    EntryPoint = "FunctionApp.HttpTriggerSimple.Run",
            //};
            //rpcFunctionMetadata.Bindings.Add("req", new BindingInfo { Type = "httpTrigger" });
            //rpcFunctionMetadata.Bindings.Add("$return", new BindingInfo { Type = "http", Direction = BindingInfo.Types.Direction.Out });
            //await messageProcessor.ProcessMessageAsync(new StreamingMessage
            //{
            //    FunctionLoadRequest = new FunctionLoadRequest
            //    {
            //        FunctionId = functionId,
            //        ManagedDependencyEnabled = true,
            //        Metadata = rpcFunctionMetadata
            //    }
            //});

            
            var invocationRequest = new InvocationRequest
            {
                InvocationId = Guid.NewGuid().ToString(),
                InputData = { new ParameterBinding
                {
                    Name = "req",
                    Data = new TypedData
                    {
                        Http = new RpcHttp
                        {
                            Method = "GET",
                            Url = "http://localhost:0/api/HttpTriggerSimple",

                        }
                    }
                } },
                TraceContext = new RpcTraceContext { },
            };

            //var response = await handler.ProcessMessageAsync(message);

            //Assert.That(response.Result, Is.EqualTo(StatusResult.Success));
            //Assert.That(response.ReturnValue.Http.StatusCode, Is.EqualTo(StatusCodes.Status200OK.ToString()));
            //Assert.That(response.ReturnValue.Http.Body.Bytes.ToStringUtf8(), Is.EqualTo("Hello world!"));

            //await Task.Delay(TimeSpan.FromSeconds(100));
            var response = await host.Services.GetRequiredService<FunctionRpcTestServer>().Test("HttpTriggerSimple", invocationRequest);
            Assert.That(response.Result, Is.EqualTo(StatusResult.Success));
            Assert.That(response.ReturnValue.Http.StatusCode, Is.EqualTo(StatusCodes.Status200OK.ToString()));
            Assert.That(response.ReturnValue.Http.Body.Bytes.ToStringUtf8(), Is.EqualTo("Hello world!"));
            //await Task.Delay(TimeSpan.FromSeconds(100));
            var test2 = await host.GetTestClient().GetAsync("/api/HttpTriggerSimple");
            Assert.That(test2.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var body = await test2.Content.ReadAsStringAsync();
            Assert.That(body, Is.EqualTo("Hello world!"));
        }

    }
}
