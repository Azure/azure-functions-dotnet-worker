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
        // keep
        public async static Task DeleteTableEntity(string partitionKey, string rowKey)
        {
            _ = await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}
