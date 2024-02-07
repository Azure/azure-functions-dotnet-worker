// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Invocation;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultMethodInvokerFactoryTests
    {
        private static bool _parameterlessMethodCalled;
        private readonly IMethodInvokerFactory _methodInvokerFactory = new DefaultMethodInvokerFactory();

        [Fact]
        public void Create_IfMethodIsNull_Throws()
        {
            // Arrange
            MethodInfo method = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _methodInvokerFactory.Create<DefaultMethodInvokerFactoryTests, object>(method));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Create_IfStaticMethodReturnsVoid_ReturnsVoidInvoker(bool isInstance)
        {
            // Arrange
            MethodInfo method = GetMethodInfo(isInstance, "ReturnVoid");

            // Act
            IMethodInvoker<DefaultMethodInvokerFactoryTests, object> invoker =
                _methodInvokerFactory.Create<DefaultMethodInvokerFactoryTests, object>(method);

            // Assert
            Assert.IsType<VoidMethodInvoker<DefaultMethodInvokerFactoryTests, object>>(invoker);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Create_IfStaticMethodReturnsTask_ReturnsVoidTaskInvoker(bool isInstance)
        {
            // Arrange
            MethodInfo method = GetMethodInfo(isInstance, "ReturnTask");

            // Act
            IMethodInvoker<DefaultMethodInvokerFactoryTests, object> invoker =
                _methodInvokerFactory.Create<DefaultMethodInvokerFactoryTests, object>(method);

            // Assert
            Assert.IsType<VoidTaskMethodInvoker<DefaultMethodInvokerFactoryTests, object>>(invoker);
        }

        [Theory]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.InstanceReturnVoid))]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.StaticReturnVoid))]
        public void Create_IfTReflectedIsNotReflectedType_Throws(string functionName)
        {
            // Arrange
            MethodInfo method = GetMethodInfo(functionName);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _methodInvokerFactory.Create<object, object>(method));
            Assert.Equal("The Type must match the method's ReflectedType.", ex.Message);
        }

        [Theory]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.InstanceTestIntStringObjectArray))]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.StaticTestIntStringObjectArray))]
        public async Task Create_IfMultipleInputParameters_PassesInputArguments(string functionName)
        {
            // Arrange
            MethodInfo method = GetMethodInfo(functionName);
            int expectedA = 1;
            string expectedB = "B";
            object[] expectedC = new object[] { new object() };

            // Act
            IMethodInvoker<DefaultMethodInvokerFactoryTests, object> invoker =
                _methodInvokerFactory.Create<DefaultMethodInvokerFactoryTests, object>(method);

            // Assert
            Assert.NotNull(invoker);
            bool callbackCalled = false;
            Action<int, string, object> callback = (a, b, c) =>
            {
                callbackCalled = true;
                Assert.Equal(expectedA, a);
                Assert.Same(expectedB, b);
                Assert.Same(expectedC, c);
            };
            DefaultMethodInvokerFactoryTests instance = GetInstance(!method.IsStatic);
            object[] arguments = new object[] { expectedA, expectedB, expectedC, callback };
            await invoker.InvokeAsync(instance, arguments);
            Assert.True(callbackCalled);
        }

        [Theory]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.InstanceTestOutIntStringObjectArray))]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.StaticTestOutIntStringObjectArray))]
        public async Task Create_IfMultipleOutputParameters_SetsOutputArguments(string functionName)
        {
            // Arrange
            MethodInfo method = GetMethodInfo(functionName);
            int expectedA = 1;
            string expectedB = "B";
            object[] expectedC = new object[] { new object() };

            // Act
            IMethodInvoker<DefaultMethodInvokerFactoryTests, object> invoker =
                _methodInvokerFactory.Create<DefaultMethodInvokerFactoryTests, object>(method);

            // Assert
            Assert.NotNull(invoker);
            bool callbackCalled = false;
            OutAction callback = delegate (out int a, out string b, out object[] c)
            {
                callbackCalled = true;
                a = expectedA;
                b = expectedB;
                c = expectedC;
            };
            DefaultMethodInvokerFactoryTests instance = GetInstance(!method.IsStatic);
            object[] arguments = new object[] { default(int), null, null, callback };
            await invoker.InvokeAsync(instance, arguments);
            Assert.True(callbackCalled);
            Assert.Equal(expectedA, arguments[0]);
            Assert.Same(expectedB, arguments[1]);
            Assert.Same(expectedC, arguments[2]);
        }

        [Theory]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.InstanceTestByRefIntStringObjectArray))]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.StaticTestByRefIntStringObjectArray))]
        public async Task Create_IfMultipleReferenceParameters_RoundTripsArguments(string functionName)
        {
            // Arrange
            MethodInfo method = GetMethodInfo(functionName);
            int expectedInitialA = 1;
            string expectedInitialB = "B";
            object[] expectedInitialC = new object[] { new object() };
            int expectedFinalA = 2;
            string expectedFinalB = "b";
            object[] expectedFinalC = new object[] { new object(), default(int), String.Empty };

            // Act
            IMethodInvoker<DefaultMethodInvokerFactoryTests, object> invoker =
                _methodInvokerFactory.Create<DefaultMethodInvokerFactoryTests, object>(method);

            // Assert
            Assert.NotNull(invoker);
            bool callbackCalled = false;
            ByRefAction callback = delegate (ref int a, ref string b, ref object[] c)
            {
                callbackCalled = true;
                Assert.Equal(expectedInitialA, a);
                Assert.Same(expectedInitialB, b);
                Assert.Same(expectedInitialC, c);
                a = expectedFinalA;
                b = expectedFinalB;
                c = expectedFinalC;
            };
            DefaultMethodInvokerFactoryTests instance = GetInstance(!method.IsStatic);
            object[] arguments = new object[] { expectedInitialA, expectedInitialB, expectedInitialC, callback };
            await invoker.InvokeAsync(instance, arguments);
            Assert.True(callbackCalled);
            Assert.Equal(expectedFinalA, arguments[0]);
            Assert.Same(expectedFinalB, arguments[1]);
            Assert.Same(expectedFinalC, arguments[2]);
        }

        [Theory]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.InstanceTestInOutByRefReturnTask))]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.StaticTestInOutByRefReturnTask))]
        public async Task Create_IfInOutByRefMethodReturnsTask_RoundTripsArguments(string functionName)
        {
            // Arrange
            MethodInfo method = GetMethodInfo(functionName);
            int expectedA = 1;
            string expectedInitialB = "B";
            string expectedFinalB = "b";
            object[] expectedC = new object[] { new object(), default(int), String.Empty };

            // Act
            IMethodInvoker<DefaultMethodInvokerFactoryTests, object> invoker =
                _methodInvokerFactory.Create<DefaultMethodInvokerFactoryTests, object>(method);

            // Assert
            Assert.NotNull(invoker);
            bool callbackCalled = false;
            InOutRefTaskFunc callback = delegate (int a, ref string b, out object[] c)
            {
                callbackCalled = true;
                Assert.Equal(expectedA, a);
                Assert.Same(expectedInitialB, b);
                b = expectedFinalB;
                c = expectedC;
                return Task.FromResult(0);
            };
            DefaultMethodInvokerFactoryTests instance = GetInstance(!method.IsStatic);
            object[] arguments = new object[] { expectedA, expectedInitialB, null, callback };
            await invoker.InvokeAsync(instance, arguments);
            Assert.True(callbackCalled);
            Assert.Same(expectedFinalB, arguments[1]);
            Assert.Same(expectedC, arguments[2]);
        }

        [Theory]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.InstanceReturnCanceledTask))]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.StaticReturnCanceledTask))]
        public void Create_IfReturnsTaskAndTaskCanceled_ReturnsCanceledTask(string functionName)
        {
            // Arrange
            MethodInfo method = GetMethodInfo(functionName);

            // Act
            IMethodInvoker<DefaultMethodInvokerFactoryTests, object> invoker =
                _methodInvokerFactory.Create<DefaultMethodInvokerFactoryTests, object>(method);

            // Assert
            DefaultMethodInvokerFactoryTests instance = GetInstance(!method.IsStatic);
            Task task = invoker.InvokeAsync(instance, null);
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        [Theory]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.InstanceParameterlessMethod))]
        [InlineData(nameof(DefaultMethodInvokerFactoryTests.StaticParameterlessMethod))]
        public async Task Create_IfParameterlessMethod_CanInvoke(string functionName)
        {
            // Arrange
            MethodInfo method = GetMethodInfo(functionName);

            // Act
            IMethodInvoker<DefaultMethodInvokerFactoryTests, object> invoker =
                _methodInvokerFactory.Create<DefaultMethodInvokerFactoryTests, object>(method);

            try
            {
                // Assert
                DefaultMethodInvokerFactoryTests instance = GetInstance(!method.IsStatic);
                await invoker.InvokeAsync(instance, null);
                Assert.True(_parameterlessMethodCalled);
            }
            finally
            {
                _parameterlessMethodCalled = false;
            }
        }

        private DefaultMethodInvokerFactoryTests GetInstance(bool isInstance)
        {
            return isInstance ? this : null;
        }

        private static MethodInfo GetMethodInfo(bool isInstance, string name)
        {
            return GetMethodInfo(GetPrefixedMethodName(isInstance, name));
        }

        private static MethodInfo GetMethodInfo(string name)
        {
            return typeof(DefaultMethodInvokerFactoryTests).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.Instance);
        }

        private static string GetPrefixedMethodName(bool isInstance, string name)
        {
            string prefix = isInstance ? "Instance" : "Static";
            return prefix + name;
        }

        private static int ReturnInt() { return default(int); }

        private static void StaticReturnVoid() { }

        private static Task StaticReturnTask() { return null; }

        private void InstanceReturnVoid() { }

        private Task InstanceReturnTask() { return null; }

        private static void StaticTestIntStringObjectArray(int a, string b, object[] c,
            Action<int, string, object[]> callback)
        {
            callback.Invoke(a, b, c);
        }

        private void InstanceTestIntStringObjectArray(int a, string b, object[] c,
            Action<int, string, object[]> callback)
        {
            callback.Invoke(a, b, c);
        }

        private delegate void OutAction(out int a, out string b, out object[] c);

        private static void StaticTestOutIntStringObjectArray(out int a, out string b, out object[] c,
            OutAction callback)
        {
            callback.Invoke(out a, out b, out c);
        }

        private void InstanceTestOutIntStringObjectArray(out int a, out string b, out object[] c, OutAction callback)
        {
            callback.Invoke(out a, out b, out c);
        }

        private delegate void ByRefAction(ref int a, ref string b, ref object[] c);

        private static void StaticTestByRefIntStringObjectArray(ref int a, ref string b, ref object[] c,
            ByRefAction callback)
        {
            callback.Invoke(ref a, ref b, ref c);
        }

        private void InstanceTestByRefIntStringObjectArray(ref int a, ref string b, ref object[] c,
            ByRefAction callback)
        {
            callback.Invoke(ref a, ref b, ref c);
        }

        private static Task StaticReturnCanceledTask()
        {
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();
            source.SetCanceled();
            return source.Task;
        }

        private Task InstanceReturnCanceledTask()
        {
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();
            source.SetCanceled();
            return source.Task;
        }

        private static void StaticParameterlessMethod()
        {
            _parameterlessMethodCalled = true;
        }

        private void InstanceParameterlessMethod()
        {
            _parameterlessMethodCalled = true;
        }

        private delegate Task InOutRefTaskFunc(int a, ref string b, out object[] c);

        private static Task StaticTestInOutByRefReturnTask(int a, ref string b, out object[] c,
            InOutRefTaskFunc callback)
        {
            return callback.Invoke(a, ref b, out c);
        }

        private Task InstanceTestInOutByRefReturnTask(int a, ref string b, out object[] c, InOutRefTaskFunc callback)
        {
            return callback.Invoke(a, ref b, out c);
        }
    }

}
