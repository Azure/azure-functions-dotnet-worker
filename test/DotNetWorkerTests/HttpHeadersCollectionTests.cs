// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Http;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class HttpHeadersCollectionTests
    {
        [Theory]
        [InlineData("headername", "headervalue")]
        [InlineData("headername", "header(test)value")]
        [InlineData("headername", "header-value")]
        [InlineData("From", "test(at)test")]
        public void HeaderValidationResult(string name, string value)
        {
            IEnumerable<KeyValuePair<string, string>> headers = new Dictionary<string, string>()
            {
                { name, value },
            };

            var headersCollection = new HttpHeadersCollection(headers);


            Assert.Single(headersCollection);
        }
    }
}

