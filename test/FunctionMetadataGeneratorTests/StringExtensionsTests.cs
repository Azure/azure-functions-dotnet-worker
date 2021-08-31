// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Sdk;
using Xunit;

namespace Microsoft.Azure.Functions.SdkTests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("HttpTrigger", "httpTrigger")]
        [InlineData("Blob", "blob")]
        [InlineData("http", "http")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void ToLowerFirstCharacterWorks(string input, string expectedOutput)
        {
            var actual = input.ToLowerFirstCharacter();

            Assert.Equal(expectedOutput, actual);
        }
    }
}
