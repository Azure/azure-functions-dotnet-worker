using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class JsonPocoConverterTests
    {
        private JsonPocoConverter _jsonPocoConverter;

        public JsonPocoConverterTests()
        {
            var options = new JsonSerializerOptions();
            var wrapper = new OptionsWrapper<JsonSerializerOptions>(options);
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
            string source = "{ \"Title\": \"a\", \"Author\": \"b\" }";
            var context = new TestConverterContext("input", typeof(Book), source);

            Assert.True(_jsonPocoConverter.TryConvert(context, out object bookObj));

            Book book = bookObj as Book;
            Assert.NotNull(book);
            Assert.Equal("a", book.Title);
            Assert.Equal("b", book.Author);
        }


        [Fact]
        public void ConvertJsonStringArrayToIEnumerableOfT()
        {
            var source = "[ \"a\", \"b\", \"c\" ]";
            var context = new TestConverterContext("input", typeof(IEnumerable<string>), source);

            Assert.True(_jsonPocoConverter.TryConvert(context, out object? target));

            var targetEnum = TestUtility.AssertIsTypeAndConvert<IEnumerable<string>>(target);
            Assert.Collection(targetEnum,
                p => Assert.True(p == "a"),
                p => Assert.True(p == "b"),
                p => Assert.True(p == "c"));
        }
    }
}
