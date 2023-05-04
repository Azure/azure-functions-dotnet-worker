using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Functions.Tests.E2ETests;

namespace Microsoft.Azure.Functions.Worker.E2ETests.Helpers
{
    public static class TableHelpers
    {
        private static readonly TableClient _tableClient;
        static TableHelpers()
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder
            {
                ConnectionString = Constants.Tables.TablesConnectionStringSetting
            };
            var tableName = Constants.Tables.TableName;
            _tableClient = new TableClient(Constants.Tables.TablesConnectionStringSetting, tableName);

        }

        public async static Task CreateTable()
        {
            _ = await _tableClient.CreateIfNotExistsAsync();
        }

        public async static Task DeleteTable()
        {
            _ = await _tableClient.DeleteAsync();
        }

        // keep
        public async static Task CreateTableEntity(string partitionKey, string rowKey, string value)
        {
            var tableEntity = new TableEntity(partitionKey, rowKey);
            tableEntity.Add("Text", value);
            _ = await _tableClient.AddEntityAsync(tableEntity);
        }

        // keep
        public async static Task DeleteTableEntity(string partitionKey, string rowKey)
        {
            _ = await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}
