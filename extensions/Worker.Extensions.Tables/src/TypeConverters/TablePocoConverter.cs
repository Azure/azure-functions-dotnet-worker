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
using System.Threading;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.TypeConverters
{
    /// <summary>
    /// Converter to bind <see cref="object" /> type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    internal sealed class TablePocoConverter : TableConverterBase<object>
    {
        private readonly IOptions<WorkerOptions> _workerOptions;

        public TablePocoConverter(IOptions<WorkerOptions> workerOptions, IOptionsMonitor<TablesBindingOptions> tableOptions, ILogger<TablePocoConverter> logger)
            : base(tableOptions, logger)
        {
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
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
                var result = await ConvertModelBindingDataAsync(tableData, context!.TargetType);

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

        private async Task<object?> ConvertModelBindingDataAsync(TableData content, Type targetType)
        {
            if (string.IsNullOrEmpty(content.TableName))
            {
                throw new ArgumentNullException(nameof(content.TableName));
            }

            if (string.IsNullOrEmpty(content.PartitionKey))
            {
                throw new ArgumentNullException(nameof(content.PartitionKey));
            }

            var tableClient = GetTableClient(content.Connection, content.TableName!);

            if (targetType.IsCollectionType())
            {
                IEnumerable<TableEntity> tableEntities = await GetEnumerableTableEntityAsync(content);
                return DeserializeToTargetObject(targetType, tableEntities);
            }
            else 
            {
                if (string.IsNullOrEmpty(content.RowKey))
                {
                    throw new ArgumentNullException(nameof(content.RowKey));
                }

                TableEntity tableEntity = await tableClient.GetEntityAsync<TableEntity>(content.PartitionKey, content.RowKey);
                return DeserializeToTargetObject(targetType, tableEntity);
            }
        }

        private object? DeserializeToTargetObject(Type targetType, object tableEntity)
        {
            string jsonString = JsonSerializer.Serialize(tableEntity);
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonString);

            var stream = new MemoryStream(byteArray);
            stream.Seek(0, SeekOrigin.Begin);

            return _workerOptions?.Value?.Serializer?.Deserialize(stream, targetType, CancellationToken.None);
        }
    }
}
