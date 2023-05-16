// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.Tables;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.Config;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind Table type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedConverterType(typeof(TableClient))]
    [SupportedConverterType(typeof(TableEntity))]
    [SupportedConverterType(typeof(IEnumerable<TableEntity>))]
    internal class TableConverter : IInputConverter
    {
        private readonly ILogger<TableConverter> _logger;
        private readonly IOptionsSnapshot<TablesBindingOptions> _tableOptions;

        public TableConverter(IOptionsSnapshot<TablesBindingOptions> tableOptions, ILogger<TableConverter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tableOptions = tableOptions ?? throw new ArgumentNullException(nameof(tableOptions));
        }

        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            return context?.Source switch
            {
                ModelBindingData binding => await ConvertFromBindingDataAsync(context, binding),
                _ => ConversionResult.Unhandled()
            };
        }

        public virtual async ValueTask<ConversionResult> ConvertFromBindingDataAsync(ConverterContext context, ModelBindingData modelBindingData)
        {
            if (!IsTableExtension(modelBindingData))
            {
                return ConversionResult.Unhandled();
            }
            try
            {
                Dictionary<string, object> content = GetBindingDataContent(modelBindingData);
                var result = await ConvertModelBindingDataAsync(content, context.TargetType, modelBindingData);

                if (result is not null)
                {
                    return ConversionResult.Success(result);
                }
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }

            return ConversionResult.Unhandled();
        }

        internal bool IsTableExtension(ModelBindingData bindingData)
        {
            if (bindingData?.Source is not Constants.TablesExtensionName)
            {
                _logger.LogTrace("Source '{source}' is not supported by {converter}", bindingData?.Source, nameof(TableConverter));
                return false;
            }

            return true;
        }

        public virtual async Task<object?> ConvertModelBindingDataAsync(IDictionary<string, object> content, Type targetType, ModelBindingData bindingData)
        {
            content.TryGetValue(Constants.Connection, out var connection);
            content.TryGetValue(Constants.TableName, out var tableName);
            content.TryGetValue(Constants.PartitionKey, out var partitionKey);
            content.TryGetValue(Constants.RowKey, out var rowKey);
            content.TryGetValue(Constants.Take, out var take);
            content.TryGetValue(Constants.Filter, out var filter);


            if (string.IsNullOrEmpty(tableName?.ToString()))
            {
                throw new ArgumentNullException("'TableName' cannot be null or empty");
            }

            return await ToTargetTypeAsync(targetType, connection?.ToString() ?? null, tableName!.ToString(), partitionKey?.ToString() ?? null, rowKey?.ToString() ?? null, Convert.ToInt32(take?.ToString()), filter?.ToString() ?? null);
        }

        internal Dictionary<string, object> GetBindingDataContent(ModelBindingData bindingData)
        {
            return bindingData?.ContentType switch
            {
                Constants.JsonContentType => new Dictionary<string, object>(bindingData?.Content?.ToObjectFromJson<Dictionary<string, object>>(), StringComparer.OrdinalIgnoreCase),
                _ => throw new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported.")
            };
        }

        internal virtual async Task<object?> ToTargetTypeAsync(Type targetType, string? connection, string tableName, string? partitionKey, string? rowKey, int take, string? filter) => targetType switch
        {
            Type _ when targetType == typeof(TableClient) => GetTableClient(connection, tableName),
            Type _ when targetType == typeof(TableEntity) => await GetTableEntity(connection, tableName, partitionKey, rowKey),
            Type _ when targetType == typeof(IEnumerable<TableEntity>) => await GetEnumerableTableEntity(connection, tableName, partitionKey, rowKey, take, filter),
            _ => null
        };

        internal virtual TableClient GetTableClient(string? connection, string tableName)
        {
            var tableOptions = _tableOptions.Get(connection);
            TableServiceClient tableServiceClient = tableOptions.CreateClient();
            return tableServiceClient.GetTableClient(tableName);
        }

        internal virtual async Task<IEnumerable<TableEntity>> GetEnumerableTableEntity(string? connection, string tableName, string? partitionKey, string? rowKey, int take, string? filter)
        {
            if (rowKey != null && (take > 0 || filter != null))
            {
                throw new ArgumentNullException($"Row key {rowKey} cannot have a value if {take} or {filter} are defined");
            }
            var tableClient = GetTableClient(connection, tableName);

            if (!string.IsNullOrEmpty(partitionKey))
            {
                var partitionKeyPredicate = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey}");
                filter = !string.IsNullOrEmpty(filter) ? $"{partitionKeyPredicate} and {filter}" : partitionKeyPredicate;
            }

            int? maxPerPage = null;
            if (take > 0)
            {
                maxPerPage = take;
            }

            int countRemaining = take;

            var entities = tableClient.QueryAsync<TableEntity>(
               filter: filter,
               maxPerPage: maxPerPage).ConfigureAwait(false);

            List<TableEntity> bindingDataContent = new List<TableEntity>();

            await foreach (var entity in entities)
            {
                countRemaining--;
                bindingDataContent.Add(entity);
                if (countRemaining == 0)
                {
                    break;
                }
            }
            return bindingDataContent;
        }

        internal virtual async Task<TableEntity> GetTableEntity(string? connection, string tableName, string? partitionKey, string? rowKey)
        {
            if (partitionKey == null)
            {
                throw new ArgumentNullException($"Partition key {partitionKey} cannot be null");
            }
            if (rowKey == null)
            {
                throw new ArgumentNullException($"Row key {rowKey} cannot be null");
            }
            var tableClient = GetTableClient(connection, tableName);
            return await tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);

        }
    }
}
