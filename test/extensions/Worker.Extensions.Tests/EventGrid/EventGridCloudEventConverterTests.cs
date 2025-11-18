// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.EventGrid
{
    public class EventGridCloudEventConverterTests
    {
        private EventGridCloudEventConverter _eventGridConverter;

        public EventGridCloudEventConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            _eventGridConverter = new EventGridCloudEventConverter();
        }

        [Fact]
        public async Task ConvertAsync_Source_IsNotAString_ReturnsFailed()
        {
            var context = new TestConverterContext(typeof(CloudEvent), new object());

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
            var context = new TestConverterContext(typeof(CloudEvent[]), @"{""invalid"" :json""}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Contains("Binding parameters to complex objects uses JSON serialization", conversionResult.Error.Message);
        }

        [Fact]
        public async Task ConvertAsync_SingleCloudEvent_ReturnsSuccess()
        {
            var context = new TestConverterContext(typeof(CloudEvent), EventGridTestHelper.GetEventGridJsonData());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is CloudEvent);
        }


        [Fact]
        public async Task ConvertAsync_CloudEventArray_ReturnsSuccess()
        {
            var context = new TestConverterContext(typeof(CloudEvent[]), EventGridTestHelper.GetEventGridJsonDataArray());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is CloudEvent[]);
            Assert.Equal(2, ((CloudEvent[])conversionResult.Value).Length);
        }

        [Fact]
        public async Task ConvertAsync_CloudEventArray_SingleElement_ReturnsSuccess()
        {
            var context = new TestConverterContext(typeof(CloudEvent[]), $"[{EventGridTestHelper.GetEventGridJsonData()}]");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Single((CloudEvent[])conversionResult.Value);
        }
    }
}
