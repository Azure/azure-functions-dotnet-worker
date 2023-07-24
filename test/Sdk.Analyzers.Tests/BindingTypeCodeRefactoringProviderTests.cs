
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Sdk.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using RoslynTestKit;
using Xunit;

namespace Sdk.Analyzers.Tests
{
    public class BindingTypeCodeRefactoringProviderTests : CodeRefactoringTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override CodeRefactoringProvider CreateProvider()
        {
            return new BindingTypeCodeRefactoringProvider();
        }

        protected override IReadOnlyCollection<MetadataReference> References => new[]
        {
            ReferenceSource.NetStandard2_0,
            ReferenceSource.FromAssembly(Assembly.Load("System.Runtime, Version=7.0.0.0").Location),
            ReferenceSource.FromAssembly(Assembly.Load("Microsoft.Azure.Functions.Worker.Core, Version=1.13.0.0").Location),
            ReferenceSource.FromAssembly(Assembly.Load("Microsoft.Azure.Functions.Worker.Extensions.Abstractions, Version=1.3.0.0")),

            // Uncomment when reenabling tables test
            // ReferenceSource.FromAssembly(Assembly.Load("Azure.Data.Tables, Version=12.8.0.0").Location),
            // ReferenceSource.FromAssembly(Assembly.Load("Microsoft.Azure.Functions.Worker.Extensions.Tables, Version=1.2.0.0").Location)
        };

        [Theory]
        [InlineData("string", 0)]
        [InlineData("bool", 1)]
        public void TestBinding_SuggestsCodeRefactor(string supportedType, int index)
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
                        public void Run([TestTrigger()] [|int message|])
                        {
                        }
                    }
                }";

            string expectedCode = $@"
                using System;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Core;
                using Microsoft.Azure.Functions.Worker.Converters;
                using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

                namespace Microsoft.Azure.Functions.Worker
                {{
                    [ConverterFallbackBehavior(ConverterFallbackBehavior.Disallow)]
                    [InputConverter(typeof(TestConverter))]
                    public class TestTriggerAttribute : TriggerBindingAttribute
                    {{
                    }}

                    [SupportsDeferredBinding]
                    [SupportedTargetType(typeof(string))]
                    [SupportedTargetType(typeof(bool))]
                    public class TestConverter
                    {{
                    }}
                }}

                namespace FunctionApp
                {{
                    public class SomeFunction
                    {{
                        [Function(nameof(SomeFunction))]
                        public void Run([TestTrigger()] {supportedType} message)
                        {{
                        }}
                    }}
                }}";

            TestCodeRefactoring(testCode, expectedCode, index);
        }

        [Theory (Skip="Pending Tables release")]
        [InlineData("TableClient", 0)]
        [InlineData("TableEntity", 1)]
        public void TableInput_SuggestsCodeRefactor(string supportedType, int index)
        {
            string testCode = @"
                using System;
                using Azure.Data.Tables;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([TableInput(""input"")] [|string message|])
                        {
                        }
                    }
                }";

            string expectedCode = $@"
                using System;
                using Azure.Data.Tables;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {{
                    public static class SomeFunction
                    {{
                        [Function(nameof(SomeFunction))]
                        public static void Run([TableInput(""input"")] {supportedType} message)
                        {{
                        }}
                    }}
                }}";

            TestCodeRefactoring(testCode, expectedCode, index);
        }
    }
}
