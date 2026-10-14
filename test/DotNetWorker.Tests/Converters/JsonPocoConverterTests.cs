// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core.Serialization;
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
            options.Serializer = new JsonObjectSerializer(new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });
            var wrapper = new OptionsWrapper<WorkerOptions>(options);
            _jsonPocoConverter = new JsonPocoConverter(wrapper);
        }

        [Fact]
        public async Task SourceIsNotValidJsonString_ReturnsNull()
        {
            string source = "invalid string";
            var context = new TestConverterContext(typeof(Book), source);
                        
            var conversionResult = await _jsonPocoConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.Null(conversionResult.Value);
            Assert.NotNull(conversionResult.Error);
        }

        [Fact]
        public async Task SuccessfulConversion()
        {
            string source = "{ \"Title\": \"a\", \"Author\": \"b\" }";
            var context = new TestConverterContext(typeof(Book), source);
                        
            var conversionResult = await _jsonPocoConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);

            var book = TestUtility.AssertIsTypeAndConvert<Book>(conversionResult.Value);
            Assert.Equal("a", book.Title);
            Assert.Equal("b", book.Author);
        }

        [Fact]
        public async Task ConvertMemory()
        {
            string source = "{ \"Title\": \"a\", \"Author\": \"b\" }";
            var sourceMemory = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(source));
            var context = new TestConverterContext(typeof(Book), sourceMemory);

            var conversionResult = await _jsonPocoConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);

            var book = TestUtility.AssertIsTypeAndConvert<Book>(conversionResult.Value);
            Assert.Equal("a", book.Title);
            Assert.Equal("b", book.Author);
        }

        [Fact]
        public async Task ConvertJsonStringArrayToIEnumerableOfT()
        {
            var source = "[ \"a\", \"b\", \"c\" ]";
            var context = new TestConverterContext(typeof(IEnumerable<string>), source);

            var conversionResult = await _jsonPocoConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);

            var targetEnum = TestUtility.AssertIsTypeAndConvert<IEnumerable<string>>(conversionResult.Value);
            Assert.Collection(targetEnum,
                p => Assert.True(p == "a"),
                p => Assert.True(p == "b"),
                p => Assert.True(p == "c"));
        }

        [Fact]
        public async Task Newtonsoft()
        {
            var options = new WorkerOptions
            {
                Serializer = new NewtonsoftJsonObjectSerializer()
            };

            var wrapper = new OptionsWrapper<WorkerOptions>(options);
            var jsonPocoConverter = new JsonPocoConverter(wrapper);

            string source = "{ \"title\": \"a\", \"Author\": \"b\" }";
            var context = new TestConverterContext(typeof(NewtonsoftBook), source);
                        
            var conversionResult = await jsonPocoConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);

            var book = TestUtility.AssertIsTypeAndConvert<NewtonsoftBook>(conversionResult.Value);
            Assert.Equal("a", book.BookTitle);
            Assert.Equal("b", book.BookAuthor);
        }

        [Theory]
        [InlineData("true", typeof(bool), true)]
        [InlineData("True", typeof(bool), true)]
        [InlineData("FALSE", typeof(bool), false)]
        [InlineData("False", typeof(bool?), false)]
        [InlineData("tRuE", typeof(bool), true)]
        [InlineData("fAlSe", typeof(bool?), false)]
        public async Task ConvertBooleanString_IsCaseInsensitive(string source, Type targetType, bool expected)
        {
            var context = new TestConverterContext(targetType, source);

            var conversionResult = await _jsonPocoConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(expected, conversionResult.Value);
        }

        [Fact]
        public async Task ConvertInvalidBooleanString_Fails()
        {
            var context = new TestConverterContext(typeof(bool), "yes");

            var conversionResult = await _jsonPocoConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.NotNull(conversionResult.Error);
        }

        [Fact]
        public async Task ConvertBooleanString_CustomJsonConverterTakesPrecedence()
        {
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new YesNoBooleanConverter());
            var options = new WorkerOptions { Serializer = new JsonObjectSerializer(serializerOptions) };
            var jsonPocoConverter = new JsonPocoConverter(new OptionsWrapper<WorkerOptions>(options));
            var context = new TestConverterContext(typeof(bool), "\"yes\"");

            var conversionResult = await jsonPocoConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, conversionResult.Status);
            Assert.Equal(true, conversionResult.Value);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("True")]
        [InlineData(" FALSE ")]
        public async Task ConvertBooleanString_CustomJsonConverterRejection_IsPreserved(string source)
        {
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new RejectingBooleanConverter());
            var options = new WorkerOptions { Serializer = new JsonObjectSerializer(serializerOptions) };
            var jsonPocoConverter = new JsonPocoConverter(new OptionsWrapper<WorkerOptions>(options));
            var context = new TestConverterContext(typeof(bool), source);

            var conversionResult = await jsonPocoConverter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, conversionResult.Status);
            Assert.NotNull(conversionResult.Error);
        }

        private class RejectingBooleanConverter : System.Text.Json.Serialization.JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                throw new System.Text.Json.JsonException("Booleans are not accepted.");

            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
                writer.WriteBooleanValue(value);
        }

        private class YesNoBooleanConverter : System.Text.Json.Serialization.JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                reader.GetString() == "yes";

            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
                writer.WriteStringValue(value ? "yes" : "no");
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
