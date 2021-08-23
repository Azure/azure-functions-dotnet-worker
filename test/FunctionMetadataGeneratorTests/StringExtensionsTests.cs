// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Sdk;
using Xunit;

namespace Microsoft.Azure.Functions.SdkTests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("QueueTrigger", "queueTrigger")]
        [InlineData("HTTPTrigger", "httpTrigger")]
        [InlineData("Blob", "blob")]
        [InlineData("http", "http")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void ToCamelCaseWorks(string input, string expectedOutput)
        {
            var actual = input.ToCamelCase();

            Assert.Equal(expectedOutput, actual);
        }
    }
}
