// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class CancellationTokenConverterTests
    {
        private CancellationTokenConverter _converter = new CancellationTokenConverter();

        [Theory]
        [InlineData(typeof(CancellationToken))]
        [InlineData(typeof(CancellationToken?))]
        public async Task ConversionSuccessfulForValidTargetTypeAsync(Type targetType)
        {
            var context = new TestConverterContext(targetType, null);
            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(typeof(CancellationToken), conversionResult.Value.GetType());
        }

        [Theory]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(String))]
        [InlineData(typeof(Object))]
        [InlineData(typeof(DateTimeOffset))]
        public async Task ConversionFailedForInvalidTargetType(Type targetType)
        {
            var context = new TestConverterContext(targetType, null);
            var conversionResult = await _converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Unhandled, conversionResult.Status);
            Assert.Null(conversionResult.Value);
        }
    }
}
