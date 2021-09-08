using Xunit;
using AnalizerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.AsyncVoidAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using AnalyzerVerifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.AsyncVoidAnalyzer>;
using CodeFixTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.AsyncVoidAnalyzer, Microsoft.Azure.Functions.Worker.Sdk.Analyzers.AsyncVoidCodeFixProvider, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using CodeFixVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.AsyncVoidAnalyzer, Microsoft.Azure.Functions.Worker.Sdk.Analyzers.AsyncVoidCodeFixProvider, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;
using System.Threading;

namespace Sdk.Analyzers.Tests
{
    public sealed class AsyncVoidAnalyzerTests
    {
        private const string DiagnosticId = "AZFW0002";

        [Fact]
        public async Task AnalyzerReportsErrorForAsyncVoid()
        {
            string inputCode = @"
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp4
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static async void Run([QueueTrigger(""myqueue-items"")] string myQueueItem, FunctionContext context)
        {
            var logger = context.GetLogger(nameof(Function1));
            logger.LogInformation(myQueueItem);
            await Task.Delay(100);
        }
    }
}";
            var test = new AnalizerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            var expectedDiagnosticResult = AnalyzerVerifier
                                                .Diagnostic(DiagnosticId)
                                                .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                                                .WithSpan(11, 34, 11, 37);  // 11th line, 34th character(Run method)

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);

            await test.RunAsync();
        }

        [Fact]
        public async Task AnalyzerDoesNotReportForAsyncTask()
        {
            string inputCode = @"
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp4
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static async Task Run([QueueTrigger(""myqueue-items"")] string myQueueItem, FunctionContext context)
        {
            var logger = context.GetLogger(nameof(Function1));
            logger.LogInformation(myQueueItem);
            await Task.Delay(100);
        }
    }
}";
            var test = new AnalizerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            await test.RunAsync();
        }
                
        [Fact]
        public async Task AnalyzerDoesNotReportForNonAsyncCode()
        {
            string inputCode = @"
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp4
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static void Run([QueueTrigger(""myqueue-items"")] string myQueueItem, FunctionContext context)
        {
            var logger = context.GetLogger(nameof(Function1));
            logger.LogInformation(myQueueItem);
        }
    }
}";
            var test = new AnalizerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task CodeFixerWorks()
        {
            string inputCode = @"
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp4
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static async void Run([QueueTrigger(""myqueue-items"")] string myQueueItem, FunctionContext context)
        {
            var logger = context.GetLogger(nameof(Function1));
            logger.LogInformation(myQueueItem);
            await Task.Delay(100);
        }
    }
}";

            string expectedFixedCode = @"
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp4
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static async Task Run([QueueTrigger(""myqueue-items"")] string myQueueItem, FunctionContext context)
        {
            var logger = context.GetLogger(nameof(Function1));
            logger.LogInformation(myQueueItem);
            await Task.Delay(100);
        }
    }
}";

            var expectedDiagnosticResult = CodeFixVerifier
                                            .Diagnostic(DiagnosticId)
                                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                                            .WithSpan(11, 34, 11, 37); // 11th line, 34th character(Run method)

            var test = new CodeFixTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode,
                FixedCode = expectedFixedCode
            };

            test.ExpectedDiagnostics.AddRange(new[] { expectedDiagnosticResult });
            await test.RunAsync(CancellationToken.None);
        }
                
        private static ReferenceAssemblies LoadRequiredDependencyAssemblies()
        {
            var referenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.Azure.WebJobs.Extensions", "4.0.1"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.1.0"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage", "4.0.4")));

            return referenceAssemblies;
        }
    }
}
