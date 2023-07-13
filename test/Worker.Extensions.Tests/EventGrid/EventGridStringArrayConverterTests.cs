// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.EventGrid
{
    public class EventGridStringArrayConverterTests
    {
        private EventGridStringArrayConverter _eventGridConverter;

        public EventGridStringArrayConverterTests()
        {
            _eventGridConverter = new EventGridStringArrayConverter();
        }

        [Fact]
        public async Task ConvertAsync_Source_IsNotAString_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(string[]), new object());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Context source must be a non-null string", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_UnsupportedTargetType_ReturnsFailed()
        {
            var context = new TestConverterContext(typeof(byte[]), "");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_InvalidJson_ThrowsJsonException_ReturnsFailed()
        {
            var context = new TestConverterContext(typeof(string[]), @"{""invalid"" :json""}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Contains("Binding parameters to complex objects uses JSON serialization", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_StringArray_ReturnsSuccess()
        {
            var context = new TestConverterContext(typeof(string[]), EventGridTestHelper.GetEventGridJsonDataArray());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is string[]);
            Assert.Equal(2, ((string[])conversionResult.Value).Length);
        }

        [Fact]
        public async Task ConvertAsync_StringArray_SingleElement_Returns_Success()
        {
            var context = new TestConverterContext(typeof(string[]),  $"[{EventGridTestHelper.GetEventGridJsonData()}]");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is string[]);
            Assert.Single((string[])conversionResult.Value);
        }
    }
}
