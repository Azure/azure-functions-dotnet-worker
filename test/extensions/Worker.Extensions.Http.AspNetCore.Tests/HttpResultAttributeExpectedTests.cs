using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.HttpResultAttributeExpectedAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.HttpResultAttributeExpectedAnalyzer>;
using CodeFixTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.HttpResultAttributeExpectedAnalyzer, Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.CodeFixForRegistrationInASPNetCoreIntegration, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using CodeFixVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.HttpResultAttributeExpectedAnalyzer, Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.CodeFixForRegistrationInASPNetCoreIntegration, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Tests
{
    public class HttpResultAttributeExpectedTests
    {
        private const string ExpectedAttribute = "HttpResultAttribute";

        [Fact]
        public async Task HttpResultAttribute_WhenUsingIActionResultAndMultiOutput_Expected()
        {
            string testCode = @"
            using System;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.Azure.Functions.Worker;

            namespace AspNetIntegration
            {
                public class MultipleOutputBindings
                {
                    [Function(""MultipleOutputBindings"")]
                    public MyOutputType Run([HttpTrigger(AuthorizationLevel.Function, ""post"")] HttpRequest req)
                    {
                        throw new NotImplementedException();
                    }
                    public class MyOutputType
                    {
                        public IActionResult Result { get; set; }

                        [BlobOutput(""test-samples-output/{name}-output.txt"")]
                        public string MessageText { get; set; }
                    }
                }
            }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verifier.Diagnostic()
                            .WithSeverity(DiagnosticSeverity.Error)
                            .WithLocation(12, 28)
                            .WithArguments("\"MultipleOutputBindings\""));

            await test.RunAsync();
        }

        [Fact]
        public async Task HttpResultAttribute_WhenUsingHttpRequestDataAndMultiOutput_NotExpected()
        {
                        string testCode = @"
            using System;
            using Microsoft.AspNetCore.Http;
            using Microsoft.Azure.Functions.Worker.Http;
            using Microsoft.Azure.Functions.Worker;

            namespace AspNetIntegration
            {
                public class MultipleOutputBindings
                {
                    [Function(""MultipleOutputBindings"")]
                    public MyOutputType Run([HttpTrigger(AuthorizationLevel.Function, ""post"")] HttpRequest req)
                    {
                        throw new NotImplementedException();
                    }
                    public class MyOutputType
                    {
                        public HttpResponseData Result { get; set; }

                        [BlobOutput(""test-samples-output/{name}-output.txt"")]
                        public string MessageText { get; set; }
                    }
                }
            }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task AspNetIntegration_WithIncorrectRegistration_Diagnostics_Expected_CodeFixWorks()
        {
            string inputCode = @"
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
                                .ConfigureFunctionsWorkerDefaults()
                                .Build();
                            host.Run();
                            //</docsnippet_aspnet_registration>
                        }
                        public static void Method1()
                        {
                        }
                        private static void Method2()
                        {
                        }
                    }
                }";

            string expectedCode = @"
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
                        public static void Method1()
                        {
                        }
                        private static void Method2()
                        {
                        }
                    }
                }";


            var expectedDiagnosticResult = CodeFixVerifier
                                .Diagnostic("AZFW0015")
                                .WithSeverity(DiagnosticSeverity.Error)
                                .WithSpan(16, 34, 16, 66)
                                .WithArguments(ExpectedAttribute);

            var test = new CodeFixTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode,
                FixedCode = expectedCode
            };

            test.ExpectedDiagnostics.AddRange(new[] { expectedDiagnosticResult });
            await test.RunAsync();
        }

        private static ReferenceAssemblies LoadRequiredDependencyAssemblies()
        {
            var referenceAssemblies = ReferenceAssemblies.Net.Net60.WithPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.21.0"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.17.2"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore", "1.2.1"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "5.0.0"),
                new PackageIdentity("Microsoft.AspNetCore.Mvc.Core", "2.2.5"),
                new PackageIdentity("Microsoft.AspNetCore.Http.Abstractions", "2.2.0"),
                new PackageIdentity("Microsoft.Extensions.Hosting.Abstractions", "6.0.0")));

            return referenceAssemblies;
        }
    }
}
