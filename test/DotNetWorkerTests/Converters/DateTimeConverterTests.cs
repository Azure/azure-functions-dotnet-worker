// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class DateTimeConverterTests
    {
        private readonly DateTimeConverter _converter = new();

        [Theory]
        [InlineData("2022-05-16T08:16:53.1880572-03:00", typeof(DateTimeOffset), -3)]
        [InlineData("2022-05-16T08:17:54.1880573-01:00", typeof(DateTimeOffset?), -1)]
        [InlineData("2022-05-16T08:16:53", typeof(DateTimeOffset), null)]
        [InlineData("2022-05-16T08:16:53", typeof(DateTimeOffset?), null)]
        [InlineData("2022-05-16", typeof(DateTimeOffset), null)]
        [InlineData("2022-05-16", typeof(DateTimeOffset?), null)]
        public async Task ConversionSuccessfulForValidSourceDateTimeOffset(object source, Type parameterType, int? expectedOffsetHours)
        {
            var context = new TestConverterContext(parameterType, source);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            var convertedDateTimeOffset = TestUtility.AssertIsTypeAndConvert<DateTimeOffset>(conversionResult.Value);

            // when no offset info is present in input value, offset of local timezone will be set as the offset of the DateTimeOffSet instance.

            if (expectedOffsetHours is null)
            {
                DateTimeOffset offsetCheck = DateTimeOffset.UtcNow;
                expectedOffsetHours = TimeZoneInfo.Local.GetUtcOffset(offsetCheck).Hours;
                if (TimeZoneInfo.Local.IsDaylightSavingTime(offsetCheck) != TimeZoneInfo.Local.IsDaylightSavingTime(convertedDateTimeOffset))
                {
                    expectedOffsetHours += TimeZoneInfo.Local.IsDaylightSavingTime(offsetCheck) ? -1 : +1;
                }
            }

            Assert.Equal(expectedOffsetHours.Value, convertedDateTimeOffset.Offset.Hours);
            Assert.Equal(DateTimeOffset.Parse(source.ToString()), convertedDateTimeOffset);
        }

        [Theory]
        [InlineData("04/11/2022", typeof(DateTime))]
        [InlineData("04/11/2022", typeof(DateTime?))]
        [InlineData("04-13-2022", typeof(DateTime))]
        [InlineData("04-13-2022", typeof(DateTime?))]
        [InlineData("2022-08-15", typeof(DateTime))]
        [InlineData("2022-08-15", typeof(DateTime?))]
        [InlineData("2022-04-11T17:17:12.9326256Z", typeof(DateTime))]
        [InlineData("2022-04-11T17:17:12.9326256Z", typeof(DateTime?))]
        public async Task ConversionSuccessfulForValidSourceValueAsync(object source, Type parameterType)
        {
            var context = new TestConverterContext(parameterType, source);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            var convertedDate = TestUtility.AssertIsTypeAndConvert<DateTime>(conversionResult.Value);
            Assert.Equal(DateTime.Parse(source.ToString()), convertedDate);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("foo")]
        [InlineData(12345)]
        [InlineData(true)]
        [InlineData("20211220")]
        [InlineData("2022")]
        public async Task ConversionFailedForInvalidSourceValue(object source)
        {
            var context = new TestConverterContext(typeof(DateTime), source);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
            Assert.Null(conversionResult.Value);
        }

        [Theory]
        [InlineData("2022-04-15", typeof(string))]
        [InlineData("2022-04-15", typeof(int))]
        [InlineData("2022-04-15", typeof(bool))]
        [InlineData("2022-04-15", typeof(Guid))]
        public async Task ConversionFailedForInvalidParameterType(object source, Type parameterType)
        {
            var context = new TestConverterContext(parameterType, source);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
            Assert.Null(conversionResult.Value);
        }
    }
}
