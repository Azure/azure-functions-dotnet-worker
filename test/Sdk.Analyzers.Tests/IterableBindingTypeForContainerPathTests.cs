using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.IterableBindingTypeForContainerPath, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.IterableBindingTypeForContainerPath>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Sdk.Analyzers.Tests
{
    public class IterableBindingTypeForContainerPathTests
    {
        [Fact]
        public async Task InputBindingClass_IterableType_Diagnostics_NotExpected()
        {
            string testCode = @"
                using System;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([BlobInput(""input"")] string message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.9.0-preview1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "5.1.1-preview2"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.2.0-preview1"))),

                TestCode = testCode
            };

           // test.ExpectedDiagnostics.Clear();

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                           // .WithSpan(10, 48, 10, 67)
                            .WithSpan(10, 76, 10, 83)
                            .WithArguments("string"));

            await test.RunAsync();
        }
    }
}
