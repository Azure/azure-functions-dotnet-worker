// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core.Serialization;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Configuration
{
    public class WorkerOptionsTests
    {
        [Fact]
        public void Default_Serializer()
        {
            var options = new WorkerOptions();
            var serializer = options.Serializer;

            Assert.IsType<JsonObjectSerializer>(serializer);

            // Ensure that serializer is being cached.
            Assert.Same(serializer, options.Serializer);
        }
    }
}
