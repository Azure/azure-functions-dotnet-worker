// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.EventGrid
{
    public class EventGridEventConverterTests
    {
        private EventGridEventConverter _eventGridConverter;

        public EventGridEventConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();

            _eventGridConverter = new EventGridEventConverter();
        }

        [Fact]
        public async Task ConvertAsync_Source_IsNotAString_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(EventGridEvent), new object());

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
            var context = new TestConverterContext(typeof(EventGridEvent), @"{""invalid"" :json""}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Contains("Binding parameters to complex objects uses JSON serialization", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_InvalidData_Throws_ReturnsFailed()
        {
            var context = new TestConverterContext(typeof(EventGridEvent), EventGridTestHelper.GetEventGridJsonData());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Equal("Value cannot be null. (Parameter 'EventType')", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_SingleEventGridEvent_ReturnsSuccess()
        {
            var context = new TestConverterContext(typeof(EventGridEvent), EventGridTestHelper.GetEventGridEventJsonData());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is EventGridEvent);
        }

        [Fact]
        public async Task ConvertAsync_EventGridArray_ReturnsSuccess()
        {
            var context = new TestConverterContext(typeof(EventGridEvent[]), EventGridTestHelper.GetEventGridEventJsonDataArray());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is EventGridEvent[]);
            Assert.Equal(2, ((EventGridEvent[])conversionResult.Value).Length);
        }
    }
}
