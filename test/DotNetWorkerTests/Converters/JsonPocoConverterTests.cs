// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class JsonPocoConverterTests
    {
        private JsonPocoConverter _jsonPocoConverter;

        public JsonPocoConverterTests()
        {
            var options = new WorkerOptions();
            var wrapper = new OptionsWrapper<WorkerOptions>(options);
            _jsonPocoConverter = new JsonPocoConverter(wrapper);
        }

        [Fact]
        public void SourceIsNotValidJsonString_ReturnsNull()
        {
            string source = "invalid string";
            var context = new TestConverterContext("input", typeof(Book), source);

            Assert.False(_jsonPocoConverter.TryConvert(context, out object target));

            Assert.Null(target);
        }

        [Fact]
        public void SuccessfulConversion()
        {
            // Also validate that this is, by default, case insensitive
            string source = "{ \"title\": \"a\", \"Author\": \"b\" }";
            var context = new TestConverterContext("input", typeof(Book), source);

            Assert.True(_jsonPocoConverter.TryConvert(context, out object bookObj));

            var book = TestUtility.AssertIsTypeAndConvert<Book>(bookObj);
            Assert.Equal("a", book.Title);
            Assert.Equal("b", book.Author);
        }


        [Fact]
        public void ConvertJsonStringArrayToIEnumerableOfT()
        {
            var source = "[ \"a\", \"b\", \"c\" ]";
            var context = new TestConverterContext("input", typeof(IEnumerable<string>), source);

            Assert.True(_jsonPocoConverter.TryConvert(context, out object target));

            var targetEnum = TestUtility.AssertIsTypeAndConvert<IEnumerable<string>>(target);
            Assert.Collection(targetEnum,
                p => Assert.True(p == "a"),
                p => Assert.True(p == "b"),
                p => Assert.True(p == "c"));
        }

        [Fact]
        public void Newtonsoft()
        {
            var options = new WorkerOptions
            {
                Serializer = new NewtonsoftJsonObjectSerializer()
            };

            var wrapper = new OptionsWrapper<WorkerOptions>(options);
            var jsonPocoConverter = new JsonPocoConverter(wrapper);

            string source = "{ \"title\": \"a\", \"Author\": \"b\" }";
            var context = new TestConverterContext("input", typeof(NewtonsoftBook), source);

            Assert.True(jsonPocoConverter.TryConvert(context, out object bookObj));

            var book = TestUtility.AssertIsTypeAndConvert<NewtonsoftBook>(bookObj);
            Assert.Equal("a", book.BookTitle);
            Assert.Equal("b", book.BookAuthor);
        }

        // Used to test that you can use Newtonsoft.Json attributes
        private class NewtonsoftBook
        {
            [JsonProperty("title")]
            public string BookTitle { get; set; }

            [JsonProperty("author")]
            public string BookAuthor { get; set; }
        }
    }
}
