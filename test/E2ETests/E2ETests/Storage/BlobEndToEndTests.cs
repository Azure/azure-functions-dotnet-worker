// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Functions.Tests.E2ETests.Storage
{
    [Collection(Constants.FunctionAppCollectionName)]
    public class BlobEndToEndTests
    {
        private FunctionAppFixture _fixture;

        public BlobEndToEndTests(FunctionAppFixture fixture)
        {
            _fixture = fixture;
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

        [Theory]
        [InlineData(Constants.Blob.TriggerBlobContainerClientContainer, Constants.Blob.OutputBlobContainerClientContainer)]
        [InlineData(Constants.Blob.TriggerBlobContainerClientContainer, Constants.Blob.OutputBlobContainerClientContainer)]
        [InlineData(Constants.Blob.TriggerBlobClientContainer, Constants.Blob.OutputBlobClientContainer)]
        [InlineData(Constants.Blob.TriggerStreamContainer, Constants.Blob.OutputStreamContainer)]
        [InlineData(Constants.Blob.TriggerStringContainer, Constants.Blob.OutputStringContainer)]
        [InlineData(Constants.Blob.TriggerPocoContainer, Constants.Blob.OutputPocoContainer)]
        [InlineData()]
        public async Task BlobTrigger_Succeeds(string triggerContainer, string outputContainer)
        {
            string fileName = Guid.NewGuid().ToString();

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Trigger
            await StorageHelpers.UploadFileToContainer(triggerContainer, fileName);

            //Verify
            string result = await StorageHelpers.DownloadFileFromContainer(outputContainer, fileName);

            Assert.Equal("Hello World", result);
        }

        [Theory]
        [InlineData("BlobInputClientTest")]
        [InlineData("BlobInputStreamTest")]
        [InlineData("BlobInputByteTest")]
        [InlineData("BlobInputStringTest")]
        [InlineData("BlobInputBookArrayTest")]
        [InlineData("BlobInputCollectionTest")]
        [InlineData("BlobInputStringArrayTest")]
        public async Task BlobInput_Succeeds(string functionName)
        {
            string fileName = "testFile";
            string expectedMessage = "Hello World";
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, fileName, expectedMessage);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger(functionName);
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Contains(expectedMessage, actualMessage);
        }

        [Fact]
        public async Task BlobInput_Poco_Succeeds()
        {
            string fileContent = $@"{{ ""id"": ""1"", ""name"": ""To Kill a Mockingbird""}}";
            string expectedMessage = "To Kill a Mockingbird";
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile", fileContent);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("BlobInputPocoTest");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Contains(expectedMessage, actualMessage);
        }

        [Fact]
        public async Task BlobInput_BlobClientCollection_Succeeds()
        {
            string expectedMessage = "testFile1, testFile2, testFile3";
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            //Cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile1");
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile2");
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, "testFile3");

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("BlobInputCollectionTest");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Contains(expectedMessage, actualMessage);
        }

        [Fact]
        public async Task BlobInput_StringCollection_Succeeds()
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
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("BlobInputCollectionTest");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Contains(expectedMessage, actualMessage);
        }

        [Fact]
        public async Task BlobInput_PocoCollection_Succeeds()
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
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("BlobInputPocoArrayTest");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Contains(expectedMessage, actualMessage);
        }
    }
}
