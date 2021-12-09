using Microsoft.Azure.Functions.Worker.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    public static class TestWorkerHostingExtensions
    {
        public static IHostBuilder UseFunctionsTestHost(this IHostBuilder hostBuilder) => hostBuilder.ConfigureServices(s => s.AddTestWorker());

        public static TestWorkerClient GetTestWorkerClient(this IHost host) => host.Services.GetRequiredService<TestWorkerClient>();
    }
}
