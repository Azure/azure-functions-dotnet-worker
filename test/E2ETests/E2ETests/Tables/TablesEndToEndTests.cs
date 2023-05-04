// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Functions.Worker.E2ETests.Helpers;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Tests.E2ETests.Tables
{
    [Collection(Constants.FunctionAppCollectionName)]
    public class TablesEndToEndTests : IDisposable
    {
        private readonly IDisposable _disposeLog;
        private FunctionAppFixture _fixture;
        private const string partitionKey = "Partition";
        private const string firstRowKey = "FirstRowKey";
        private const string firstValue = "FirstValue";
        private const string secondRowKey = "SecondRowKey";
        private const string secondValue = "SecondValue";
        private const string thirdRowKey = "ThirdRowKey";
        private const string thirdValue = "ThirdValue";

        public TablesEndToEndTests(FunctionAppFixture fixture, ITestOutputHelper testOutput)
        {
            _fixture = fixture;
            _disposeLog = _fixture.TestLogs.UseTestLogger(testOutput);
        }

        [Fact]
        public async Task Read_TableClient_Data_Succeeds()
        {
            // Create the table if it doesn't exist
            await TableHelpers.CreateTable();

            // Add table entity
            await TableHelpers.CreateTableEntity(partitionKey, firstRowKey, firstValue);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger("TableClientFunction");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(firstValue, actualMessage);

            // Delete table
            await TableHelpers.DeleteTable();
        }

        [Fact]
        public async Task Read_TableData_Succeeds()
        {
            // Create the table if it doesn't exist
            await TableHelpers.CreateTable();

            // Add table entity
            await TableHelpers.CreateTableEntity(partitionKey, firstRowKey, firstValue);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger($"ReadTableDataFunction/items/{partitionKey}/{firstRowKey}");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(firstValue, actualMessage);

            // Delete table
            await TableHelpers.DeleteTable();
        }

        [Fact]
        public async Task Read_TableData_With_Filter_And_Take()
        {
            // Create table
            await TableHelpers.CreateTable();

            // Add table entity
            await TableHelpers.CreateTableEntity(partitionKey, firstRowKey, firstValue);
            await TableHelpers.CreateTableEntity(partitionKey, secondRowKey, secondValue);
            await TableHelpers.CreateTableEntity(partitionKey, thirdRowKey, thirdValue);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger($"ReadTableDataFunctionWithFilter/items/{partitionKey}/{secondRowKey}");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal($"{firstValue},{thirdValue}", actualMessage);

            // Delete table
            await TableHelpers.DeleteTable();
        }

        [Fact]
        public async Task EnumerableFunction_Succeeds()
        {
            // Create table
            await TableHelpers.CreateTable();

            // Add table entity
            await TableHelpers.CreateTableEntity(partitionKey, firstRowKey, firstValue);
            await TableHelpers.CreateTableEntity(partitionKey, secondRowKey, secondValue);

            //Trigger
            HttpResponseMessage response = await HttpHelpers.InvokeHttpTrigger($"EnumerableFunction/items/{partitionKey}");
            string actualMessage = await response.Content.ReadAsStringAsync();

            //Verify
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK;

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal($"{firstValue},{secondValue}", actualMessage);

            // Delete table
            await TableHelpers.DeleteTable();
        }

        public async void Dispose()
        {
            // Cleanup
            await TableHelpers.DeleteTable();
            _disposeLog?.Dispose();
        }
    }
}
