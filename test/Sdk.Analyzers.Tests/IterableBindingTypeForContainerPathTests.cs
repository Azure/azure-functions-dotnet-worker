using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.IterableBindingTypeForContainerPath, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.IterableBindingTypeForContainerPath>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;
using System;

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

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                            .WithSpan(10, 76, 10, 83)
                            .WithArguments("string"));

            await test.RunAsync();
        }

        [Fact]
        public async Task InputBindingClass3_IterableType_Diagnostics_NotExpected()
        {
            string testCode = @"
                using System;
                using System.IO;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([BlobInput(""input"")] Stream message)
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

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                            .WithSpan(11, 76, 11, 83)
                            .WithArguments("System.IO.Stream"));

            await test.RunAsync();
        }

        [Fact]
        public async Task InputBindingClass4_IterableType_Diagnostics_NotExpected()
        {
            string testCode = @"
                using System;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([BlobInput(""input"")] byte[] message)
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

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                            .WithSpan(10, 76, 10, 83)
                            .WithArguments("byte[]"));

            await test.RunAsync();
        }

        [Fact]
        public async Task InputBindingClass2_IterableType_Diagnostics_NotExpected()
        {
            string testCode = @"
                using System.IO;
                using Microsoft.Azure.WebJobs;
                using Microsoft.Extensions.Logging;
                using System.Net;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [FunctionName(""Function1"")]
                        public static void Run([Blob(""blob-container"", FileAccess.Read)] string blob)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Azure.Storage.Blobs", "12.14.1"),
                    new PackageIdentity("Microsoft.Azure.WebJobs.Extensions.Storage", "5.0.1"),
                    new PackageIdentity("Microsoft.NET.Sdk.Functions", "4.1.1"))),

                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                            .WithSpan(12, 97, 12, 101)
                            .WithArguments("string"));

            await test.RunAsync();
        }
    }
}
