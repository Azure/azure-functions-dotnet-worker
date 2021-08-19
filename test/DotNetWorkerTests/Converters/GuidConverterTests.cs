// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class GuidConverterTests
    {
        private readonly GuidConverter _converter = new GuidConverter();
        private readonly string _parameterName = "Id";

        [Theory]
        [InlineData("6cf8151848244ca78a169e14b4f13beb", typeof(Guid))]
        [InlineData("6cf81518-4824-4ca7-8a16-9e14b4f13beb", typeof(Guid))]
        [InlineData("{6cf81518-4824-4ca7-8a16-9e14b4f13beb}", typeof(Guid))]
        [InlineData("(6cf81518-4824-4ca7-8a16-9e14b4f13beb)", typeof(Guid))]
        [InlineData("6cf8151848244ca78a169e14b4f13beb", typeof(Guid?))]
        [InlineData("6cf81518-4824-4ca7-8a16-9e14b4f13beb", typeof(Guid?))]
        public void ConversionSuccessfulForValidSourceValue(object source, Type parameterType)
        {
            var context = new TestConverterContext(_parameterName, parameterType, source);
            
            var didConvert = _converter.TryConvert(context, out object target);

            Assert.True(didConvert);
            var convertedGuid = TestUtility.AssertIsTypeAndConvert<Guid>(target);
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
        public void ConversionFailedForInvalidSourceValue(object source)
        {
            var context = new TestConverterContext(_parameterName, typeof(Guid), source);
            
            var didConvert = _converter.TryConvert(context, out object target);

            Assert.False(didConvert);
            Assert.Null(target);
        }
                
        [Theory]
        [InlineData("6cf81518-4824-4ca7-8a16-9e14b4f13beb", typeof(string))]
        [InlineData("6cf81518-4824-4ca7-8a16-9e14b4f13beb", typeof(int))]
        [InlineData("6cf81518-4824-4ca7-8a16-9e14b4f13beb", typeof(bool))]
        public void ConversionFailedForInvalidParameterType(object source, Type parameterType)
        {
            var context = new TestConverterContext(_parameterName, parameterType, source);
            
            var didConvert = _converter.TryConvert(context, out object target);

            Assert.False(didConvert);
            Assert.Null(target);
        }
    }
}
