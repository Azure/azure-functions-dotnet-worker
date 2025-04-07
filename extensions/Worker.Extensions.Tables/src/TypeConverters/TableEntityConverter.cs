// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.Config;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.TypeConverters
{
    /// <summary>
    /// Converter to bind <see cref="TableEntity" /> type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(TableEntity))]
    [SupportedTargetType(typeof(ITableEntity))]
    internal class TableEntityConverter : TableConverterBase<ITableEntity>
    {
        public TableEntityConverter(IOptions<WorkerOptions> workerOptions, IOptionsMonitor<TablesBindingOptions> tableOptions, ILogger<TableEntityConverter> logger)
            : base(workerOptions, tableOptions, logger)
        {
        }

        public override async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            try
            {
                if (!CanConvert(context))
                {
                    return ConversionResult.Unhandled();
                }
                if (context.TargetType != typeof(TableEntity) && context.TargetType != typeof(TableEntity) && !typeof(ITableEntity).IsAssignableFrom(context.TargetType))
                {
                    return ConversionResult.Unhandled();
                }

                var modelBindingData = context?.Source as ModelBindingData;
                var tableData = GetBindingDataContent(modelBindingData);
                var result = await ConvertModelBindingData(context.TargetType, tableData);

                if (result is null)
                {
                    return ConversionResult.Failed(new InvalidOperationException($"Unable to convert table binding data to type '{context.TargetType.Name}'."));
                }

                return ConversionResult.Success(result);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private async Task<object?> ConvertModelBindingData(Type targetType, TableData content)
        {
            if (string.IsNullOrEmpty(content.TableName))
            {
                throw new ArgumentNullException(nameof(content.TableName));
            }

            if (string.IsNullOrEmpty(content.PartitionKey))
            {
                throw new ArgumentNullException(nameof(content.PartitionKey));
            }

            if (string.IsNullOrEmpty(content.RowKey))
            {
                throw new ArgumentNullException(nameof(content.RowKey));
            }

            return await ToTargetTypeAsync(targetType, content);
        }

        private async Task<object?> ToTargetTypeAsync(Type targetType, TableData content) => targetType switch
        {
            Type _ when targetType == typeof(TableEntity) => await GetTableEntityAsync(content),
            Type _ when typeof(ITableEntity).IsAssignableFrom(targetType) => await DeserializeToTargetEntityAsync(targetType, content)
        };

        private async Task<TableEntity> GetTableEntityAsync(TableData content)
        {
            var tableClient = GetTableClient(content.Connection, content.TableName!);
            var tableEntity = await tableClient.GetEntityAsync<TableEntity>(content.PartitionKey, content.RowKey);
            return (TableEntity)tableEntity;
        }

        private async Task<object?> DeserializeToTargetEntityAsync(Type targetType, TableData content)
        {
            TableEntity tableEntity = await GetTableEntityAsync(content);
            return DeserializeToTargetObject(targetType, tableEntity);
        }
    }
}
