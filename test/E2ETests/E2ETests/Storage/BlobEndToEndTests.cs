// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Worker.E2ETests.Storage
{
    [Collection(Constants.FunctionAppCollectionName)]
    public class BlobEndToEndTests : IDisposable
    {
        private readonly IDisposable _disposeLog;
        private FunctionAppFixture _fixture;

        public BlobEndToEndTests(FunctionAppFixture fixture, ITestOutputHelper testOutput)
        {
            _fixture = fixture;
            _disposeLog = _fixture.TestLogs.UseTestLogger(testOutput);
        }

        [Fact]
        public async Task BlobTriggerToBlob_Succeeds()
        {
            string fileName = Guid.NewGuid().ToString();

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, fileName);

            //Trigger
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerInputBindingContainer, fileName);

            //Verify
            string result = await StorageHelpers.DownloadFileFromContainer(Constants.Blob.OutputBindingContainer, fileName);

            Assert.Equal("Hello World", result);
        }

        [Fact]
        public async Task BlobTrigger_Poco_Succeeds()
        {
            string fileName = Guid.NewGuid().ToString();

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Trigger
            var json = JsonSerializer.Serialize(new { text = "Hello World" });
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerPocoContainer, fileName, json);

            //Verify
            string result = await StorageHelpers.DownloadFileFromContainer(Constants.Blob.OutputPocoContainer, fileName);

            Assert.Equal(json, result);
        }

        [Fact]
        public async Task BlobTrigger_String_Succeeds()
        {
            string fileName = Guid.NewGuid().ToString();

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Trigger
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerStringContainer, fileName);

            //Verify
            string result = await StorageHelpers.DownloadFileFromContainer(Constants.Blob.OutputStringContainer, fileName);

            Assert.Equal("Hello World", result);
        }

        [Fact(Skip = "TODO: https://github.com/Azure/azure-functions-dotnet-worker/issues/1935")]
        public async Task BlobTrigger_Stream_Succeeds()
        {
            string key = "StreamTriggerOutput: ";
            string fileName = Guid.NewGuid().ToString();

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Trigger
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerStreamContainer, fileName);

            //Verify
            IEnumerable<string> logs = null;
            await TestUtility.RetryAsync(() =>
            {
                logs = _fixture.TestLogs.CoreToolsLogs.Where(p => p.Contains(key));
                return Task.FromResult(logs.Count() >= 1);
            });

            var lastLog = logs.Last();
            int subStringStart = lastLog.LastIndexOf(key) + key.Length;
            var result = lastLog[subStringStart..];

            Assert.Equal("Hello World", result);
        }

        [Fact(Skip = "TODO: https://github.com/Azure/azure-functions-dotnet-worker/issues/1910")]
        public async Task BlobTrigger_BlobClient_Succeeds()
        {
            string key = "BlobClientTriggerOutput: ";
            string fileName = Guid.NewGuid().ToString();

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Trigger
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerBlobClientContainer, fileName);

            //Verify
            IEnumerable<string> logs = null;
            await TestUtility.RetryAsync(() =>
            {
                logs = _fixture.TestLogs.CoreToolsLogs.Where(p => p.Contains(key));
                return Task.FromResult(logs.Count() >= 1);
            });

            var lastLog = logs.Last();
            int subStringStart = lastLog.LastIndexOf(key) + key.Length;
            var result = lastLog[subStringStart..];

            Assert.Equal("Hello World", result);
        }

        [Theory]
        [InlineData("BlobInputClientTest")]
        [InlineData("BlobInputBlockClientTest")]
        [InlineData("BlobInputAppendClientTest")]
        [InlineData("BlobInputPageClientTest")]
        [InlineData("BlobInputBaseClientTest")]
        [InlineData("BlobInputContainerClientTest")]
        [InlineData("BlobInputStreamTest")]
        [InlineData("BlobInputByteTest")]
        [InlineData("BlobInputStringTest")]
        public async Task BlobInput_SingleCardinality_Succeeds(string functionName)
        {
            string expectedMessage = "Hello World";

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile", expectedMessage);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionName);
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Contains(expectedMessage, actualMessage);
        }

        [Fact]
        public async Task BlobInput_Poco_Succeeds()
        {
            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            var json = JsonSerializer.Serialize(new { id = "1", name = "To Kill a Mockingbird" });
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile", json);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("BlobInputPocoTest");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            string expectedMessage = "To Kill a Mockingbird";
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Contains(expectedMessage, actualMessage);
        }

        [Theory]
        [InlineData("BlobInputClientArrayTest")]
        [InlineData("BlobInputClientEnumerableTest")]
        [InlineData("BlobInputStreamArrayTest")]
        [InlineData("BlobInputStreamEnumerableTest")]
        [InlineData("BlobInputBytesArrayTest")]
        [InlineData("BlobInputBytesEnumerableTest")]
        [InlineData("BlobInputStringArrayTest")]
        [InlineData("BlobInputStringEnumerableTest")]
        public async Task BlobInput_BlobCollection_Succeeds(string functionName)
        {
            string expectedMessage = "ABC, DEF, GHI";
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile1", "ABC");
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile2", "DEF");
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile3", "GHI");

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionName);
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedMessage, actualMessage);
        }

        [Theory]
        [InlineData("BlobInputPocoArrayTest")]
        [InlineData("BlobInputPocoEnumerableTest")]
        public async Task BlobInput_PocoCollection_Succeeds(string functionName)
        {
            string book1 = $@"{{ ""id"": ""1"", ""name"": ""To Kill a Mockingbird""}}";
            string book2 = $@"{{ ""id"": ""2"", ""name"": ""Of Mice and Men""}}";
            string book3 = $@"{{ ""id"": ""3"", ""name"": ""The Wind in the Willows""}}";

            string expectedMessage = "To Kill a Mockingbird, Of Mice and Men, The Wind in the Willows";
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;


            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "book1", book1);
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "book2", book2);
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "book3", book3);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionName);
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedMessage, actualMessage);
        }

        [Theory]
        [InlineData("BlobInputStringArraySingleBlobTest")]
        [InlineData("BlobInputStringEnumerableSingleBlobTest")]
        public async Task BlobInput_StringCollection_WithSingleBlobFile_Succeeds(string functionName)
        {
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            var fileContent = JsonSerializer.Serialize(new string[] { "Hello", "World" });
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile", fileContent);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionName);
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal("Hello, World", actualMessage);
        }

        [Theory]
        [InlineData("BlobInputBytesArraySingleBlobTest")]
        [InlineData("BlobInputBytesEnumerableSingleBlobTest")]
        public async Task BlobInput_ByteArrayCollection_WithSingleBlobFile_Succeeds(string functionName)
        {
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            var data = new List<byte[]> { Encoding.UTF8.GetBytes("Item1"), Encoding.UTF8.GetBytes("Item2") };
            var fileContent = JsonSerializer.Serialize(data);
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile", fileContent);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionName);
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal("Item1, Item2", actualMessage);
        }


        [Theory]
        [InlineData("BlobInputPocoArraySingleBlobTest")]
        [InlineData("BlobInputPocoEnumerableSingleBlobTest")]
        public async Task BlobInput_PocoCollection_WithSingleBlobFile_Succeeds(string functionName)
        {
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            var data = new List<object> { new { id = "1", name = "To Kill a Mockingbird" }, new { id = "2", name = "Of Mice and Men" } };
            var fileContent = JsonSerializer.Serialize(data);
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile", fileContent);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionName);
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal("To Kill a Mockingbird, Of Mice and Men", actualMessage);
        }

        [Fact]
        public async Task BlobInput_BlobCollection_WithSubdirectory_Succeeds()
        {
            string expectedMessage = "ABC, GHI";
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "test/file1", "ABC", true);
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "file2", "DEF");
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "test/file3", "GHI", true);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("BlobInputClientCollectionWithSubdirectoryTest");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedMessage, actualMessage);
        }

        public void Dispose()
        {
            _disposeLog?.Dispose();
        }
    }
}
