// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly Mock<IInputConversionFeature> _mockInputConversionFeature = new(MockBehavior.Strict);
        private readonly Mock<IConverterContextFactory> _mockConvertContextFactory = new(MockBehavior.Strict);
        private MethodInfo _methodInfoToReturn;

        public DefaultFunctionInvokerTests()
        {
            _mockLocator
                .Setup(m => m.GetMethod(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => _methodInfoToReturn);

            var functionContext = new TestFunctionContext(new TestFunctionDefinition(), invocation: null);
            _mockConvertContextFactory
                .Setup(m=> m.Create(It.IsAny<Type>(), It.IsAny<object>(), It.IsAny<FunctionContext>(),It.IsAny<IReadOnlyDictionary<string, object>>()))
                .Returns(new DefaultConverterContext(typeof(string),"foo", functionContext, ImmutableDictionary<string, object>.Empty));

            var functionActivator = new DefaultFunctionActivator();
            var methodInvokerFactory = new DefaultMethodInvokerFactory();
            _functionInvokerFactory = new DefaultFunctionInvokerFactory(methodInvokerFactory, functionActivator, _mockLocator.Object);

            _executor = new DefaultFunctionExecutor(_functionInvokerFactory, NullLogger<DefaultFunctionExecutor>.Instance);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithReturn()
        {
            _methodInfoToReturn = typeof(Functions).GetMethod(nameof(Functions.FunctionWithVoidReturn));
            var context = CreateContext();

            await _executor.ExecuteAsync(context);

            Assert.Null(context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithVoidReturn()
        {
            _methodInfoToReturn = typeof(Functions).GetMethod(nameof(Functions.FunctionWithReturn));
            var context = CreateContext();

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithTaskReturn()
        {
            _methodInfoToReturn = typeof(Functions).GetMethod(nameof(Functions.FunctionWithTaskReturn));
            var context = CreateContext();

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithVoidTaskReturn()
        {
            _methodInfoToReturn = typeof(Functions).GetMethod(nameof(Functions.FunctionWithVoidTaskReturn));
            var context = CreateContext();

            await _executor.ExecuteAsync(context);

            Assert.Null(context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithInputBindingAndReturn()
        {
            SetupMockResultForInputConverter("foo");
            _methodInfoToReturn = typeof(Functions).GetMethod(nameof(Functions.FunctionWithInputBindingAndReturn));

            var context = CreateContext(invocation: new TestFunctionInvocation());
            var converter = new List<IInputConverter>
            {
                new TypeConverter()
            };
            context.Features.Set<IInputConversionFeature>(_mockInputConversionFeature.Object);
            context.Features.Set<IModelBindingFeature>(new DefaultModelBindingFeature(_mockConvertContextFactory.Object));
            context.Features.Set<IFunctionBindingsFeature>(_functionBindings);

            await _executor.ExecuteAsync(context);

            Assert.Equal("foo", context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithReturn()
        {
            _methodInfoToReturn = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithVoidReturn));
            var context = CreateContext();

            await _executor.ExecuteAsync(context);

            Assert.Null(context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithVoidReturn()
        {
            _methodInfoToReturn = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithReturn));
            var context = CreateContext();

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithTaskReturn()
        {
            _methodInfoToReturn = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithTaskReturn));
            var context = CreateContext();

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithVoidTaskReturn()
        {
            _methodInfoToReturn = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithVoidTaskReturn));
            var context = CreateContext();

            await _executor.ExecuteAsync(context);

            Assert.Null(context.GetBindings().InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithInputBindingAndReturn()
        {
            SetupMockResultForInputConverter("triggerValue");
            _methodInfoToReturn = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithInputBindingAndReturn));

            var context = CreateContext(invocation: new TestFunctionInvocation());
            context.Features.Set<IFunctionBindingsFeature>(_functionBindings);
            context.Features.Set<IInputConversionFeature>(_mockInputConversionFeature.Object);
            context.Features.Set<IModelBindingFeature>(new DefaultModelBindingFeature(_mockConvertContextFactory.Object));

            await _executor.ExecuteAsync(context);

            Assert.Equal("triggerValue", context.GetBindings().InvocationResult);
        }

        private void SetupMockResultForInputConverter(string mockOutputValue)
        {
            _mockInputConversionFeature
                .Setup(m => m.ConvertAsync(It.IsAny<ConverterContext>()))
                .Returns(new ValueTask<ConversionResult>(ConversionResult.Success(mockOutputValue)));
        }

        private FunctionContext CreateContext(FunctionDefinition definition = null, FunctionInvocation invocation = null)
        {
            invocation = invocation ?? new TestFunctionInvocation(id: "1234", functionId: "test");

            // We're controlling the method via the IMethodInfoLocator, so the strings here don't matter.
            var parameters = _mockLocator.Object.GetMethod(string.Empty, string.Empty)
                .GetParameters()
                .Select(p => new FunctionParameter(p.Name, p.ParameterType));

            definition = definition ?? new TestFunctionDefinition(parameters: parameters);

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
