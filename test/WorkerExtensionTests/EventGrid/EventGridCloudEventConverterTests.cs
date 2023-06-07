﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Functions.WorkerExtension.Tests.EventGrid
{
    public class EventGridCloudEventConverterTests
    {
        private EventGridCloudEventConverter _eventGridConverter;

        public EventGridCloudEventConverterTests()
        {
            var host = new HostBuilder().ConfigureFunctionsWorkerDefaults((WorkerOptions options) => { }).Build();
            var logger = host.Services.GetService<ILogger<EventGridCloudEventConverter>>();

            _eventGridConverter = new EventGridCloudEventConverter(logger);
        }

        [Fact]
        public async Task ConvertAsync_SourceAsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(CloudEvent), new object());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_Returns_Success()
        {
            var context = new TestConverterContext(typeof(CloudEvent), "{\"specversion\":\"1.0\",\"id\":\"2947780a-356b-c5a5-feb4-f5261fb2f155\",\"type\":\"test\",\"source\":\"moo\",\"subject\":\"lol test\",\"time\":\"2020-09-14T10:00:00Z\",\"data\":{\"artist\":\"wooo\",\"song\":\"some song\"}}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_Returns_Unhandled_For_Unsupported_Type()
        {
            var context = new TestConverterContext(typeof(string), "{\"specversion\":\"1.0\",\"id\":\"2947780a-356b-c5a5-feb4-f5261fb2f155\",\"type\":\"test\",\"source\":\"moo\",\"subject\":\"lol test\",\"time\":\"2020-09-14T10:00:00Z\",\"data\":{\"artist\":\"wooo\",\"song\":\"some song\"}}");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }
    }
}
