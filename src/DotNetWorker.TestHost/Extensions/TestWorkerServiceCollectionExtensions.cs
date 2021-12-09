using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.TestHost;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TestWorkerServiceCollectionExtensions
    {
        public static IServiceCollection AddTestWorker(this IServiceCollection services)
        {
            return services
                .AddSingleton<IWorker, TestWorker>()
                .AddSingleton<IWorkerDiagnostics, TestWorkerDiagnostics>()
                .AddSingleton<TestFunctionMap>()
                .AddSingleton<TestFunctionDefinitionFactory>()
                .AddSingleton<TestWorkerClient, DefaultTestWorkerClient>();
        }
    }
}
