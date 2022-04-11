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
        private readonly DateTimeConverter _converter = new DateTimeConverter();

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
        [InlineData("10:30 AM", typeof(TimeOnly))]
        [InlineData("10:30 AM", typeof(TimeOnly?))]
        [InlineData("10:30", typeof(TimeOnly))]
        [InlineData("10:30", typeof(TimeOnly?))]
        [InlineData("23:59:59", typeof(TimeOnly))]
        [InlineData("23:59:59", typeof(TimeOnly?))]
        public async Task ConversionSuccessfulForValidSource_TimeOnly(object source, Type parameterType)
        {
            var context = new TestConverterContext(parameterType, source);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            var convertedTimeOnly = TestUtility.AssertIsTypeAndConvert<TimeOnly>(conversionResult.Value);
            Assert.Equal(TimeOnly.Parse(source.ToString()), convertedTimeOnly);
        }

        [Theory]
        [InlineData("04/11/2022", typeof(DateOnly))]
        [InlineData("04/11/2022", typeof(DateOnly?))]
        [InlineData("04-11-2022", typeof(DateOnly))]
        [InlineData("04-11-2022", typeof(DateOnly?))]
        [InlineData("2022-05-14", typeof(DateOnly))]
        [InlineData("2022-05-14", typeof(DateOnly?))]
        public async Task ConversionSuccessfulForValidSource_DateOnly(object source, Type parameterType)
        {
            var context = new TestConverterContext(parameterType, source);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            var convertedDateOnly = TestUtility.AssertIsTypeAndConvert<DateOnly>(conversionResult.Value);
            Assert.Equal(DateOnly.Parse(source.ToString()), convertedDateOnly);
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
