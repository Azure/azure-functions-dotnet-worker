// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class ConversionResultTests
    {
        [Fact]
        public void Unhandled_Result_Properties()
        {
            var conversionResult = ConversionResult.Unhandled();

            Assert.False(conversionResult.IsHandled);
            Assert.Null(conversionResult.IsSuccessful);
            Assert.Null(conversionResult.Value);
            Assert.Null(conversionResult.Error);
        }

        [Fact]
        public void Success_Result_Properties()
        {
            var conversionResult = ConversionResult.Success(value: "foo");

            Assert.True(conversionResult.IsHandled);
            Assert.True(conversionResult.IsSuccessful);
            Assert.Null(conversionResult.Error);
            var convertedValue = TestUtility.AssertIsTypeAndConvert<string>(conversionResult.Value);
            Assert.Equal("foo", convertedValue);
        }

        [Fact]
        public void Failed_Result_Properties()
        {
            var exception = new ArgumentException();
            var conversionResult = ConversionResult.Failed(exception);

            Assert.True(conversionResult.IsHandled);
            Assert.False(conversionResult.IsSuccessful);
            Assert.Null(conversionResult.Value);
            Assert.Equal(exception, conversionResult.Error);
        }

        [Fact]
        public void Failed_Throws_With_Null_Argument()
        {
            Assert.Throws<ArgumentNullException>(() => ConversionResult.Failed(exception: null));
        }
    }
}
