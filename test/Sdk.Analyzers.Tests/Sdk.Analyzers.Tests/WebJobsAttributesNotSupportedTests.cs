using Xunit;
using AnalizerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.WebJobsAttributesNotSupported, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.WebJobsAttributesNotSupported>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Sdk.Analyzers.Tests
{
    public class WebJobsAttributesNotSupportedTests
    {
        [Fact]
        public async Task Test1()
        {
            string testCode = @"
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs;

namespace FunctionApp
{
    public static class SomeFunction
    {
        [Function(nameof(SomeFunction))]
        public static void Run([HttpTrigger(AuthorizationLevel.Anonymous, ""get"")] HttpRequestData req, [TimerTrigger(""b"")] string test, FunctionContext context)
        {
        }
    }
}";
            var test = new AnalizerTest();
            // TODO: This needs to pull from a local source
            test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.Azure.WebJobs.Extensions", "4.0.1"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.0.0-preview5"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.0.1-preview5"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.0.0-preview5"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Http", "3.0.12-preview1")));

            test.TestCode = testCode;

            test.ExpectedDiagnostics.Add(Verify.Diagnostic().WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithSpan(12, 105, 12, 122).WithArguments("TimerTriggerAttribute"));
            
            await test.RunAsync();
        }
    }
}
