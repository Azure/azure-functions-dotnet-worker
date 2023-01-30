using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FunctionApp;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.TestServer;

namespace SampleIntegrationTests
{
    public class Tests
    {
        private IHost _host;
        private ITestServer _testServer;

        [OneTimeSetUp]
        public async Task Setup()
        {
            using var testVariables = new TestScopedEnvironmentVariable("AzureWebJobsScriptRoot", ".");
            _host = new HostBuilder()
                .ConfigureWebHost(a => a.UseTestServer().UseStartup<Startup>())
                .ConfigureLogging(a => a.AddProvider(new NUnitLoggerProvider()))
                .ConfigureServices((context, services) =>
                {
                    IFunctionsWorkerApplicationBuilder appBuilder = services.AddFunctionsWorkerDefaults();
                    services.ConfigureGrpcClient(a =>
                        a.ConfigurePrimaryHttpMessageHandler(provider =>
                            ((TestServer)provider.GetRequiredService<IServer>()).CreateHandler()));

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

            await _host.StartAsync();

            _testServer = _host.Services.GetRequiredService<ITestServer>();
            await _testServer.StartAsync();
        }

        [Test]
        public async Task Sample_using_http_trigger()
        {
            var response = await _testServer.CallAsync("HttpTriggerSimple", new Dictionary<string, object>
            {
                ["req"] = new TestHttpRequest(new Uri("http://localhost:0/api/HttpTriggerSimple"))
            });
            Assert.That(response.Result, Is.EqualTo(StatusResult.Success));
            Assert.That(response.ReturnValue.Http.StatusCode, Is.EqualTo(StatusCodes.Status200OK.ToString()));
            Assert.That(response.ReturnValue.Http.Body.Bytes.ToStringUtf8(), Is.EqualTo("Hello world!"));
        }


        [Test]
        public async Task Sample_using_http_trigger_with_injection()
        {
            var response = await _testServer.CallAsync("HttpTriggerWithDependencyInjection", new Dictionary<string, object>
            {
                ["req"] = new TestHttpRequest(new Uri("http://localhost:0/api/HttpTriggerWithDependencyInjection"))
            });
            Assert.That(response.Result, Is.EqualTo(StatusResult.Success));
            Assert.That(response.ReturnValue.Http.StatusCode, Is.EqualTo(StatusCodes.Status200OK.ToString()));
            Assert.That(response.ReturnValue.Http.Body.Bytes.ToStringUtf8(), Is.EqualTo("Welcome to .NET 5!!"));
        }

        [Test]
        public async Task Sample_using_http_trigger_with_Blob_input()
        {
            var book = new Book { id = "1", name = "How to test isolated functions" };

            string sample1 = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(book));
            var response = await _testServer.CallAsync("HttpTriggerWithBlobInput", new Dictionary<string, object>
            {
                ["req"] = new TestHttpRequest(new Uri("http://localhost:0/api/HttpTriggerSimple")),
                ["myBlob"] = sample1
            });
            Assert.That(response.Result, Is.EqualTo(StatusResult.Success));
            var http = response.OutputData.First(a => a.BindingName == "HttpResponse").Data.Http;
            Assert.That(http.StatusCode, Is.EqualTo(StatusCodes.Status200OK.ToString()));
            Assert.That(http.Body.Bytes.ToStringUtf8(), Is.EqualTo("Book Sent to Queue!"));
            var resultBook = response.OutputData.Single(a => a.BindingName == "Book");
            Assert.That(resultBook.Data.Json, Is.EqualTo(sample1));
        }

        [Test]
        public async Task Sample_using_queue_trigger_with_Blob_input()
        {
            var book = new Book { id = "1", name = "How to test isolated functions" };

            string sample1 = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(book));
            var response = await _testServer.CallAsync("QueueTrigger", new Dictionary<string, object>
            {
                ["myQueueItem"] = book,
                ["myBlob"] = sample1
            });
            Assert.That(response.Result, Is.EqualTo(StatusResult.Success));
            Assert.That(response.ReturnValue.Json, Is.EqualTo(sample1));
        }
    }
}
