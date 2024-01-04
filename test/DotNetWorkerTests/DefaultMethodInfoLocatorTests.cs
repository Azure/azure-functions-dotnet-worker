// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Invocation;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultMethodInfoLocatorTests
    {
        [Fact]
        public void DoesNotBreakOldBehaviorWhenCorrectlyUsed() 
        {
            var methodLocator = new DefaultMethodInfoLocator();
            var path = typeof(OldBehaviorClass).Assembly.Location;
            var classType = typeof(OldBehaviorClass);
            var methodInfo = methodLocator.GetMethod(path, $"{classType.FullName}.{nameof(OldBehaviorClass.Run)}");
            Assert.NotNull(methodInfo);
            Assert.Same(classType.GetMethod(nameof(OldBehaviorClass.Run)),methodInfo);
        }

        [Fact]
        public void NewBehaviorSelectsTheCorrectMethod()
        {
            var methodLocator = new DefaultMethodInfoLocator();
            var path = typeof(NewBehaviorClass).Assembly.Location;
            var classType = typeof(NewBehaviorClass);
            var correctMethod = classType.GetMethods().First(x => x.Name == nameof(NewBehaviorClass.Run) && x.GetCustomAttribute<FunctionAttribute>() != null);
            var methodInfo = methodLocator.GetMethod(path, $"{classType.FullName}.{nameof(NewBehaviorClass.Run)}");
            Assert.NotNull(methodInfo);
            Assert.Same(correctMethod, methodInfo);
        }

        [Fact]
        
        public void StillThrowsOnWrongEntryPoint()
        {
            var methodLocator = new DefaultMethodInfoLocator();
            var path = typeof(NewBehaviorClass).Assembly.Location;
            var classType = typeof(NewBehaviorClass);
            var hasThrown = false;
            try
            {
                var methodInfo = methodLocator.GetMethod(path, $"{classType.FullName}.{nameof(NewBehaviorClass.Run)}Wrong");
            }
            catch (InvalidOperationException ex)
            {
                hasThrown = true;
            }
            Assert.True( hasThrown );
        }

        private class OldBehaviorClass
        {
            public void Run()
            {

            }
        }

        private class NewBehaviorClass
        {
            [Function("EnterHere")]
            public void Run() => Run(true);
            public void Run(bool b) { }
        }
    }
}
