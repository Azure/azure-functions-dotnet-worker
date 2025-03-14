// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Tests.Shared;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultFunctionInvokerFactoryTests
    {
        private readonly IMethodInvokerFactory _methodInvokerFactory;
        private readonly DefaultFunctionActivator _activator;
        private readonly DefaultFunctionInvokerFactory _invokerFactory;

        private MethodInfo _functionMethod;

        public DefaultFunctionInvokerFactoryTests()
        {
            Mock<IMethodInfoLocator> invokerMock = new Mock<IMethodInfoLocator>(MockBehavior.Strict);
            invokerMock
                .Setup(p => p.GetMethod(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => _functionMethod);

            _methodInvokerFactory = CreateMethodInvokerFactory();
            _activator = new DefaultFunctionActivator();
            _invokerFactory = new DefaultFunctionInvokerFactory(_methodInvokerFactory, _activator, invokerMock.Object);
        }

        [Fact]
        public void Create_ReturnsFunctionInvoker()
        {
            // Arrange
            _functionMethod = GetMethodInfo(nameof(DefaultFunctionInvokerFactoryTests.StaticReturnVoid));

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(new TestFunctionDefinition());

            // Assert
            Assert.IsType<DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>>(invoker);
        }

        [Fact]
        public void Create_IfStatic_UsesNullInstanceFactory()
        {
            // Arrange
            _functionMethod = GetMethodInfo(nameof(DefaultFunctionInvokerFactoryTests.StaticReturnVoid));

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(new TestFunctionDefinition());

            // Assert
            Assert.IsType<DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>>(invoker);

            var typedInvoker = (DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>)invoker;
            Assert.IsType<NullFunctionActivator>(typedInvoker.FunctionActivator);
        }

        [Fact]
        public void Create_IfInstance_UsesActivatorInstanceFactory()
        {
            // Arrange
            _functionMethod = GetMethodInfo(nameof(DefaultFunctionInvokerFactoryTests.InstanceReturnVoid));

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(new TestFunctionDefinition());

            // Assert
            Assert.IsType<DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>>(invoker);
            var typedInvoker = (DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>)invoker;
            Assert.IsType<DefaultFunctionActivator>(typedInvoker.FunctionActivator);
        }

        [Fact]
        public void Create_IfInstanceAndMethodIsInherited_UsesReflectedType()
        {
            // Arrange
            _functionMethod = GetMethodInfo(typeof(Subclass), nameof(Subclass.InheritedReturnVoid));

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(new TestFunctionDefinition());

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
