// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Core;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class ExceptionExtensionTests
    {
        [Theory]
        [ClassData(typeof(ExceptionTestData))]
        public void IsFatal_ReturnsTrueForFatalExceptions(Exception exception, bool isFatal)
        {
            var result = exception.IsFatal();
            
            Assert.Equal(result, isFatal);
        }

        public class ExceptionTestData : TheoryData<Exception, bool>
        {
            public ExceptionTestData()
            {
                Add(new OutOfMemoryException(), true);
                Add(new AppDomainUnloadedException(), true);
                Add(new BadImageFormatException(), true);
                Add(new CannotUnloadAppDomainException(), true);
                Add(new InvalidProgramException(), true);
                Add(new AccessViolationException(), true);
                Add(new InsufficientMemoryException(), false);
                Add(new Exception("test", new OutOfMemoryException()), true);
            }
        }
    }
}
