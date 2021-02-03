// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class OutputBindingConverterTests
    {
        [Fact]
        public void UpdatesExecutionContext()
        {
            var converter = new OutputBindingConverter();

            var context = new TestConverterContext("output", typeof(OutputBinding<string>), null);

            Assert.True(converter.TryConvert(context, out object target));

            var outputBinding = TestUtility.AssertIsTypeAndConvert<OutputBinding<string>>(target);
            outputBinding.SetValue("abc");

            var outputs = context.ExecutionContext.OutputBindings;
            Assert.Equal("abc", outputs["output"]);
        }
    }
}
