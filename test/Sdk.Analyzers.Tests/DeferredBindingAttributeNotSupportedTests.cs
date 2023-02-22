using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.DeferredBindingAttributeNotSupported, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.DeferredBindingAttributeNotSupported>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Sdk.Analyzers.Tests
{
    public class DeferredBindingAttributeNotSupportedTests
    {
        [Fact]
        public async Task TriggerBindingClass_SupportsDeferredBindingAttribute_Diagnostics_NotExpected()
        {
            string testCode = @"
                using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

                namespace TestBindings
                {
                    [SupportsDeferredBinding]
                    public sealed class BlobTriggerAttribute : TriggerBindingAttribute
                    {
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.12.1-preview1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.9.0-preview1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.2.0-preview1"))),

                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task InputBindingClass_SupportsDeferredBindingAttribute_Diagnostics_NotExpected()
        {
            string testCode = @"
                using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

                namespace TestBindings
                {
                    [SupportsDeferredBinding]
                    public sealed class BlobInputAttribute : InputBindingAttribute
                    {
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.12.1-preview1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.9.0-preview1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.2.0-preview1"))),

                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task OutputBindingClass_SupportsDeferredBindingAttribute_Diagnostic_Expected()
        {
            string testCode = @"
                using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

                namespace TestBindings
                {
                    [SupportsDeferredBinding]
                    public sealed class BlobOutputAttribute : OutputBindingAttribute
                    {
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.12.1-preview1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.9.0-preview1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.2.0-preview1"))),

                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                                        .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                                        .WithSpan(6, 22, 6, 45)
                                        .WithArguments("SupportsDeferredBindingAttribute"));

            await test.RunAsync();
        }

        [Fact]
        public async Task ClassWithoutBase_SupportsDeferredBindingAttribute_Diagnostic_Expected()
        {
            string testCode = @"
                using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

                namespace TestBindings
                {
                    [SupportsDeferredBinding]
                    public sealed class JustAnotherClass
                    {
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.12.1-preview1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.9.0-preview1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.2.0-preview1"))),

                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                                        .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                                        .WithSpan(6, 22, 6, 45)
                                        .WithArguments("SupportsDeferredBindingAttribute"));

            await test.RunAsync();
        }
    }
}
