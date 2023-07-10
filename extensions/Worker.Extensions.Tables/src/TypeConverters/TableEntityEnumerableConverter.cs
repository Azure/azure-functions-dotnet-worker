// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    /// Converter to bind <see cref="IEnumerable{T}" /> of type <see cref="TableEntity"/> parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(IEnumerable<TableEntity>))]
    internal class TableEntityEnumerableConverter : TableConverterBase<IEnumerable<TableEntity>>
    {
        public TableEntityEnumerableConverter(IOptionsSnapshot<TablesBindingOptions> tableOptions, ILogger<TableEntityEnumerableConverter> logger)
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

        private async Task<IEnumerable<TableEntity>> ConvertModelBindingData(TableData content)
        {
            if (string.IsNullOrEmpty(content.TableName))
            {
                throw new ArgumentNullException(nameof(content.TableName));
            }

            if (content.RowKey != null && (content.Take > 0 || content.Filter != null))
            {
                throw new InvalidOperationException($"Row key {content.RowKey} cannot have a value if {content.Take} or {content.Filter} are defined");
            }

            return await GetEnumerableTableEntity(content);
        }

        private async Task<IEnumerable<TableEntity>> GetEnumerableTableEntity(TableData content)
        {
            var tableClient = GetTableClient(content.Connection, content.TableName!);
            string? filter = content.Filter;

            if (!string.IsNullOrEmpty(content.PartitionKey))
            {
                var partitionKeyPredicate = TableClient.CreateQueryFilter($"PartitionKey eq {content.PartitionKey}");
                filter = !string.IsNullOrEmpty(content.Filter) ? $"{partitionKeyPredicate} and {content.Filter}" : partitionKeyPredicate;
            }

            int? maxPerPage = null;
            if (content.Take > 0)
            {
                maxPerPage = content.Take;
            }

            int countRemaining = content.Take;

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
    }
}

