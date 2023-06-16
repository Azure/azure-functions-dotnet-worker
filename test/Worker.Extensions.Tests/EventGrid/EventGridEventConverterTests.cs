// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
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
        public async Task ConvertAsync_SourceAsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(EventGridEvent), new object());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_Returns_Success()
        {
            var context = new TestConverterContext(typeof(EventGridEvent), "{\"id\":\"'1\",\"topic\":\"hello\",\"subject\":\"yoursubject\",\"eventType\":\"yourEventType\",\"eventTime\":\"2018-01-23T17:02:19.6069787Z\",\"data\":\"test\",\"dataVersion\":\"test\"}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is EventGridEvent);
        }

        [Fact]
        public async Task ConvertAsync_Returns_Unhandled_For_Unsupported_Type()
        {
            var context = new TestConverterContext(typeof(string), "{\"id\":\"'1\",\"topic\":\"hello\",\"subject\":\"yoursubject\",\"eventType\":\"yourEventType\",\"eventTime\":\"2018-01-23T17:02:19.6069787Z\",\"data\":\"test\",\"dataVersion\":\"test\"}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_Returns_Failed_Bad_Source()
        {
            var context = new TestConverterContext(typeof(EventGridEvent), "{\"specversion\":\"1.0\",\"id\":\"2947780a-356b-c5a5-feb4-f5261fb2f155\",\"type\":\"test\",\"source\":\"moo\",\"subject\":\"lol test\",\"time\":\"2020-09-14T10:00:00Z\",\"data\":{\"artist\":\"wooo\",\"song\":\"some song\"}}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync__EventGridCollectible_SourceAsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(EventGridEvent[]), new object());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync__EventGridCollectible_Returns_Success()
        {
            var context = new TestConverterContext(typeof(EventGridEvent[]), "[{\"id\":\"'1\",\"topic\":\"hello\",\"subject\":\"yoursubject\",\"eventType\":\"yourEventType\",\"eventTime\":\"2018-01-23T17:02:19.6069787Z\",\"data\":\"test\",\"dataVersion\":\"test\"},{\"id\":\"'1\",\"topic\":\"hello\",\"subject\":\"yoursubject\",\"eventType\":\"yourEventType\",\"eventTime\":\"2018-01-23T17:02:19.6069787Z\",\"data\":\"lmao\",\"dataVersion\":\"test\"}]");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is EventGridEvent[]);
            Assert.Equal(2, ((EventGridEvent[])conversionResult.Value).Length);
        }

        [Fact]
        public async Task ConvertAsync_EventGridCollectible_Returns_Unhandled_For_Unsupported_Type()
        {
            var context = new TestConverterContext(typeof(string), "{\"id\":\"'1\",\"topic\":\"hello\",\"subject\":\"yoursubject\",\"eventType\":\"yourEventType\",\"eventTime\":\"2018-01-23T17:02:19.6069787Z\",\"data\":\"test\",\"dataVersion\":\"test\"}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync__EventGridCollectible_Returns_Failed_Bad_Source()
        {
            var context = new TestConverterContext(typeof(EventGridEvent[]), "{\"specversion\":\"1.0\",\"id\":\"2947780a-356b-c5a5-feb4-f5261fb2f155\",\"type\":\"test\",\"source\":\"moo\",\"subject\":\"lol test\",\"time\":\"2020-09-14T10:00:00Z\",\"data\":{\"artist\":\"wooo\",\"song\":\"some song\"}}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }
    }
}
