// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.EventGrid
{
    public class EventGridBinaryDataConverterTests
    {
        private EventGridBinaryDataConverter _eventGridConverter;

        public EventGridBinaryDataConverterTests()
        {
            _eventGridConverter = new EventGridBinaryDataConverter();
        }

        [Fact]
        public async Task ConvertAsync_Source_IsNotAString_ReturnsFailed()
        {
            var context = new TestConverterContext(typeof(BinaryData), new object());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Context source must be a non-null string", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_UnsupportedTargetType_ReturnsFailed()
        {
            var context = new TestConverterContext(typeof(string), "");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_InvalidJson_ThrowsJsonException_ReturnsFailed()
        {
            var context = new TestConverterContext(typeof(BinaryData[]), @"{""invalid"" :json""}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Contains("Binding parameters to complex objects uses JSON serialization", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_SingleBinaryData_ReturnsSuccess()
        {
            var context = new TestConverterContext(typeof(BinaryData), EventGridTestHelper.GetEventGridJsonData());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is BinaryData);
        }

        [Fact]
        public async Task ConvertAsync_BinaryDataArray_ReturnsSuccess()
        {
            var context = new TestConverterContext(typeof(BinaryData[]), EventGridTestHelper.GetEventGridJsonDataArray());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is BinaryData[]);
            Assert.Equal(2, ((BinaryData[])conversionResult.Value).Length);
        }

        [Fact]
        public async Task ConvertAsync_BinaryDataArray_SingleElement_ReturnsSuccess()
        {
            var context = new TestConverterContext(typeof(BinaryData[]), $"[{EventGridTestHelper.GetEventGridJsonData()}]");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is BinaryData[]);
            Assert.Single((BinaryData[])conversionResult.Value);
        }
    }
}
