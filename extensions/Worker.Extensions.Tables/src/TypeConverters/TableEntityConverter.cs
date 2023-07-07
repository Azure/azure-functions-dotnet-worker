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
    internal class TableEntityConverter : TableConverterBase<TableEntity>
    {
        public TableEntityConverter(IOptionsSnapshot<TablesBindingOptions> tableOptions, ILogger<TableEntityConverter> logger)
            : base(tableOptions, logger)
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

                var modelBindingData = context?.Source as ModelBindingData;
                var tableData = GetBindingDataContent(modelBindingData);
                var result = await ConvertModelBindingData(tableData);

                return ConversionResult.Success(result);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private async Task<TableEntity> ConvertModelBindingData(TableData content)
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

            return await GetTableEntity(content);
        }

        private async Task<TableEntity> GetTableEntity(TableData content)
        {
            var tableClient = GetTableClient(content.Connection, content.TableName!);
            return await tableClient.GetEntityAsync<TableEntity>(content.PartitionKey, content.RowKey);
        }
    }
}
