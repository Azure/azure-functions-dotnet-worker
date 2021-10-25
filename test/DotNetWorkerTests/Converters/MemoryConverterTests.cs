// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
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
        public async Task ConvertToByteArray()
        {
            var context = new TestConverterContext(typeof(byte[]), _sourceMemory);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            var bytes = TestUtility.AssertIsTypeAndConvert<byte[]>(conversionResult.Value);
            Assert.Equal(_sourceBytes, bytes);
        }

        [Fact]
        public async Task ConvertToString()
        {
            var context = new TestConverterContext(typeof(string), _sourceMemory);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            var convertedString = TestUtility.AssertIsTypeAndConvert<string>(conversionResult.Value);
            Assert.Equal(_sourceString, convertedString);
        }
    }
}
