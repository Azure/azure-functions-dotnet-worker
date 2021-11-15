// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class GuidConverterTests
    {
        private readonly GuidConverter _converter = new GuidConverter();

        [Theory]
        [InlineData("6cf8151848244ca78a169e14b4f13beb", typeof(Guid))]
        [InlineData("6cf81518-4824-4ca7-8a16-9e14b4f13beb", typeof(Guid))]
        [InlineData("{6cf81518-4824-4ca7-8a16-9e14b4f13beb}", typeof(Guid))]
        [InlineData("(6cf81518-4824-4ca7-8a16-9e14b4f13beb)", typeof(Guid))]
        [InlineData("6cf8151848244ca78a169e14b4f13beb", typeof(Guid?))]
        [InlineData("6cf81518-4824-4ca7-8a16-9e14b4f13beb", typeof(Guid?))]
        public async Task ConversionSuccessfulForValidSourceValueAsync(object source, Type parameterType)
        {
            var context = new TestConverterContext(parameterType, source);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            var convertedGuid = TestUtility.AssertIsTypeAndConvert<Guid>(conversionResult.Value);
            Assert.Equal(Guid.Parse(source.ToString()), convertedGuid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("a-string-with-four-hyphens")]
        [InlineData(12345)]
        [InlineData(true)]
        [InlineData("6cf81518-4824-4ca7-8a16")]
        [InlineData("6cf81518-4824-4ca7-8a16_9e14b4f13beb")]
        [InlineData("ValidGuidInsideAString6cf81518-4824-4ca7-8a16-9e14b4f13beb")]
        public async Task ConversionFailedForInvalidSourceValue(object source)
        {
            var context = new TestConverterContext(typeof(Guid), source);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
            Assert.Null(conversionResult.Value);
        }

        [Theory]
        [InlineData("6cf81518-4824-4ca7-8a16-9e14b4f13beb", typeof(string))]
        [InlineData("6cf81518-4824-4ca7-8a16-9e14b4f13beb", typeof(int))]
        [InlineData("6cf81518-4824-4ca7-8a16-9e14b4f13beb", typeof(bool))]
        public async Task ConversionFailedForInvalidParameterType(object source, Type parameterType)
        {
            var context = new TestConverterContext(parameterType, source);

            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
            Assert.Null(conversionResult.Value);
        }
    }
}
