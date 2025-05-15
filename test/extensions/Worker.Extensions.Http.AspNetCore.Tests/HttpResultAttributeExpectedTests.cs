// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.HttpResultAttributeExpectedAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.HttpResultAttributeExpectedAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using CodeFixTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.HttpResultAttributeExpectedAnalyzer, Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.CodeFixForHttpResultAttribute, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using CodeFixVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.HttpResultAttributeExpectedAnalyzer, Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.CodeFixForHttpResultAttribute, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Tests
{
    public class HttpResultAttributeExpectedTests
    {
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

            test.ExpectedDiagnostics.Add(Verifier.Diagnostic(DiagnosticDescriptors.MultipleOutputHttpTriggerWithoutHttpResultAttribute)
                            .WithSeverity(DiagnosticSeverity.Error)
                            .WithLocation(12, 28)
                            .WithArguments("\"MultipleOutputBindings\""));

            await test.RunAsync();
        }

        [Fact]
        public async Task HttpResultAttributeUsedCorrectly_NoDiagnostic()
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
                        [HttpResult]
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

            await test.RunAsync();
        }

        [Fact]
        public async Task SimpleHttpTrigger_NoDiagnostic()
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
                    [Function(""SimpleHttpTrigger"")]
                    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, ""post"")] HttpRequest req)
                    {
                        throw new NotImplementedException();
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
        public async Task PocoUsedWithoutOutputBindings_NoDiagnostic()
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
                    [Function(""PocoOutput"")]
                    public MyOutputType Run([HttpTrigger(AuthorizationLevel.Function, ""post"")] HttpRequest req)
                    {
                        throw new NotImplementedException();
                    }
                    public class MyOutputType
                    {
                        public string Name { get; set; }

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
        public async Task HttpResultAttributeWarning_WhenUsingHttpResponseDataAndMultiOutput_Expected()
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

            test.ExpectedDiagnostics.Add(Verifier.Diagnostic(DiagnosticDescriptors.MultipleOutputWithHttpResponseDataWithoutHttpResultAttribute)
                            .WithSeverity(DiagnosticSeverity.Warning)
                            .WithLocation(12, 28)
                            .WithArguments("\"MultipleOutputBindings\""));

            await test.RunAsync();
        }

        [Fact]
        public async Task HttpResultAttributeExpected_CodeFixWorks()
        {
            string inputCode = @"
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

            string expectedCode = @"
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
            [HttpResult]
            public IActionResult Result { get; set; }

            [BlobOutput(""test-samples-output/{name}-output.txt"")]
            public string MessageText { get; set; }
        }
    }
}";


            var expectedDiagnosticResult = CodeFixVerifier
                                .Diagnostic("AZFW0015")
                                .WithSeverity(DiagnosticSeverity.Error)
                                .WithLocation(12, 16)
                                .WithArguments("\"MultipleOutputBindings\"");

            var test = new CodeFixTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode,
                FixedCode = expectedCode
            };

            test.ExpectedDiagnostics.AddRange(new[] { expectedDiagnosticResult });
            await test.RunAsync();
        }

        [Fact]
        public async Task HttpResultAttributeForHttpResponseDataExpected_CodeFixWorks()
        {
            string inputCode = @"
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AspNetIntegration
{
    public class MultipleOutputBindings
    {
        [Function(""MultipleOutputBindings"")]
        public MyOutputType Run([HttpTrigger(AuthorizationLevel.Function, ""post"")] HttpRequestData req)
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

            string expectedCode = @"
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AspNetIntegration
{
    public class MultipleOutputBindings
    {
        [Function(""MultipleOutputBindings"")]
        public MyOutputType Run([HttpTrigger(AuthorizationLevel.Function, ""post"")] HttpRequestData req)
        {
            throw new NotImplementedException();
        }
        public class MyOutputType
        {
            [HttpResult]
            public HttpResponseData Result { get; set; }

            [BlobOutput(""test-samples-output/{name}-output.txt"")]
            public string MessageText { get; set; }
        }
    }
}";


            var expectedDiagnosticResult = CodeFixVerifier
                                .Diagnostic("AZFW0016")
                                .WithSeverity(DiagnosticSeverity.Warning)
                                .WithLocation(13, 16)
                                .WithArguments("\"MultipleOutputBindings\"");

            var test = new CodeFixTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode,
                FixedCode = expectedCode
            };

            test.ExpectedDiagnostics.AddRange(new[] { expectedDiagnosticResult });
            await test.RunAsync();
        }

        [Fact]
        public async Task HttpResultAttribute_WhenReturnTypeIsWrappedInTask_Expected()
        {
            string testCode = @"
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;

    namespace AspNetIntegration
    {
        public class MultipleOutputBindings
        {
            [Function(""TaskOfPocoOutput"")]
            public Task<MyOutputType> Run([HttpTrigger(AuthorizationLevel.Function, ""post"")] HttpRequestData req)
            {
                throw new System.NotImplementedException();
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

            test.ExpectedDiagnostics.Add(Verifier.Diagnostic(DiagnosticDescriptors.MultipleOutputWithHttpResponseDataWithoutHttpResultAttribute)
                            .WithSeverity(DiagnosticSeverity.Warning)
                            .WithLocation(12, 20)
                            .WithArguments("\"TaskOfPocoOutput\""));

            await test.RunAsync();
        }

        [Fact]
        public async Task HttpResultAttributeForTaskOfPocoExpected_CodeFixWorks()
        {
            string inputCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AspNetIntegration
{
    public class MultipleOutputBindings
    {
        [Function(""TaskOfPocoOutput"")]
        public Task<MyOutputType> Run([HttpTrigger(AuthorizationLevel.Function, ""post"")] HttpRequestData req)
        {
            throw new System.NotImplementedException();
        }
        public class MyOutputType
        {
            public HttpResponseData Result { get; set; }

            [BlobOutput(""test-samples-output/{name}-output.txt"")]
            public string MessageText { get; set; }
        }
    }
}";

            string expectedCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AspNetIntegration
{
    public class MultipleOutputBindings
    {
        [Function(""TaskOfPocoOutput"")]
        public Task<MyOutputType> Run([HttpTrigger(AuthorizationLevel.Function, ""post"")] HttpRequestData req)
        {
            throw new System.NotImplementedException();
        }
        public class MyOutputType
        {
            [HttpResult]
            public HttpResponseData Result { get; set; }

            [BlobOutput(""test-samples-output/{name}-output.txt"")]
            public string MessageText { get; set; }
        }
    }
}";

            var expectedDiagnosticResult = CodeFixVerifier
                                .Diagnostic("AZFW0016")
                                .WithSeverity(DiagnosticSeverity.Warning)
                                .WithLocation(12, 16)
                                .WithArguments("\"TaskOfPocoOutput\"");

            var test = new CodeFixTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode,
                FixedCode = expectedCode
            };

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);
            await test.RunAsync();
        }

        private static ReferenceAssemblies LoadRequiredDependencyAssemblies()
        {
            var referenceAssemblies = ReferenceAssemblies.Net.Net60.WithPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.22.0"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.17.4"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore", "1.3.2"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "5.0.0"),
                new PackageIdentity("Microsoft.AspNetCore.Mvc.Core", "2.2.5"),
                new PackageIdentity("Microsoft.Extensions.Hosting.Abstractions", "6.0.0"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Http", "3.2.0")));

            return referenceAssemblies;
        }
    }
}
