using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.BindingTypeAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using AnalyzerVerifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.BindingTypeAnalyzer>;
using CodeFixTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.BindingTypeAnalyzer, Microsoft.Azure.Functions.Worker.Sdk.Analyzers.BindingTypeCodeFixProvider, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using CodeFixVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.BindingTypeAnalyzer, Microsoft.Azure.Functions.Worker.Sdk.Analyzers.BindingTypeCodeFixProvider, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Sdk.Analyzers.Tests
{
    public class BindingTypeCodeFixTests
    {
        [Fact]
        public async Task ReportsInfo_ForBindingsThat_AdvertiseTypes()
        {
            string testCode = @"
                using System;
                using Azure.Storage.Queues.Models;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([QueueTrigger(""input-queue"")] QueueMessage message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(
                                        ImmutableArray.Create(
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"),
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues", "5.1.0-dev638205624203226170-local")
                                        )),
                TestCode = testCode
            };

            var expectedDiagnosticResult = AnalyzerVerifier
                                .Diagnostic("AZFW0011")
                                .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                                .WithSpan(11, 91, 11, 98).WithArguments("QueueTriggerAttribute");

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);

            await test.RunAsync();
        }

        [Fact]
        public async Task DoesNotReport_ForBindingsThat_DoNotAdvertiseTypes()
        {
            string testCode = @"
                using System;
                using Azure.Storage.Blobs;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([BlobTrigger(""blob-trigger/a"")] BlobClient client)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(
                                        ImmutableArray.Create(
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"),
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "5.1.1-preview2")
                                        )),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Theory]
        [InlineData("string")]
        public async Task SuggestCodeFix_ForBindingsThat_AdvertiseTypes(string type)
        {
            string testCode = $@"
                using System;
                using Azure.Storage.Queues.Models;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {{
                    public static class SomeFunction
                    {{
                        [Function(nameof(SomeFunction))]
                        public static void Run([QueueTrigger(""input-queue"")] {type} message)
                        {{
                        }}
                    }}
                }}";

            string fixedCode = @"
                using System;
                using Azure.Storage.Queues.Models;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([QueueTrigger(""input-queue"")] QueueMessage message)
                        {
                        }
                    }
                }";

            var test = new CodeFixTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(
                                        ImmutableArray.Create(
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"),
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues", "5.1.0-dev638205624203226170-local")
                                        )),
                TestCode = testCode,
                FixedCode = fixedCode,
            };

            var expectedDiagnosticResult = CodeFixVerifier
                                .Diagnostic("AZFW0011")
                                .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                                .WithSpan(11, 85, 11, 92).WithArguments("QueueTriggerAttribute");

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);

            await test.RunAsync();
        }
    }
}
