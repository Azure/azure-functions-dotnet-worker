// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class MemoryConverterTests
    {
        private const string _sourceString = "hello";
        private static readonly byte[] _sourceBytes = Encoding.UTF8.GetBytes(_sourceString);
        private static readonly ReadOnlyMemory<byte> _sourceMemory = new ReadOnlyMemory<byte>(_sourceBytes);
        private MemoryConverter _converter = new MemoryConverter();

        [Fact]
        public void ConvertToByteArray()
        {
            var context = new TestConverterContext("output", typeof(byte[]), _sourceMemory);
            Assert.True(_converter.TryConvert(context, out object target));

            var bytes = TestUtility.AssertIsTypeAndConvert<byte[]>(target);
            Assert.Equal(_sourceBytes, bytes);
        }

        [Fact]
        public void ConvertToString()
        {
            var context = new TestConverterContext("output", typeof(string), _sourceMemory);
            Assert.True(_converter.TryConvert(context, out object target));

            var convertedString = TestUtility.AssertIsTypeAndConvert<string>(target);
            Assert.Equal(_sourceString, convertedString);
        }
    }
}
