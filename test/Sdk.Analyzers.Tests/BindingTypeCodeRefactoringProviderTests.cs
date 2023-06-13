
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
            ReferenceSource.FromAssembly(Assembly.Load("Microsoft.Azure.Functions.Worker.Extensions.Abstractions, Version=1.2.0.0")),

            ReferenceSource.FromAssembly(Assembly.Load("Azure.Data.Tables, Version=12.8.0.0").Location),
            ReferenceSource.FromAssembly(Assembly.Load("Microsoft.Azure.Functions.Worker.Extensions.Tables, Version=1.2.0.0").Location),

            ReferenceSource.FromAssembly(Assembly.Load("Azure.Messaging.ServiceBus, Version=7.14.0.0").Location),
            ReferenceSource.FromAssembly(Assembly.Load("Microsoft.Azure.Functions.Worker.Extensions.ServiceBus, Version=5.10.0.0").Location),
        };

        [Theory]
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

        [Theory]
        [InlineData("ServiceBusReceivedMessage", 0)]
        [InlineData("ServiceBusReceivedMessage[]", 1)]
        public void ServiceBusTrigger_SuggestsCodeRefactor(string supportedType, int index)
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
                        public static void Run([ServiceBusTrigger(""input"")] [|string message|])
                        {
                        }
                    }
                }";

            string expectedCode = $@"
                using System;
                using Azure.Messaging.ServiceBus;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {{
                    public static class SomeFunction
                    {{
                        [Function(nameof(SomeFunction))]
                        public static void Run([ServiceBusTrigger(""input"")] {supportedType} message)
                        {{
                        }}
                    }}
                }}";

            TestCodeRefactoring(testCode, expectedCode, index);
        }
    }
}
