// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Tests.E2ETests.Tables
{
    [Collection(Constants.FunctionAppCollectionName)]
    public class TablesEndToEndTests : IDisposable
    {
        private readonly IDisposable _disposeLog;
        private FunctionAppFixture _fixture;

        public TablesEndToEndTests(FunctionAppFixture fixture, ITestOutputHelper testOutput)
        {
            _fixture = fixture;
            _disposeLog = _fixture.TestLogs.UseTestLogger(testOutput);
        }

        [Fact]
        public async Task TableInput_Succeeds()
        {
            var partitionKey = "Partition";
            var rowKey = "First Row Key";
            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("TableInputClientFunction", $"/items/{partitionKey}/{rowKey}/firstValue");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(expectedStatusCode, response.StatusCode);

            // Add another row to same partition
            var secondRowKey = "Second Row Key";
            //Trigger
            HttpResponseMessage secondResponse = await HttpHelpers.InvokeHttpTrigger("TableInputClientFunction", $"items/{partitionKey}/{secondRowKey}/secondValue");
            string secondActualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            HttpStatusCode secondExpectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(secondExpectedStatusCode, response.StatusCode);
        }

        [Fact]
        public async Task Read_TableData_Succeeds()
        {
            var partitionKey = "Partition";
            var rowKey = "First Row Key";
            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("ReadTableDataFunction", $"/items/{partitionKey}/{rowKey}");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal("FirstValue", actualMessage);
        }

        [Fact]
        public async Task Read_TableData_With_Filter()
        {
            var rowKey = "Second Row Key";
            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("ReadTableDataFunctionWithFilter", $"items/{rowKey}");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal("SecondValue", actualMessage);
        }

        [Fact]
        public async Task EnumerableFunction_Succeeds()
        {
            var partitionKey = "Partition";
            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("EnumerableFunction", $"items/{partitionKey}");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal("FirstValue, SecondValue", actualMessage);
        }

        public async void Dispose()
        {
            // Cleanup
            await TableHelpers.DeleteTableEntity("Partition", "FirstRowKey");
            await TableHelpers.DeleteTableEntity("Partition", "SecondRowKey");
            _disposeLog?.Dispose();
        }
    }
}
