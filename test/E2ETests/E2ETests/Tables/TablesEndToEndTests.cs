// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
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
        public async Task Read_TableClient_Data_Succeeds()
        {
            const string partitionKey = "Partition";
            const string firstRowKey = "FirstRowKey";
            const string firstValue = "FirstValue";

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
            const string partitionKey = "Partition";
            const string firstRowKey = "FirstRowKey";
            const string firstValue = "FirstValue";

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
            const string partitionKey = "Partition";
            const string firstRowKey = "FirstRowKey";
            const string firstValue = "FirstValue";
            const string secondRowKey = "SecondRowKey";
            const string secondValue = "SecondValue";
            const string thirdRowKey = "ThirdRowKey";
            const string thirdValue = "ThirdValue";

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
            const string partitionKey = "Partition";
            const string firstRowKey = "FirstRowKey";
            const string firstValue = "FirstValue";
            const string secondRowKey = "SecondRowKey";
            const string secondValue = "SecondValue";

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

        public void Dispose()
        {
            // Cleanup
            TableHelpers.DeleteTable().GetAwaiter().GetResult();
            _disposeLog?.Dispose();
        }
    }
}
