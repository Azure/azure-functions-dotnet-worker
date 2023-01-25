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
using Microsoft.Extensions.Azure;
using Azure.Storage.Blobs;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SampleIntegrationTests
{
    public class Tests
    {
        private IHost _host;
        private FunctionRpcTestServer _functionRpcTestServer;

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

                    services.AddAzureClients(a => a.AddBlobServiceClient("UseDevelopmentStorage=true"));

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

            _functionRpcTestServer = _host.Services.GetRequiredService<FunctionRpcTestServer>();
            await _functionRpcTestServer.Init();
        }

        [Test]
        public async Task Sample_using_http_trigger_with_CallByNameAsync()
        {
            var response = await _functionRpcTestServer.CallByNameAsync("HttpTriggerSimple", new Dictionary<string, object>
            {
                ["req"] = new FakeHttpRequest(new Uri("http://localhost:0/api/HttpTriggerSimple"))
            });
            Assert.That(response.Result, Is.EqualTo(StatusResult.Success));
            Assert.That(response.ReturnValue.Http.StatusCode, Is.EqualTo(StatusCodes.Status200OK.ToString()));
            Assert.That(response.ReturnValue.Http.Body.Bytes.ToStringUtf8(), Is.EqualTo("Hello world!"));
        }

        [Test]
        public async Task Sample_using_test_client_on_an_api_call()
        {
            var response = await _host.GetTestClient().GetAsync("/api/HttpTriggerSimple");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(body, Is.EqualTo("Hello world!"));
        }


        [Test]
        public async Task Sample_using_test_client_on_a_function_with_route_specific_uri()
        {
            var response = await _host.GetTestClient().GetAsync("/api/some/route");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(body, Is.EqualTo("Hello world! From HttpTriggerWithCustomRoute"));
        }

        [Test]
        public async Task Sample_using_test_client_on_api_with_blob_input()
        {
            var blobServiceClient = _host.Services.GetRequiredService<BlobServiceClient>();
            var client = blobServiceClient.GetBlobContainerClient("test-samples");

            var book = new Book { id = "1", name = "How to test isolated functions" };

            await client.DeleteIfExistsAsync();
            if (!(await client.ExistsAsync()).Value)
            {
                await client.CreateAsync();
            }

            if (!(await client.GetBlobClient("sample1.txt").ExistsAsync()).Value)
            {
                
                byte[] sample1 = JsonSerializer.SerializeToUtf8Bytes(book);
                await using MemoryStream stream = new MemoryStream(sample1);
                await client.GetBlobClient("sample1.txt").UploadAsync(stream);
            }
            var response = await _host.GetTestClient().GetAsync("/api/HttpTriggerWithBlobInput");
            var content = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), () => content);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(body, Is.EqualTo("Book Sent to Queue!"));
        }

    }
}
