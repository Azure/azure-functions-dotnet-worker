// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Shared.Tests
{
    public class TypeExtensionsTests
    {
        [Theory]
        [InlineData(typeof(string[]), true)]
        [InlineData(typeof(List<string>), true)]
        [InlineData(typeof(IEnumerable<string>), true)]
        [InlineData(typeof(MyList), true)]
        [InlineData(typeof(MyLayeredList), true)]
        [InlineData(typeof(MyEnumerable), true)]
        [InlineData(typeof(MyGenericList), true)]
        [InlineData(typeof(string), false)]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(bool), false)]
        public void IsCollectionType(Type type, bool expectedResult)
        {
            // Act
            bool result = type.IsCollectionType();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(typeof(string[]), typeof(string))]
        [InlineData(typeof(List<string>), typeof(string))]
        [InlineData(typeof(IEnumerable<string>), typeof(string))]
        [InlineData(typeof(MyList), typeof(string))]
        [InlineData(typeof(MyLayeredList), typeof(string))]
        [InlineData(typeof(MyEnumerable), typeof(string))]
        [InlineData(typeof(MyGenericList), typeof(string))]
        public void TryGetCollectionElementType_ReturnsElementType(Type type, Type expectedElementType)
        {
            // Act
            var result = type.TryGetCollectionElementType(out var elementType);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedElementType, elementType);
        }

        public class MyList : List<string>
        {
        }

        public class MyLayeredList : Layer2
        {
        }

        public class Layer2 : List<string>
        {
        }

        public class MyEnumerable : IEnumerable<string>
        {
            public IEnumerator<string> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        public interface IGeneric<T>
        {
        }

        public class MyGenericList : IGeneric<int>, IEnumerable<string>
        {
            public IEnumerator<string> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}
