﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.TypeConverters
{
    internal abstract class TableConverterBase<T> : IInputConverter
    {
        private readonly ILogger<TableConverterBase<T>> _logger;
        protected readonly IOptionsSnapshot<TablesBindingOptions> _tableOptions;

        public TableConverterBase(IOptionsSnapshot<TablesBindingOptions> tableOptions, ILogger<TableConverterBase<T>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tableOptions = tableOptions ?? throw new ArgumentNullException(nameof(tableOptions));
        }
        
        protected bool CanConvert(ConverterContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.TargetType != typeof(T))
            {
                return false;
            }

            if (!(context.Source is ModelBindingData bindingData))
            {
                return false;
            }

            if (bindingData.Source is not Constants.TablesExtensionName)
            {
                return false;
            }

            return true;
        }

        public abstract ValueTask<ConversionResult> ConvertAsync(ConverterContext context);

        protected Dictionary<string, object> GetBindingDataContent(ModelBindingData? bindingData)
        {
            return bindingData?.ContentType switch
            {
                Constants.JsonContentType => new Dictionary<string, object>(bindingData?.Content?.ToObjectFromJson<Dictionary<string, object>>(), StringComparer.OrdinalIgnoreCase),
                _ => throw new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported.")
            };
        }

        protected TableClient GetTableClient(string? connection, string tableName)
        {
            var tableOptions = _tableOptions.Get(connection);
            TableServiceClient tableServiceClient = tableOptions.CreateClient();
            return tableServiceClient.GetTableClient(tableName);
        }
        
        protected TableData GetTableData(Dictionary<string, object> content)
        {
            // Parse through dictionary
            content.TryGetValue(Constants.Connection, out var connection);
            content.TryGetValue(Constants.TableName, out var tableName);
            content.TryGetValue(Constants.PartitionKey, out var partitionKey);
            content.TryGetValue(Constants.RowKey, out var rowKey);
            content.TryGetValue(Constants.Take, out var take);
            content.TryGetValue(Constants.Filter, out var filter);

            TableData tableData = new()
            {
                Connection = connection?.ToString(),
                TableName = tableName?.ToString(),
                PartitionKey = partitionKey?.ToString(),
                RowKey = rowKey?.ToString(),
                Take = Convert.ToInt32(take?.ToString()),
                Filter = filter?.ToString()
            };

            return tableData;
        }

        protected class TableData
        {
            public string? TableName { get; set; }
            public string? Connection { get; set; }
            public string? PartitionKey { get; set; }
            public string? RowKey { get; set; }
            public int Take { get; set; }
            public string? Filter { get; set; }
        }
    }
}
