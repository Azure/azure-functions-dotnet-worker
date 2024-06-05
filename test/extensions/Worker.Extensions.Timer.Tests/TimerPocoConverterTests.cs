// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Timer.Converters;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

namespace Worker.Extensions.Timer.Tests
{
    public sealed class TimerPocoConverterTests
    {
        [Fact]
        public async Task ConvertAsync_ShouldConvertJsonStringToTimerInfo()
        {
            // The Source JSON contains properties which does not exist in the "TimerInfo" type (ex; Foo).
            const string timerTriggerSourceJson = @"
{
  ""Foo"": ""Bar"",
  ""Schedule"": {
    ""adjustForDST"": true
  },
  ""scheduleStatus"": {
    ""next"": ""2024-12-31T09:51:00-07:00""
  },
  ""IsPastDue"": true
}";
            var converter = new TimerInfoConverter();
            var contextMock = new Mock<ConverterContext>();
            contextMock.SetupGet(c => c.TargetType).Returns(typeof(TimerInfo));
            contextMock.SetupGet(c => c.Source).Returns(timerTriggerSourceJson);

            var result = await converter.ConvertAsync(contextMock.Object);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var timerInfo = Assert.IsType<TimerInfo>(result.Value);
            Assert.True(timerInfo.IsPastDue);
            Assert.Equal(DateTime.Parse("2024-12-31T09:51:00-07:00"), timerInfo?.ScheduleStatus?.Next);
        }

        [Fact]
        public async Task ConvertAsync_ShouldReturnUnhandledWhenTargetTypeIsNotTimerInfo()
        {
            var converter = new TimerInfoConverter();
            var contextMock = new Mock<ConverterContext>();
            contextMock.SetupGet(c => c.TargetType).Returns(typeof(HttpRequestData));
            contextMock.SetupGet(c => c.Source).Returns("{\"ScheduleStatus\": {\"Last\": \"2022-01-01T00:00:00Z\"}}");

            var result = await converter.ConvertAsync(contextMock.Object);

            Assert.Equal(ConversionStatus.Unhandled, result.Status);
        }
    }
}
