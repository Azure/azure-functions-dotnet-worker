using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.LocalSettingsJsonNotAllowedAsConfiguration, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.LocalSettingsJsonNotAllowedAsConfiguration, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sdk.Analyzers.Tests;

public class LocalSettingsJsonNotValidConfigurationTests
{
    [Fact]
    public async Task LocalSettingsJsonPassedToConfigurationIssuesWarning()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70.WithPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.23.0"))),
            
            TestCode = """
               using Microsoft.Extensions.Hosting;
               using Microsoft.Extensions.Configuration;

               public static class Program
               {
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
               }
               """,
            
            ExpectedDiagnostics = {
                Verify.Diagnostic()
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithSpan(12, 36, 12, 57)
            }
        }.RunAsync();
    }
}
