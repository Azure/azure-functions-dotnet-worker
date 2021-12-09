// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Tests.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultFunctionInvokerTests
    {
        private IFunctionBindingsFeature _functionBindings = new TestFunctionBindingsFeature
        {
            InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "inputKey", "inputValue" } }),
            TriggerMetadata = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "triggerKey", "triggerValue" } })
        };

        private readonly DefaultFunctionExecutor _executor;
        private readonly DefaultFunctionInvokerFactory _functionInvokerFactory;
        private readonly Mock<IMethodInfoLocator> _mockLocator = new Mock<IMethodInfoLocator>(MockBehavior.Strict);

        private MethodInfo _methodInfoToReturn;

        public DefaultFunctionInvokerTests()
        {
            _mockLocator
                .Setup(m => m.GetMethod(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => _methodInfoToReturn);

            var functionActivator = new DefaultFunctionActivator();
            var methodInvokerFactory = new DefaultMethodInvokerFactory();
            _functionInvokerFactory = new DefaultFunctionInvokerFactory(methodInvokerFactory, functionActivator, _mockLocator.Object);

            _executor = new DefaultFunctionExecutor(_functionInvokerFactory, NullLogger<DefaultFunctionExecutor>.Instance);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithReturn()
        {
            var methodInfo = typeof(Functions).GetMethod(nameof(Functions.FunctionWithVoidReturn));
            var definition = TestUtility.CreateDefinition(methodInfo);

            var context = CreateContext(definition);

            await _executor.ExecuteAsync(context);

            Assert.Null(context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithVoidReturn()
        {
            var methodInfo = typeof(Functions).GetMethod(nameof(Functions.FunctionWithReturn));
            var definition = TestUtility.CreateDefinition(methodInfo);

            var context = CreateContext(definition);

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithTaskReturn()
        {
            var methodInfo = typeof(Functions).GetMethod(nameof(Functions.FunctionWithTaskReturn));
            var definition = TestUtility.CreateDefinition(methodInfo);

            var context = CreateContext(definition);

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithVoidTaskReturn()
        {
            var methodInfo = typeof(Functions).GetMethod(nameof(Functions.FunctionWithVoidTaskReturn));
            var definition = TestUtility.CreateDefinition(methodInfo);

            var context = CreateContext(definition);

            await _executor.ExecuteAsync(context);

            Assert.Null(context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithInputBindingAndReturn()
        {
            var methodInfo = typeof(Functions).GetMethod(nameof(Functions.FunctionWithInputBindingAndReturn));
            var definition = TestUtility.CreateDefinition(methodInfo);

            var context = CreateContext(definition, new TestFunctionInvocation());

            var converter = new List<IConverter>
            {
                new TypeConverter()
            };

            context.Features.Set<IModelBindingFeature>(new DefaultModelBindingFeature(converter));
            context.Features.Set<IFunctionBindingsFeature>(_functionBindings);

            await _executor.ExecuteAsync(context);

            Assert.Equal("inputValue", context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithReturn()
        {
            var methodInfo = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithVoidReturn));
            var definition = TestUtility.CreateDefinition(methodInfo);

            var context = CreateContext(definition);

            await _executor.ExecuteAsync(context);

            Assert.Null(context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithVoidReturn()
        {
            var methodInfo = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithReturn));
            var definition = TestUtility.CreateDefinition(methodInfo);

            var context = CreateContext(definition);

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithTaskReturn()
        {
            var methodInfo = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithTaskReturn));
            var definition = TestUtility.CreateDefinition(methodInfo);

            var context = CreateContext(definition);

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithVoidTaskReturn()
        {
            var methodInfo = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithVoidTaskReturn));
            var definition = TestUtility.CreateDefinition(methodInfo);

            var context = CreateContext(definition);

            await _executor.ExecuteAsync(context);

            Assert.Null(context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithInputBindingAndReturn()
        {
            var methodInfo = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithInputBindingAndReturn));
            var definition = TestUtility.CreateDefinition(methodInfo);

            var context = CreateContext(definition, new TestFunctionInvocation());

            context.Features.Set<IFunctionBindingsFeature>(_functionBindings);

            var converter = new List<IConverter>
            {
                new TypeConverter()
            };

            context.Features.Set<IModelBindingFeature>(new DefaultModelBindingFeature(converter));

            await _executor.ExecuteAsync(context);

            Assert.Equal("triggerValue", context.GetBindings().InvocationResult);
        }

        private static FunctionDefinition CreateDefinition(MethodInfo methodInfo)
        {
            return new TestFunctionDefinition(entryPoint: $"{methodInfo.DeclaringType.FullName}.{methodInfo.Name}");
        }

        private static FunctionContext CreateContext(FunctionDefinition definition = null, FunctionInvocation invocation = null)
        {
            invocation ??= new TestFunctionInvocation(id: "1234", functionId: "test");

            definition ??= new TestFunctionDefinition();

            return new TestFunctionContext(definition, invocation);
        }

        private class Functions
        {
            public void FunctionWithVoidReturn()
            {
            }

            public string FunctionWithReturn()
            {
                return "done";
            }

            public async Task FunctionWithVoidTaskReturn()
            {
                await Task.Delay(100);
            }

            public async Task<string> FunctionWithTaskReturn()
            {
                await Task.Delay(100);
                return "done";
            }

            public string FunctionWithInputBindingAndReturn(string inputKey)
            {
                return inputKey;
            }
        }

        private static class StaticFunctions
        {
            public static void StaticFunctionWithVoidReturn()
            {
            }

            public static string StaticFunctionWithReturn()
            {
                return "done";
            }

            public static async Task StaticFunctionWithVoidTaskReturn()
            {
                await Task.Delay(100);
            }

            public static async Task<string> StaticFunctionWithTaskReturn()
            {
                await Task.Delay(100);
                return "done";
            }

            public static string StaticFunctionWithInputBindingAndReturn(string triggerKey)
            {
                return triggerKey;
            }
        }
    }
}
