// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class NullInstanceFactoryTests
    {
        [Fact]
        public void Create_ReturnsNull()
        {
            // Arrange
            IFunctionActivator product = NullFunctionActivator.Instance;

            // Act
            object instance = product.CreateInstance<object>(new ServiceCollection().BuildServiceProvider());

            // Assert
            Assert.Null(instance);
        }
    }
}
