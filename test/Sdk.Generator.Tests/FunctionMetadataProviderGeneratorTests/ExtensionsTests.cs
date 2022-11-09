// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public partial class FunctionMetadataProviderGeneratorTests
    {
        public class ExtensionsTests
        {
            [Theory]
            [InlineData("HttpTriggerAttribute", "Attribute", "HttpTrigger")]
            [InlineData("QueueOutput", "Output", "Queue")]
            [InlineData("QueueInput", "Input", "Queue")]
            public void TrimStringFromEndWorks(string input, string toTrim, string expectedOutput)
            {
                var actual = input.TrimStringFromEnd(toTrim);

                Assert.Equal(expectedOutput, actual);
            }

            [Theory]
            [InlineData("HttpTriggerAttribute", "HttpTrigger")]
            [InlineData("QueueOutput", "Queue")]
            [InlineData("QueueInput", "Queue")]
            [InlineData("Foo", "Foo")]
            public void TrimStringsFromEndWorks(string input, string expectedOutput)
            {
                var toTrim = new string[] { "Attribute", "Input", "Output" };
                var actual = input.TrimStringsFromEnd(toTrim);

                Assert.Equal(expectedOutput, actual);
            }

            [Theory]
            [InlineData("isBatched", "IsBatched")]
            [InlineData("MyProperty", "MyProperty")]
            [InlineData("myproperty", "Myproperty")]
            public void UpperCaseFirstLetter(string input, string expectedOutput)
            {
                Assert.Equal(input.UppercaseFirst(), expectedOutput);
            }
        }
    }
}
