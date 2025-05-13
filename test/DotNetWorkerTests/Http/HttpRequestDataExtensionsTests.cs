// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Tests.OutputBindings;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class HttpRequestDataExtensionsTests
    {
        /// <summary>
        /// Tests if ReadAsString throws ArgumentNullException when the HttpRequestData is null.
        /// </summary>
        [Fact]
        public void ReadAsString_RequestIsNull_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => HttpRequestDataExtensions.ReadAsString(null!));
        }

        /// <summary>
        /// Tests if ReadAsString correctly reads the body with the default UTF-8 encoding.
        /// </summary>
        [Fact]
        public void ReadAsString_BodyIsNotNull_ReadsBodyCorrectly()
        {
            // Arrange
            FunctionContext context = TestFunctionContext.Create();
            var body = "Test String";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var request = new TestHttpRequestData(context, body: stream);

            // Act
            var result = request.ReadAsString();

            // Assert
            Assert.Equal("Test String", result);
        }

        /// <summary>
        /// Tests if ReadAsString correctly reads the body with a specified encoding.
        /// </summary>
        [Fact]
        public void ReadAsString_WithEncoding_ReadsBodyCorrectly()
        {
            // Arrange
            FunctionContext context = TestFunctionContext.Create();
            var body = "Test String";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(body));
            var request = new TestHttpRequestData(context, body: stream);

            // Act
            var result = request.ReadAsString(Encoding.ASCII);

            // Assert
            Assert.Equal("Test String", result);
        }

        /// <summary>
        /// Tests if ReadAsStringAsync throws ArgumentNullException when the HttpRequestData is null.
        /// </summary>
        [Fact]
        public async Task ReadAsStringAsync_RequestIsNull_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => HttpRequestDataExtensions.ReadAsStringAsync(null!));
        }

        /// <summary>
        /// Tests if ReadAsStringAsync correctly reads the body with the default UTF-8 encoding.
        /// </summary>
        [Fact]
        public async Task ReadAsStringAsync_BodyIsNotNull_ReadsBodyCorrectly()
        {
            // Arrange
            FunctionContext context = TestFunctionContext.Create();
            var body = "Test String";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var request = new TestHttpRequestData(context, body: stream);

            // Act
            var result = await request.ReadAsStringAsync();

            // Assert
            Assert.Equal("Test String", result);
        }

        /// <summary>
        /// Tests if ReadAsStringAsync correctly reads the body with a specified encoding.
        /// </summary>
        [Fact]
        public async Task ReadAsStringAsync_WithEncoding_ReadsBodyCorrectly()
        {
            // Arrange
            FunctionContext context = TestFunctionContext.Create();
            var body = "Test String";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(body));
            var request = new TestHttpRequestData(context, body: stream);

            // Act
            var result = await request.ReadAsStringAsync(Encoding.ASCII);

            // Assert
            Assert.Equal("Test String", result);
        }

        [Fact]
        public async Task ReadAsJsonAsync_SimpleOverload_AppliesDefaults()
        {
            FunctionContext context = TestFunctionContext.Create();

            var body = "{\"textjsonname\":\"Test\",\"textjsonint\":42}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var request = new TestHttpRequestData(context, body: stream);

            RequestPoco result = await request.ReadFromJsonAsync<RequestPoco>();

            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.SomeInt);
        }

        [Fact]
        public async Task ReadAsJsonAsync_SerializerOverload_AppliesSerializer()
        {
            FunctionContext context = TestFunctionContext.Create();

            var body = "{\"jsonnetname\":\"Test\",\"jsonnetint\":42}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var request = new TestHttpRequestData(context, body: stream);

            RequestPoco result = await request.ReadFromJsonAsync<RequestPoco>(new NewtonsoftJsonObjectSerializer());

            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.SomeInt);
        }

        public class RequestPoco
        {
            [JsonProperty("jsonnetname")]
            [JsonPropertyName("textjsonname")]
            public string Name { get; set; }

            [JsonProperty("jsonnetint")]
            [JsonPropertyName("textjsonint")]
            public int SomeInt { get; set; }
        }
    }
}
