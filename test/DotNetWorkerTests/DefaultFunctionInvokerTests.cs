// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultFunctionInvokerTests
    {
        private readonly DefaultFunctionExecutor _executor;
        private readonly DefaultFunctionInvokerFactory _functionInvokerFactory;

        public DefaultFunctionInvokerTests()
        {
            _executor = new DefaultFunctionExecutor(NullLogger<DefaultFunctionExecutor>.Instance);

            var functionActivator = new DefaultFunctionActivator();
            var methodInvokerFactory = new DefaultMethodInvokerFactory();
            _functionInvokerFactory = new DefaultFunctionInvokerFactory(methodInvokerFactory, functionActivator);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithReturn()
        {
            MethodInfo mi = typeof(Functions).GetMethod(nameof(Functions.FunctionWithVoidReturn));

            var context = CreateContext(mi);

            await _executor.ExecuteAsync(context);

            Assert.Null(context.InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithVoidReturn()
        {
            MethodInfo mi = typeof(Functions).GetMethod(nameof(Functions.FunctionWithReturn));

            var context = CreateContext(mi);

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithTaskReturn()
        {

            MethodInfo mi = typeof(Functions).GetMethod(nameof(Functions.FunctionWithTaskReturn));

            var context = CreateContext(mi);

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithVoidTaskReturn()
        {
            MethodInfo mi = typeof(Functions).GetMethod(nameof(Functions.FunctionWithVoidTaskReturn));

            var context = CreateContext(mi);

            await _executor.ExecuteAsync(context);

            Assert.Null(context.InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_FunctionWithInputBindingAndReturn()
        {
            MethodInfo mi = typeof(Functions).GetMethod(nameof(Functions.FunctionWithInputBindingAndReturn));

            var context = CreateContext(mi);

            var converter = new List<IConverter>
            {
                new TypeConverter()
            };

            context.Features.Set<IModelBindingFeature>(new DefaultModelBindingFeature(converter));
            context.Invocation.ValueProvider = new TestValueProvider(new Dictionary<string, object>
            {
                {"bindingValue", "bindingValue" }
            });

            await _executor.ExecuteAsync(context);

            Assert.Equal("bindingValue", context.InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithReturn()
        {
            MethodInfo mi = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithVoidReturn));

            var context = CreateContext(mi);

            await _executor.ExecuteAsync(context);

            Assert.Null(context.InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithVoidReturn()
        {
            MethodInfo mi = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithReturn));

            var context = CreateContext(mi);

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithTaskReturn()
        {

            MethodInfo mi = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithTaskReturn));

            var context = CreateContext(mi);

            await _executor.ExecuteAsync(context);

            Assert.Equal("done", context.InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithVoidTaskReturn()
        {
            MethodInfo mi = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithVoidTaskReturn));

            var context = CreateContext(mi);

            await _executor.ExecuteAsync(context);

            Assert.Null(context.InvocationResult);
        }

        [Fact]
        public async Task InvokeAsync_StaticFunctionWithInputBindingAndReturn()
        {
            MethodInfo mi = typeof(StaticFunctions).GetMethod(nameof(StaticFunctions.StaticFunctionWithInputBindingAndReturn));

            var context = CreateContext(mi);

            var converter = new List<IConverter>
            {
                new TypeConverter()
            };

            context.Features.Set<IModelBindingFeature>(new DefaultModelBindingFeature(converter));
            context.Invocation.ValueProvider = new TestValueProvider(new Dictionary<string, object>
            {
                {"bindingValue", "bindingValue" }
            });

            await _executor.ExecuteAsync(context);

            Assert.Equal("bindingValue", context.InvocationResult);
        }

        private FunctionContext CreateContext(MethodInfo mi)
        {
            var context = new TestFunctionContext
            {
                Invocation = new TestFunctionInvocation
                {
                    FunctionId = "test",
                    InvocationId = "1234"
                }
            };

            var metadata = new TestFunctionMetadata();
            var parameters = mi.GetParameters().Select(p => new FunctionParameter(p.Name, p.ParameterType));

            context.FunctionDefinition = new DefaultFunctionDefinition(metadata, parameters, EmptyOutputBindingsInfo.Instance);
            context.FunctionDefinition.Items[DefaultFunctionDefinition.InvokerKey] = _functionInvokerFactory.Create(mi);

            return context;
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

            public string FunctionWithInputBindingAndReturn(string bindingValue)
            {
                return bindingValue;
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

            public static string StaticFunctionWithInputBindingAndReturn(string bindingValue)
            {
                return bindingValue;
            }
        }
    }
}
