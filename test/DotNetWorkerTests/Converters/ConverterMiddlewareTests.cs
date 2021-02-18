// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class ConverterMiddlewareTests
    {
        private ConverterMiddleware _paramConverterManager;

        public ConverterMiddlewareTests()
        {
            // Test overriding serialization settings.
            var serializer = new JsonObjectSerializer(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _paramConverterManager = TestUtility.GetDefaultConverterMiddleware(o => o.Serializer = serializer);
        }

        [Fact]
        public void ExactMatch()
        {
            string source = "abc";
            var context = new TestConverterContext("input", typeof(string), source);

            Assert.True(_paramConverterManager.TryConvert(context, out object target));
            Assert.Equal(source, target);
        }

        [Fact]
        public void JsonArrayToIReadOnlyListOfT()
        {
            // Simulate Cosmos for POCO with case insensitive json
            var source =
                 @"[
                    { ""id"": ""1"", ""author"": ""a"", ""title"": ""b"" },
                    { ""id"": ""2"", ""author"": ""c"", ""title"": ""d"" },
                    { ""id"": ""3"", ""author"": ""e"", ""title"": ""f"" }
                  ]";

            var context = new TestConverterContext("input", typeof(IReadOnlyList<Book>), source);

            Assert.True(_paramConverterManager.TryConvert(context, out object target));

            var targetEnum = TestUtility.AssertIsTypeAndConvert<IReadOnlyList<Book>>(target);
            Assert.Collection(targetEnum,
                p => Assert.True(p.Id == "1" && p.Author == "a"),
                p => Assert.True(p.Id == "2" && p.Author == "c"),
                p => Assert.True(p.Id == "3" && p.Author == "e"));
        }
    }
}
