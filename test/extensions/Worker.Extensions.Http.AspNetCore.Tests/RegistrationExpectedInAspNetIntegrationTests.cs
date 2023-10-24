using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.RegistrationExpectedInASPNetIntegration, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.RegistrationExpectedInASPNetIntegration>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Tests
{
    public class RegistrationExpectedInAspNetIntegrationTests
    {
        private const string ExpectedRegistrationMethod = "ConfigureFunctionsWebApplication";

        [Fact]
        public async Task AspNetIntegration_MissingRegistration_Diagnostics_Expected()
        {
            string testCode = @"
                namespace AspNetIntegration
                {
                    using System.Linq;
                    using System.Threading.Tasks;
                    using Microsoft.Azure.Functions.Worker;
                    using Microsoft.Extensions.DependencyInjection;
                    using Microsoft.Extensions.Hosting;
                    using Microsoft.Extensions.Logging;

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            var host = new HostBuilder()
                                .ConfigureFunctionsWorkerDefaults()
                                .Build();

                            host.Run();
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verifier.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                            .WithSpan(13, 37, 13, 41)
                            .WithArguments(ExpectedRegistrationMethod));

            await test.RunAsync();
        }


        [Fact]
        public async Task AspNetIntegration_CommentedRegistration_Diagnostics_NotExpected()
        {
            string testCode = @"
                namespace AspNetIntegration
                {
                    using System.Linq;
                    using System.Threading.Tasks;
                    using Microsoft.Azure.Functions.Worker;
                    using Microsoft.Extensions.DependencyInjection;
                    using Microsoft.Extensions.Hosting;
                    using Microsoft.Extensions.Logging;

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            var host = new HostBuilder()
                                //.ConfigureFunctionsWorkerDefaults()
                                //.ConfigureFunctionsWebApplication()
                                .Build();

                            host.Run();
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection because ConfigureFunctionsWorkerDefaults() is not present

            await test.RunAsync();
        }

        [Fact]
        public async Task AspNetIntegrationWithTrigger_MissingRegistration_Diagnostics_Expected()
        {
            string testCode = @"
                namespace AspNetIntegration
                {
                    using System.Linq;
                    using System.Threading.Tasks;
                    using Microsoft.Azure.Functions.Worker;
                    using Microsoft.Extensions.DependencyInjection;
                    using Microsoft.Extensions.Hosting;
                    using Microsoft.Extensions.Logging;

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            var host = new HostBuilder()
                                .ConfigureFunctionsWorkerDefaults()
                                .Build();

                            host.Run();
                        }
                    }
                }

                namespace AspNetIntegration
                {
                    using Microsoft.Azure.Functions.Worker;
                    using Microsoft.Azure.Functions.Worker.Http;

                    public class FunctionHttpTrigger
                    {
                        [Function(nameof(FunctionHttpTrigger))]
                        public void Run([HttpTrigger(AuthorizationLevel.Anonymous, ""post"")] HttpRequestData req)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verifier.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                            .WithSpan(13, 37, 13, 41)
                            .WithArguments(ExpectedRegistrationMethod));

            await test.RunAsync();
        }

        [Fact]
        public async Task AspNetIntegration_WithRegistration_Diagnostics_NotExpected()
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

                        public static void Method1()
                        {
                        }

                        private static void Method2()
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task AspNetIntegration_WithMiddleWare_WithRegistration_Diagnostics_NotExpected()
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
                            #if ENABLE_MIDDLEWARE
                                var host = new HostBuilder()
                                    .ConfigureFunctionsWebApplication(builder =>
                                    {
                                        // can still register middleware and use this extension method the same way
                                        // .ConfigureFunctionsWorkerDefaults() is used
                                        builder.UseWhen<RoutingMiddleware>((context)=>
                                        {
                                            // We want to use this middleware only for http trigger invocations.
                                            return context.FunctionDefinition.InputBindings.Values
                                                            .First(a => a.Type.EndsWith(""Trigger"")).Type == ""httpTrigger"";
                                        });
                                    })
                                    .Build();
                                host.Run();
                            #else
                                //<docsnippet_aspnet_registration>
                                var host = new HostBuilder()
                                    .ConfigureFunctionsWebApplication()
                                    .Build();

                                host.Run();
                                //</docsnippet_aspnet_registration>
                            #endif
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task AspNetIntegration_WithIncorrectRegistration_Diagnostics_Expected()
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

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verifier.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                            .WithSpan(13, 37, 13, 41)
                            .WithArguments(ExpectedRegistrationMethod));

            await test.RunAsync();
        }

        private static ReferenceAssemblies LoadRequiredDependencyAssemblies()
        {
            var referenceAssemblies = ReferenceAssemblies.Net.Net60.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.19.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.14.1"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore", "1.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "5.0.0"),
                    new PackageIdentity("Microsoft.Extensions.Hosting.Abstractions", "6.0.0")));

            return referenceAssemblies;
        }
    }
}
