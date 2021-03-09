// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class StartupHookTests
    {
        [Fact]
        public void ValidadeHookSetup()
        {
            var hookType = typeof(StartupHook);
            var initializeMethod = hookType.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public);

            Assert.Equal("StartupHook", hookType.Name);
            Assert.Equal("StartupHook", hookType.FullName);
            Assert.Null(hookType.Namespace);
            Assert.False(hookType.IsVisible);
            Assert.Equal("Microsoft.Azure.Functions.Worker.Core", hookType.Assembly.GetName().Name);
            Assert.NotNull(initializeMethod);
            Assert.Empty(initializeMethod.GetParameters());
            Assert.Equal(typeof(void), initializeMethod.ReturnType);
        }
    }
}
