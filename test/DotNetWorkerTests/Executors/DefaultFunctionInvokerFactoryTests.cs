// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Invocation;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultFunctionInvokerFactoryTests
    {

        [Fact]
        public void Create_IfMethodIsNull_Throws()
        {
            // Arrange
            MethodInfo method = null;
            IMethodInvokerFactory methodInvokerFactory = CreateMethodInvokerFactory();
            IFunctionActivator activator = CreateDummyActivator();

            IFunctionInvokerFactory _invokerFactory = new DefaultFunctionInvokerFactory(methodInvokerFactory, activator);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => _invokerFactory.Create(method));
            Assert.Equal("method", ex.ParamName);
        }

        [Fact]
        public void Create_ReturnsFunctionInvoker()
        {
            // Arrange
            MethodInfo method = GetMethodInfo(nameof(DefaultFunctionInvokerFactoryTests.StaticReturnVoid));

            IMethodInvokerFactory methodInvokerFactory = CreateMethodInvokerFactory();
            IFunctionActivator activator = CreateDummyActivator();

            IFunctionInvokerFactory _invokerFactory = new DefaultFunctionInvokerFactory(methodInvokerFactory, activator);

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(method);

            // Assert
            Assert.IsType<DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>>(invoker);
        }

        [Fact]
        public void Create_IfStatic_UsesNullInstanceFactory()
        {
            // Arrange
            MethodInfo method = GetMethodInfo(nameof(DefaultFunctionInvokerFactoryTests.StaticReturnVoid));

            IMethodInvokerFactory methodInvokerFactory = CreateMethodInvokerFactory();
            IFunctionActivator activator = new DefaultFunctionActivator();

            IFunctionInvokerFactory _invokerFactory = new DefaultFunctionInvokerFactory(methodInvokerFactory, activator);

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(method);

            // Assert
            Assert.IsType<DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>>(invoker);

            var typedInvoker = (DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>)invoker;
            Assert.IsType<NullFunctionActivator>(typedInvoker.FunctionActivator);
        }

        [Fact]
        public void Create_IfInstance_UsesActivatorInstanceFactory()
        {
            // Arrange
            MethodInfo method = GetMethodInfo(nameof(DefaultFunctionInvokerFactoryTests.InstanceReturnVoid));

            IMethodInvokerFactory methodInvokerFactory = CreateMethodInvokerFactory();
            IFunctionActivator activator = new DefaultFunctionActivator();

            IFunctionInvokerFactory _invokerFactory = new DefaultFunctionInvokerFactory(methodInvokerFactory, activator);

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(method);

            // Assert
            Assert.IsType<DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>>(invoker);
            var typedInvoker = (DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>)invoker;
            Assert.IsType<DefaultFunctionActivator>(typedInvoker.FunctionActivator);
        }

        [Fact]
        public void Create_IfInstanceAndMethodIsInherited_UsesReflectedType()
        {
            // Arrange
            MethodInfo method = GetMethodInfo(typeof(Subclass), nameof(Subclass.InheritedReturnVoid));

            IMethodInvokerFactory methodInvokerFactory = CreateMethodInvokerFactory();
            IFunctionActivator activator = new DefaultFunctionActivator();

            IFunctionInvokerFactory _invokerFactory = new DefaultFunctionInvokerFactory(methodInvokerFactory, activator);

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(method);

            // Assert
            Assert.IsType<DefaultFunctionInvoker<Subclass, object>>(invoker);
            var typedInvoker = (DefaultFunctionInvoker<Subclass, object>)invoker;
            Assert.IsType<DefaultFunctionActivator>(typedInvoker.FunctionActivator);
        }

        private static IFunctionActivator CreateDummyActivator()
        {
            return new Mock<IFunctionActivator>(MockBehavior.Strict).Object;
        }

        private static IMethodInvokerFactory CreateMethodInvokerFactory()
        {
            return new DefaultMethodInvokerFactory();
        }

        private static MethodInfo GetMethodInfo(string name)
        {
            return GetMethodInfo(typeof(DefaultFunctionInvokerFactoryTests), name);
        }

        private static MethodInfo GetMethodInfo(Type type, string name)
        {
            return type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        }

        private static void StaticReturnVoid() { }

        private void InstanceReturnVoid() { }

        protected void InheritedReturnVoid() { }

        private class Subclass : DefaultFunctionInvokerFactoryTests
        {
        }
    }
}
