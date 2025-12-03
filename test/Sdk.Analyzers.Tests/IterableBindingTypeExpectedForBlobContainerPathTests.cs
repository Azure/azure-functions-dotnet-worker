// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.IterableBindingTypeExpectedForBlobContainerPath, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.IterableBindingTypeExpectedForBlobContainerPath, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Sdk.Analyzers.Tests
{
    public class IterableBindingTypeExpectedForBlobContainerPathTests
    {
        [Fact]
        public async Task BlobInputAttribute_String_Diagnostics_Expected()
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
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.13.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.3.0"))),

                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                            .WithSpan(10, 76, 10, 83)
                            .WithArguments("string"));

            await test.RunAsync();
        }

        [Fact]
        public async Task BlobInputAttribute_Stream_Diagnostics_Expected()
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
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.13.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.3.0"))),

                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                            .WithSpan(11, 76, 11, 83)
                            .WithArguments("System.IO.Stream"));

            await test.RunAsync();
        }

        [Fact]
        public async Task BlobInputAttribute_ByteArray_Diagnostics_Expected()
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
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.13.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.3.0"))),

                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic()
                            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                            .WithSpan(10, 76, 10, 83)
                            .WithArguments("byte[]"));

            await test.RunAsync();
        }


        [Fact]
        public async Task BlobInputAttribute_StringArray_Diagnostics_NotExpected()
        {
            string testCode = @"
                using System;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([BlobInput(""input"")] string[] message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.13.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.3.0"))),

                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task BlobInputAttribute_StreamEnumerable_Diagnostics_NotExpected()
        {
            string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.IO;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([BlobInput(""input"")] IEnumerable<Stream> message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.13.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.3.0"))),

                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task BlobInputAttribute_StringEnumerable_Diagnostics_NotExpected()
        {
            string testCode = @"
                using System;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([BlobInput(""input"")] IEnumerable<string> message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.13.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.3.0"))),

                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task BlobInputAttribute_ArrayOfByteArray_Diagnostics_NotExpected()
        {
            string testCode = @"
                using System;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([BlobInput(""input"")] byte[][] message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.13.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.3.0"))),

                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task BlobInputAttribute_BlobContainerClient_Diagnostics_NotExpected()
        {
            string testCode = @"
                using System;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Azure.Storage.Blobs;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([BlobInput(""input"")] BlobContainerClient message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.13.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.3.0"))),

                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task BlobInputAttribute_BlobPathExpression_Diagnostics_NotExpected()
        {
            string testCode = @"
                using System;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Azure.Storage.Blobs;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([BlobInput(""{input}"")] string message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Sdk", "1.13.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs", "6.0.0"),
                    new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Abstractions", "1.3.0"))),

                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }
    }
}
