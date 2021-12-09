using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FunctionApp;
using Microsoft.Azure.Functions.Worker.TestHost;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace TestProject
{
    public class TestWorkerSampleTests
    {
        [Fact]
        public async Task Invoke()
        {
            var builder = Program.CreateHostBuilder()
                .UseFunctionsTestHost();

            using (var host = builder.Build())
            {
                await host.StartAsync();

                var client = host.GetTestWorkerClient();

                var reqBuilder = HttpRequestDataBuilder
                    .Create(HttpMethod.Post, new Uri("http://test/api/abc"))
                    .WithBody("Some payload")
                    .AddHeader("one", "two");

                var context = client.CreateContext()
                    .WithHttpTrigger("req", reqBuilder)
                    .WithInputData("myBlob", JsonSerializer.Serialize(new { name = "abc", id = "def" }));

                InvocationResult result = await client.InvokeAsync("Function1", context);
                var response = result.GetResponseData();
            }
        }
    }
}
