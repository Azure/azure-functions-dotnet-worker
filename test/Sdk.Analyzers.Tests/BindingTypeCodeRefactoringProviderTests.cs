// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
            ReferenceSource.FromAssembly(Assembly.Load("Microsoft.Azure.Functions.Worker.Core, Version=1.14.0.0").Location),
            ReferenceSource.FromAssembly(Assembly.Load("Microsoft.Azure.Functions.Worker.Extensions.Abstractions, Version=1.3.0.0")),

            ReferenceSource.FromAssembly(Assembly.Load("Azure.Storage.Queues, Version=12.13.1.0").Location),
            ReferenceSource.FromAssembly(Assembly.Load("Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues, Version=5.2.0.0").Location),

            ReferenceSource.FromAssembly(Assembly.Load("Azure.Messaging.EventHubs, Version=5.9.2.0").Location),
            ReferenceSource.FromAssembly(Assembly.Load("Microsoft.Azure.Functions.Worker.Extensions.EventHubs, Version=5.5.0.0").Location)
        };

        [Theory]
        [InlineData("string", 0)]
        [InlineData("bool", 1)]
        [InlineData("IEnumerable<string>", 2)]
        [InlineData("bool[]", 3)]
        public void TestBinding_SuggestsCodeRefactor(string supportedType, int index)
        {
            string testCode = @"
                using System;
                using System.Collections.Generic;
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
                    [SupportedTargetType(typeof(IEnumerable<string>))]
                    [SupportedTargetType(typeof(bool[]))]
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
                using System.Collections.Generic;
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
                    [SupportedTargetType(typeof(IEnumerable<string>))]
                    [SupportedTargetType(typeof(bool[]))]
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

        [Theory]
        [InlineData("QueueMessage", 0)]
        [InlineData("BinaryData", 1)]
        public void QueueTrigger_SuggestsCodeRefactor(string supportedType, int index)
        {
            string testCode = @"
                using System;
                using Azure.Storage.Queues.Models;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([QueueTrigger(""input"")] [|string message|])
                        {
                        }
                    }
                }";

            string expectedCode = $@"
                using System;
                using Azure.Storage.Queues.Models;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {{
                    public static class SomeFunction
                    {{
                        [Function(nameof(SomeFunction))]
                        public static void Run([QueueTrigger(""input"")] {supportedType} message)
                        {{
                        }}
                    }}
                }}";

            TestCodeRefactoring(testCode, expectedCode, index);
        }

        [Theory]
        [InlineData("EventData", 0)]
        [InlineData("EventData[]", 1)]
        public void EventHubTrigger_SuggestsCodeRefactor(string supportedType, int index)
        {
            string testCode = @"
                using System;
                using Azure.Messaging.EventHubs;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {
                    public static class SomeFunction
                    {
                        [Function(nameof(SomeFunction))]
                        public static void Run([EventHubTrigger(""input"")] [|string message|])
                        {
                        }
                    }
                }";

            string expectedCode = $@"
                using System;
                using Azure.Messaging.EventHubs;
                using Microsoft.Azure.Functions.Worker;

                namespace FunctionApp
                {{
                    public static class SomeFunction
                    {{
                        [Function(nameof(SomeFunction))]
                        public static void Run([EventHubTrigger(""input"")] {supportedType} message)
                        {{
                        }}
                    }}
                }}";

            TestCodeRefactoring(testCode, expectedCode, index);
        }
    }
}
