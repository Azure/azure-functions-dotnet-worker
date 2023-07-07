using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.BindingTypeNotSupported, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.BindingTypeNotSupported>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Sdk.Analyzers.Tests
{
    public class BindingTypeNotSupportedTests
    {

        [Fact (Skip = "Test depends on new release of Microsoft.Azure.Functions.Worker")]
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
                                        new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"))),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact (Skip = "Test depends on new release of Microsoft.Azure.Functions.Worker")]
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
                                        new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"))),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact (Skip = "Test depends on new release of Microsoft.Azure.Functions.Worker")]
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
                                        new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"))),
                TestCode = testCode
            };

            test.ExpectedDiagnostics.Add(Verify.Diagnostic().WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithSpan(29, 68, 29, 75).WithArguments("BinaryData", "TestTriggerAttribute"));

            await test.RunAsync();
        }

        /*  The following tests use real binding attributes from extensions.
            We don't currently have a real binding that has SupportedTargetType
            and has ConverterFallbackBehavior set to 'disallow'. */

        [Theory]
        [InlineData("ToDoItem")]
        [InlineData("CosmosClient")]
        [InlineData("Container")]
        [InlineData("BinaryData")]
        public async Task CosmosDBInputAttribute_DiagnosticsNotExpected(string supportedType)
        {
            string testCode = $@"
                using System;
                using System.Net;
                using Microsoft.Azure.Cosmos;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {{
                    public static class SomeFunction
                    {{
                        [Function(nameof(SomeFunction))]
                        public static void Run([HttpTrigger(AuthorizationLevel.Anonymous, ""get"")] HttpRequestData req,
                        [CosmosDBInput(""a"", ""b"")] {supportedType} client)
                        {{
                        }}
                    }}

                    public class ToDoItem
                    {{
                        public string Id {{ get; set; }}
                        public string Description {{ get; set; }}
                        public bool IsComplete {{ get; set; }}
                    }}
                }}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(
                                        ImmutableArray.Create(
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"),
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Http", "3.0.13"),
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.CosmosDB", "4.2.0-preview2")
                                        )),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Theory]
        [InlineData("string")]
        [InlineData("ToDoItem")]
        [InlineData("ServiceBusReceivedMessage")]
        public async Task ServiceBusTriggerAttribute_ValidSingleBindingType_DiagnosticsNotExpected(string supportedType)
        {
            string testCode = $@"
                using System;
                using Azure.Messaging.ServiceBus;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {{
                    public static class SomeFunction
                    {{
                        [Function(nameof(SomeFunction))]
                        public static void Run([ServiceBusTrigger(""queue"")] {supportedType} message)
                        {{
                        }}
                    }}

                    public class ToDoItem
                    {{
                        public string Id {{ get; set; }}
                        public string Description {{ get; set; }}
                        public bool IsComplete {{ get; set; }}
                    }}
                }}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(
                                        ImmutableArray.Create(
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"),
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.ServiceBus", "5.10.0-preview2")
                                        )),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Fact]
        public async Task ServiceBusTriggerAttribute_ValidCollectionBindingType_DiagnosticsNotExpected()
        {
            string testCode = @"
                using System;
                using Azure.Messaging.ServiceBus;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([ServiceBusTrigger(""queue"", IsBatched = true)] ServiceBusReceivedMessage[] messages)
                        {
                        }
                    }
                }";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(
                                        ImmutableArray.Create(
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"),
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.ServiceBus", "5.10.0-preview2")
                                        )),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }

        [Theory]
        [InlineData("TableClient")]
        [InlineData("TableEntity")]
        public async Task TableInputAttribute_ValidBindingTypes_DiagnosticsNotExpected(string supportedType)
        {
            string testCode = $@"
                using System;
                using Azure.Data.Tables;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;

                namespace FunctionApp
                {{
                    public static class SomeFunction
                    {{
                        [Function(nameof(SomeFunction))]
                        public static void Run([HttpTrigger(AuthorizationLevel.Anonymous, ""get"")] HttpRequestData req,
                        [TableInput(""input"")] {supportedType} message)
                        {{
                        }}
                    }}

                    public class ToDoItem
                    {{
                        public string Id {{ get; set; }}
                        public string Description {{ get; set; }}
                        public bool IsComplete {{ get; set; }}
                    }}
                }}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(
                                        ImmutableArray.Create(
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.15.0-preview1"),
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Http", "3.0.13"),
                                            new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Tables", "1.2.0-preview1")
                                        )),
                TestCode = testCode
            };

            // test.ExpectedDiagnostics is an empty collection.

            await test.RunAsync();
        }
    }
}
