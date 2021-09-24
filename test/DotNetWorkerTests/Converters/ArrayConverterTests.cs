// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class ArrayConverterTests
    {
        private const string _sourceString = "hello";
        private static readonly byte[] _sourceBytes = Encoding.UTF8.GetBytes(_sourceString);
        private static readonly ReadOnlyMemory<byte> _sourceMemory = new ReadOnlyMemory<byte>(_sourceBytes);
        private ArrayConverter _converter = new ArrayConverter();

        private static readonly IEnumerable<ReadOnlyMemory<byte>> _sourceMemoryEnumerable = new RepeatedField<ReadOnlyMemory<byte>>() { _sourceMemory };
        private static readonly RepeatedField<string> _sourceStringEnumerable = new RepeatedField<string>() { _sourceString };
        private static readonly RepeatedField<double> _sourceDoubleEnumerable = new RepeatedField<double>() { 1.0 };
        private static readonly RepeatedField<long> _sourceLongEnumerable = new RepeatedField<long>() { 2000 };

        [Fact]
        public async Task ConvertCollectionBytesToJaggedByteArray()
        {
            var context = new TestConverterContext(typeof(byte[][]), _sourceMemoryEnumerable);
                        
            var coneversionResult = await _converter.ConvertAsync(context);

            Assert.True(coneversionResult.IsSuccess);
            TestUtility.AssertIsTypeAndConvert<byte[][]>(coneversionResult.Model);
        }

        [Fact]
        public async Task ConvertCollectionBytesToReadOnlyByteArray()
        {
            var context = new TestConverterContext(typeof(ReadOnlyMemory<byte>[]), _sourceMemoryEnumerable);
                        
            var coneversionResult = await _converter.ConvertAsync(context);

            Assert.True(coneversionResult.IsSuccess);
            TestUtility.AssertIsTypeAndConvert<ReadOnlyMemory<byte>[]>(coneversionResult.Model);
        }

        [Fact]
        public async Task ConvertCollectionStringToStringArray()
        {
            var context = new TestConverterContext(typeof(string[]), _sourceStringEnumerable);

            var coneversionResult = await _converter.ConvertAsync(context);

            Assert.True(coneversionResult.IsSuccess);
            TestUtility.AssertIsTypeAndConvert<string[]>(coneversionResult.Model);
        }

        [Fact]
        public async Task ConvertCollectionSint64ToLongArray()
        {
            var context = new TestConverterContext(typeof(long[]), _sourceLongEnumerable);

            var coneversionResult = await _converter.ConvertAsync(context);

            Assert.True(coneversionResult.IsSuccess);
            TestUtility.AssertIsTypeAndConvert<long[]>(coneversionResult.Model);
        }

        [Fact]
        public async Task ConvertCollectionDoubleToDoubleArray()
        {
            var context = new TestConverterContext(typeof(double[]), _sourceDoubleEnumerable);
                        
            var coneversionResult = await _converter.ConvertAsync(context);

            Assert.True(coneversionResult.IsSuccess);
            TestUtility.AssertIsTypeAndConvert<double[]>(coneversionResult.Model);
        }
    }
}
