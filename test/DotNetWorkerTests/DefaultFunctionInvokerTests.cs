// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultFunctionInvokerTests
    {
        private readonly DefaultFunctionExecutor _executor;
        private readonly Channel<StreamingMessage> _channel;
        private readonly DefaultFunctionInvokerFactory _functionInvokerFactory;

        public DefaultFunctionInvokerTests()
        {
            _executor = new DefaultFunctionExecutor();

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

        private FunctionExecutionContext CreateContext(MethodInfo mi)
        {
            var context = new TestFunctionExecutionContext();
            var metadata = new FunctionMetadata();
            var parameters = mi.GetParameters().Select(p => new FunctionParameter(p.Name, p.ParameterType));

            context.FunctionDefinition = new DefaultFunctionDefinition(metadata, _functionInvokerFactory.Create(mi), parameters);

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
        }
    }
}
