using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.RegistrationInASPNetIntegration, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.RegistrationInASPNetIntegration>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Sdk.Analyzers.Tests
{
    public class RegistrationAspNetIntegrationTests
    {
        private const string ExpectedRegistrationMethod = "ConfigureFunctionsWebApplication";

        [Fact]
        public async Task BlobInputAttribute_String_Diagnostics_Expected()
        {
            string testCode = @"
                using System.Linq;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;
                using Microsoft.Extensions.Logging;

                namespace AspNetIntegration
                {
                    class Program
                    {
                        static void Main(string[] args)
                        {

                            //<docsnippet_aspnet_registration>
                            var host = new HostBuilder()
                                .ConfigureFunctionsWebApplication()
                                .Build();

                            host.Run();
                            //</docsnippet_aspnet_registration>
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.19.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.14.1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore", "1.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "5.0.0"),
                    new PackageIdentity("Microsoft.Extensions.Hosting.Abstractions", "6.0.0")
                    )),

                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                            .WithArguments(ExpectedRegistrationMethod));

            await test.RunAsync();
        }
    }
}
