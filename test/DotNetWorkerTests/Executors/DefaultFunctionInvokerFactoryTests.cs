// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Invocation;
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
        private static readonly string _pathToAssembly = typeof(DefaultFunctionInvokerFactoryTests).Assembly.Location;

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
            var methodInfo = GetMethodInfo(nameof(DefaultFunctionInvokerFactoryTests.StaticReturnVoid));
            var definition = TestUtility.CreateDefinition(methodInfo);

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(definition);

            // Assert
            Assert.IsType<DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>>(invoker);
        }

        [Fact]
        public void Create_IfStatic_UsesNullInstanceFactory()
        {
            // Arrange
            var methodInfo = GetMethodInfo(nameof(DefaultFunctionInvokerFactoryTests.StaticReturnVoid));
            var definition = TestUtility.CreateDefinition(methodInfo);

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(definition);

            // Assert
            Assert.IsType<DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>>(invoker);

            var typedInvoker = (DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>)invoker;
            Assert.IsType<NullFunctionActivator>(typedInvoker.FunctionActivator);
        }

        [Fact]
        public void Create_IfInstance_UsesActivatorInstanceFactory()
        {
            // Arrange
            var methodInfo = GetMethodInfo(nameof(DefaultFunctionInvokerFactoryTests.InstanceReturnVoid));
            var definition = TestUtility.CreateDefinition(methodInfo);

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(definition);

            // Assert
            Assert.IsType<DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>>(invoker);
            var typedInvoker = (DefaultFunctionInvoker<DefaultFunctionInvokerFactoryTests, object>)invoker;
            Assert.IsType<DefaultFunctionActivator>(typedInvoker.FunctionActivator);
        }

        [Fact]
        public void Create_IfInstanceAndMethodIsInherited_UsesReflectedType()
        {
            // Arrange
            var methodInfo = GetMethodInfo(typeof(Subclass), nameof(Subclass.InheritedReturnVoid));
            var definition = TestUtility.CreateDefinition(methodInfo);

            // Act
            IFunctionInvoker invoker = _invokerFactory.Create(definition);

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

        public class Subclass : DefaultFunctionInvokerFactoryTests
        {
        }
    }
}
