// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.BindingTypeNotSupported, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.BindingTypeNotSupported, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Sdk.Analyzers.Tests
{
    public class BindingTypeNotSupportedTests
    {
        [Fact]
        public async Task TestAttribute_ValidBindingType_DiagnosticsNotExpected()
        {
            string testCode = @"
                using System;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core;
                using Microsoft.Azure.Functions.Worker.Converters;
                using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

                namespace Microsoft.Azure.Functions.Worker
                {
                    [ConverterFallbackBehavior(ConverterFallbackBehavior.Disallow)]
                    [InputConverter(typeof(TestConverter))]
                    public class TestTriggerAttribute : TriggerBindingAttribute
                    {
                    }

                    [SupportsDeferredBinding]
                    [SupportedTargetType(typeof(string))]
                    [SupportedTargetType(typeof(bool))]
                    public class TestConverter
                    {
                    }
                }

                namespace FunctionApp
                {
                    public class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public void Run([TestTrigger()] string message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                                        new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"))),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task TestAttribute_ValidBindingType_WithoutDeferredBinding_DiagnosticsNotExpected()
        {
            string testCode = @"
                using System;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core;
                using Microsoft.Azure.Functions.Worker.Converters;
                using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

                namespace Microsoft.Azure.Functions.Worker
                {
                    [ConverterFallbackBehavior(ConverterFallbackBehavior.Disallow)]
                    [InputConverter(typeof(TestConverter))]
                    public class TestTriggerAttribute : TriggerBindingAttribute
                    {
                    }

                    [SupportedTargetType(typeof(string))]
                    [SupportedTargetType(typeof(bool))]
                    public class TestConverter
                    {
                    }
                }

                namespace FunctionApp
                {
                    public class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public void Run([TestTrigger()] string message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                                        new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"))),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task TestAttribute_InvalidBindingType_DiagnosticsExpected()
        {
            string testCode = @"
                using System;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core;
                using Microsoft.Azure.Functions.Worker.Converters;
                using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

                namespace Microsoft.Azure.Functions.Worker
                {
                    [ConverterFallbackBehavior(ConverterFallbackBehavior.Disallow)]
                    [InputConverter(typeof(TestConverter))]
                    public class TestTriggerAttribute : TriggerBindingAttribute
                    {
                    }

                    [SupportsDeferredBinding]
                    [SupportedTargetType(typeof(string))]
                    [SupportedTargetType(typeof(bool))]
                    public class TestConverter
                    {
                    }
                }

                namespace FunctionApp
                {
                    public class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public void Run([TestTrigger()] BinaryData message)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                                        new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.18.0"))),
                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic().WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithSpan(29, 68, 29, 75).WithArguments("BinaryData", "TestTriggerAttribute"));

            await test.RunAsync();
        }
    }
}
