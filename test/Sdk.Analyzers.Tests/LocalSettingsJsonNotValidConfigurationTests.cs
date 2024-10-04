using System.Threading.Tasks;

namespace Sdk.Analyzers.Tests;

public class LocalSettingsJsonNotValidConfigurationTests
{
    public async Task LocalSettingsJsonPassedToConfigurationIssuesWarning()
    {
        const string code = """
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("local.settings.json", optional: true);
                })
                .Build();
        
            host.Run();
        }
        """;
        
        
        
        
    }
}
