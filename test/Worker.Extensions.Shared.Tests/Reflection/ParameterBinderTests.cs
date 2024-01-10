// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.ObjectModel;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Shared.Tests
{
    public class ParameterBinderTests
    {
        [Fact]
        public Task BindCollection_ThrowsOnNullPageableFactory()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                () => ParameterBinder.BindCollectionAsync(null!, typeof(IEnumerable<string>)));
        }

        [Fact]
        public Task BindCollection_ThrowsOnNullCollectionType()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                () => ParameterBinder.BindCollectionAsync(e => Mock.Of<IAsyncEnumerable<object>>(), null!));
        }

        [Theory]
        [InlineData(typeof(object))] // basic non-collection
        [InlineData(typeof(MyPoco))] // basic non-collection
        [InlineData(typeof(string))] // special case
        [InlineData(typeof(byte[]))] // special case
        [InlineData(typeof(IEnumerable))] // do not support non-generic IEnumerable
        [InlineData(typeof(ICollection))] // do not support non-generic ICollection
        [InlineData(typeof(IList))] // do not support non-generic IList
        [InlineData(typeof(ICustomList<object>))] // do not support custom derived IList
        [InlineData(typeof(AbstractCollection))] // do not support abstract IEnumerable
        [InlineData(typeof(NonGenericCollection))] // do not support non-generic IEnumerable
        public Task BindCollection_ThrowsOnUnsupportedType(Type type)
        {
            return Assert.ThrowsAsync<ArgumentException>(
                () => ParameterBinder.BindCollectionAsync(e => Mock.Of<IAsyncEnumerable<object>>(), type));
        }

        [Fact]
        public async Task BindCollection_Interface_AllListInterfacesSupported()
        {
            var interfaceTypes = new List<int>().GetType().GetInterfaces().Where(t => t.IsGenericType);

            foreach(Type interfaceType in interfaceTypes)
            {
                Assert.IsType<List<int>>(await ParameterBinder.BindCollectionAsync(GetIntEnumerable, interfaceType));
            }
        }

        [Theory]
        [InlineData(typeof(IEnumerable<int>))]
        [InlineData(typeof(ICollection<int>))]
        [InlineData(typeof(IList<int>))]
        [InlineData(typeof(IReadOnlyList<int>))]
        [InlineData(typeof(IReadOnlyCollection<int>))]
        public async Task BindCollection_Interface_GetsList(Type type)
        {
            object collection = await ParameterBinder.BindCollectionAsync(GetIntEnumerable, type);
            List<int> list = Assert.IsType<List<int>>(collection);
            Assert.Collection(
                list,
                i => Assert.Equal(0, i),
                i => Assert.Equal(1, i),
                i => Assert.Equal(2, i),
                i => Assert.Equal(3, i),
                i => Assert.Equal(4, i),
                i => Assert.Equal(5, i));
        }

        [Theory]
        [InlineData(typeof(MyEnumerable))]
        [InlineData(typeof(Collection<int>))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(int[]))]
        public async Task BindCollection_Concrete_GetsType(Type type)
        {
            object collection = await ParameterBinder.BindCollectionAsync(GetIntEnumerable, type);
            Assert.IsType(type, collection);
            Assert.Collection(
                (IEnumerable<int>)collection,
                i => Assert.Equal(0, i),
                i => Assert.Equal(1, i),
                i => Assert.Equal(2, i),
                i => Assert.Equal(3, i),
                i => Assert.Equal(4, i),
                i => Assert.Equal(5, i));
        }

        [Fact]
        public async Task BindCollection_PocoArray_GetsArray()
        {
            object collection = await ParameterBinder.BindCollectionAsync(GetPocoEnumerable, typeof(MyPoco[]));
            Assert.IsType<MyPoco[]>(collection);
            Assert.Collection(
                (MyPoco[])collection,
                i => Assert.Equal("0", i.Prop),
                i => Assert.Equal("1", i.Prop),
                i => Assert.Equal("2", i.Prop),
                i => Assert.Equal("3", i.Prop),
                i => Assert.Equal("4", i.Prop),
                i => Assert.Equal("5", i.Prop));
        }

        private static async IAsyncEnumerable<object> GetIntEnumerable(Type t)
        {
            Assert.Equal(typeof(int), t);
            await Task.Yield();
            foreach (int i in Enumerable.Range(0, 6))
            {
                yield return i;
            }
        }

        private static async IAsyncEnumerable<object> GetPocoEnumerable(Type t)
        {
            Assert.Equal(typeof(MyPoco), t);
            await Task.Yield();
            foreach (int i in Enumerable.Range(0, 6))
            {
                yield return new MyPoco(i.ToString());
            }
        }

        private interface ICustomList<T> : IList<T>
        {
        }

        private record MyPoco(string Prop);

        private abstract class AbstractCollection : IEnumerable<MyPoco>
        {
            public IEnumerator<MyPoco> GetEnumerator() => throw new NotImplementedException();

            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }

        private class NonGenericCollection : IEnumerable
        {
            public IEnumerator GetEnumerator() => throw new NotImplementedException();
        }

        private class MyEnumerable : IEnumerable<int>
        {
            private readonly List<int> values = new List<int>();

            public void Add(int item) => values.Add(item);

            public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)values).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)values).GetEnumerator();
        }
    }
}
