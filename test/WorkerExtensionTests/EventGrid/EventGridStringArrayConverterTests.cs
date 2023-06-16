// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.WorkerExtension.Tests.EventGrid
{
    public class EventGridStringArrayConverterTests
    {
        private EventGridStringArrayConverter _eventGridConverter;

        public EventGridStringArrayConverterTests()
        {
            _eventGridConverter = new EventGridStringArrayConverter();
        }

        [Fact]
        public async Task ConvertAsync_SourceAsObject_ReturnsUnhandled()
        {
            var context = new TestConverterContext(typeof(string[]), new object());

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_Returns_Success()
        {
            var context = new TestConverterContext(typeof(string[]), "[{\"specversion\":\"1.0\",\"id\":\"b85d631a-101e-005a-02f2-cee7aa06f148\",\"type\":\"zohan.music.request\",\"source\":\"https://zohan.dev/music/\",\"subject\":\"zohan/music/requests/4322\",\"time\":\"2020-09-14T10:00:00Z\",\"data\":{\"artist\":\"Gerardo\",\"song\":\"Rico Suave\"}},{\"specversion\":\"1.0\",\"id\":\"2947780a-356b-c5a5-feb4-f5261fb2f155\",\"type\":\"test\",\"source\":\"moo\",\"subject\":\"life is very lit\",\"time\":\"2020-09-14T10:00:00Z\",\"data\":{\"artist\":\"wooo\",\"song\":\"life is lit\"}}]");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is string[]);
            Assert.Equal(2, ((string[])conversionResult.Value).Length);
        }

        [Fact]
        public async Task ConvertAsync_Returns_Unhandled_For_Unsupported_Type()
        {
            var context = new TestConverterContext(typeof(string), "[{\"specversion\":\"1.0\",\"id\":\"b85d631a-101e-005a-02f2-cee7aa06f148\",\"type\":\"zohan.music.request\",\"source\":\"https://zohan.dev/music/\",\"subject\":\"zohan/music/requests/4322\",\"time\":\"2020-09-14T10:00:00Z\",\"data\":{\"artist\":\"Gerardo\",\"song\":\"Rico Suave\"}},{\"specversion\":\"1.0\",\"id\":\"2947780a-356b-c5a5-feb4-f5261fb2f155\",\"type\":\"test\",\"source\":\"moo\",\"subject\":\"life is very lit\",\"time\":\"2020-09-14T10:00:00Z\",\"data\":{\"artist\":\"wooo\",\"song\":\"life is lit\"}}]");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
        }

        [Fact]
        public async Task ConvertAsync_SingleElement_Returns_Success()
        {
            var context = new TestConverterContext(typeof(string[]), "[{\"specversion\":\"1.0\",\"id\":\"2947780a-356b-c5a5-feb4-f5261fb2f155\",\"type\":\"test\",\"source\":\"moo\",\"subject\":\"lol test\",\"time\":\"2020-09-14T10:00:00Z\",\"data\":{\"artist\":\"wooo\",\"song\":\"some song\"}}]");

            var conversionResult = await _eventGridConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.True(conversionResult.Value is string[]);
            Assert.Single((string[])conversionResult.Value);
        }
    }
}
