using System.Threading.Tasks;
using Azure.Data.Tables;

namespace Microsoft.Azure.Functions.Worker.E2ETests.Helpers
{
    public static class TableHelpers
    {
        private static readonly TableClient _tableClient;

        static TableHelpers()
        {
            var tableName = Constants.Tables.TableName;
            _tableClient = new TableClient(Constants.Tables.TablesConnectionStringSetting, tableName);
        }

        public async static Task CreateTable()
        {
            await _tableClient.CreateIfNotExistsAsync();
        }

        public async static Task DeleteTable()
        {
            await _tableClient.DeleteAsync();
        }

        public async static Task CreateTableEntity(string partitionKey, string rowKey, string value)
        {
            var tableEntity = new TableEntity(partitionKey, rowKey);
            tableEntity.Add("Text", value);
            await _tableClient.AddEntityAsync(tableEntity);
        }
    }
}
