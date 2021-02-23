// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Collections;
using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class EnumerableConverterTests
    {
        private const string _sourceString = "hello";
        private static readonly byte[] _sourceBytes = Encoding.UTF8.GetBytes(_sourceString);
        private static readonly ReadOnlyMemory<byte> _sourceMemory = new ReadOnlyMemory<byte>(_sourceBytes);
        private EnumerableConverter _converter = new EnumerableConverter();

        private static readonly IEnumerable<byte[]> _sourceMemoryEnumerable = new RepeatedField<byte[]>() { _sourceBytes };
        private static readonly RepeatedField<string> _sourceStringEnumerable = new RepeatedField<string>() { _sourceString };
        private static readonly RepeatedField<double> _sourceDoubleEnumerable = new RepeatedField<double>() { 1.0 };
        private static readonly RepeatedField<long> _sourceLongEnumerable = new RepeatedField<long>() { 2000 };

        [Fact]
        public void ConvertCollectionBytesToByteDoubleArray()
        {
            var context = new TestConverterContext("output", typeof(byte[][]), _sourceMemoryEnumerable);
            Assert.True(_converter.TryConvert(context, out object target));
            TestUtility.AssertIsTypeAndConvert<byte[][]>(target);
        }

        [Fact]
        public void ConvertCollectionBytesToByteArrayList()
        {
            var context = new TestConverterContext("output", typeof(List<byte[]>), _sourceMemoryEnumerable);
            Assert.True(_converter.TryConvert(context, out object target));
            TestUtility.AssertIsTypeAndConvert<List<byte[]>>(target);
        }

        [Fact]
        public void ConvertCollectionBytesToByteArrayHashSet()
        {
            var context = new TestConverterContext("output", typeof(HashSet<byte[]>), _sourceMemoryEnumerable);
            Assert.True(_converter.TryConvert(context, out object target));
            TestUtility.AssertIsTypeAndConvert<HashSet<byte[]>>(target);
        }

        // Queue implements IEnumerable but is not converted
        [Fact]
        public void ConvertCollectionBytesToByteArrayQueue()
        {
            var context = new TestConverterContext("output", typeof(Queue<byte[]>), _sourceMemoryEnumerable);
            Assert.False(context.Parameter.Type.IsAssignableFrom(typeof(IEnumerable<byte[]>)));
            Assert.False(_converter.TryConvert(context, out object target));
        }

        [Fact]
        public void ConvertCollectionStringToStringArray()
        {
            var context = new TestConverterContext("output", typeof(string[]), _sourceStringEnumerable);
            Assert.True(_converter.TryConvert(context, out object target));
            TestUtility.AssertIsTypeAndConvert<string[]>(target);
        }

        [Fact]
        public void ConvertCollectionSint64ToLongArray()
        {
            var context = new TestConverterContext("output", typeof(long[]), _sourceLongEnumerable);
            Assert.True(_converter.TryConvert(context, out object target));
            TestUtility.AssertIsTypeAndConvert<long[]>(target);
        }

        [Fact]
        public void ConvertCollectionDoubleToDoubleArray()
        {
            var context = new TestConverterContext("output", typeof(double[]), _sourceDoubleEnumerable);
            Assert.True(_converter.TryConvert(context, out object target));
            TestUtility.AssertIsTypeAndConvert<double[]>(target);
        }
    }
}
