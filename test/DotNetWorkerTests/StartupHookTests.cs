// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class StartupHookTests
    {
        [Fact]
        public void ValidateHookSetup()
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

        [Theory]
        [InlineData(null)]
        [InlineData("Microsoft.Azure.Functions.Worker.Core")]
        [InlineData("Some.Other.Assembly")]
        [InlineData("Microsoft.Azure.Functions.Worker.Core", "Some.Other.Assembly")]
        [InlineData("Some.Other.Assembly", "Microsoft.Azure.Functions.Worker.Core")]
        [InlineData("Some.Other.Assembly", "Microsoft.Azure.Functions.Worker.Core", "Another.Assembly")]
        public void ValidateHookSetup_HookVariableRemoved(params string[] hooks)
        {
            hooks ??= Array.Empty<string>();
            char separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
            string envVar = "DOTNET_STARTUP_HOOKS";
            string original = Environment.GetEnvironmentVariable(envVar);

            try
            {
                IEnumerable<string> expected = hooks.Where(
                    x => x != typeof(StartupHook).Assembly.GetName().Name);
                Environment.SetEnvironmentVariable(envVar, string.Join(separator, hooks));
                StartupHook.RemoveSelfFromStartupHooks();

                string value = Environment.GetEnvironmentVariable(envVar);
                IEnumerable<string> actual = string.IsNullOrEmpty(value)
                    ? Enumerable.Empty<string>() : value.Split(separator);
                Assert.Equal(expected, actual);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVar, original);
            }
        }
    }
}
