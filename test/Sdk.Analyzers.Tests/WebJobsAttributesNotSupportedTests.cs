using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.WebJobsAttributesNotSupported, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.WebJobsAttributesNotSupported>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Sdk.Analyzers.Tests
{
    public class WebJobsAttributesNotSupportedTests
    {
        [Fact]
        public async Task WebJobsAttributeInParameters()
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
            var test = new AnalyzerTest
            {
                // TODO: This needs to pull from a local source
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.WebJobs.Extensions", "4.0.1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.10.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.7.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.1.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Http", "3.0.12"))),

                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic().WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithSpan(12, 105, 12, 122).WithArguments("TimerTriggerAttribute"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WebJobsAttributeInReturnType()
        {
            string testCode = @"
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public static class Function1
    {
        [Function(""Function1"")]
        [return: Microsoft.Azure.WebJobs.Queue(""dest-q"")]
        public static string Run([TimerTrigger(""0 */1 * * * *"")] MyInfo myTimer)
        {
            return ""Azure"";
        }
    }

    public record MyInfo(bool IsPastDue);
}";
            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.10.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.7.0"),
                    new PackageIdentity("Microsoft.Azure.WebJobs.Extensions.Storage", "5.0.1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Timer", "4.0.1"))),

                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic().WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithSpan(9, 18, 9, 57).WithArguments("QueueAttribute"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WebJobsAttributeInReturnTypeAndParameters()
        {
            string testCode = @"
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using System.IO;

namespace FunctionApp
{
    public static class Function1
    {
        [Function(""Function1"")]
        [return: Microsoft.Azure.WebJobs.Queue(""dest-q"")]
        public static string Run([TimerTrigger(""0 */1 * * * *"")] object myTimer,
                                 [Blob(""samples-workitems/{queueTrigger}"", FileAccess.Read)] Stream myBlob)
        {
            return ""Azure"";
        }
    }
}";
            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.10.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.7.0"),
                    new PackageIdentity("Microsoft.Azure.WebJobs.Extensions.Storage", "5.0.1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Timer", "4.0.1"))),

                TestCode = testCode
            };

            // Diagnostic entry for functions parameter
            test.ExpectedDiagnostics.Add(Verify.Diagnostic().WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithSpan(13, 35, 13, 92).WithArguments("BlobAttribute"));
            // Diagnostic entry for functions return type
            test.ExpectedDiagnostics.Add(Verify.Diagnostic().WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithSpan(11, 18, 11, 57).WithArguments("QueueAttribute"));

            await test.RunAsync();
        }

        [Fact]
        public async Task NoWebJobsAttribute()
        {
            string testCode = @"
using Microsoft.Azure.Functions.Worker;

namespace GH258IsolatedReturnWebJobsAttr
{
    public class PureIsolatedTimerFunction
    {
        [Function(nameof(PureIsolatedTimerFunction))]
        public void Run([TimerTrigger(""0 */5 * * * *"")] object myTimer)
        {
        }
    }
}";
            var test = new AnalyzerTest
            {
                // TODO: This needs to pull from a local source
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.10.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.7.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Timer", "4.0.1"))),

                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }
    }
}
