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
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.TypeConverters
{
    /// <summary>
    /// Converter to bind <see cref="TableEntity" /> type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    internal class TableObjectConverter : TableConverterBase<object>
    {
        private readonly IOptions<WorkerOptions> _workerOptions;

        public TableObjectConverter(IOptions<WorkerOptions> workerOptions, IOptionsMonitor<TablesBindingOptions> tableOptions, ILogger<TableObjectConverter> logger)
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
                var result = await ConvertModelBindingData(tableData, context.TargetType);

                return ConversionResult.Success(result);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private async Task<object?> ConvertModelBindingData(TableData content, Type targetType)
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

            return await GetTableEntity(content, targetType);
        }

        private async Task<object?> GetTableEntity(TableData content, Type targetType)
        {
            var tableClient = GetTableClient(content.Connection, content.TableName!);
            var tableEntity = await tableClient.GetEntityAsync<TableEntity>(content.PartitionKey, content.RowKey);

            var value = DeserializeToTargetObjectAsync(targetType, tableEntity);
            return value;
        }

        private object? DeserializeToTargetObjectAsync(Type targetType, TableEntity tableEntity)
        {
            // Serialize the TableEntity to JSON
            string jsonString = JsonSerializer.Serialize(tableEntity);

            // Convert JSON string to byte array
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonString);

            // Return the byte array as a MemoryStream
            var stream = new MemoryStream(byteArray);
            stream.Seek(0, SeekOrigin.Begin);

            return _workerOptions?.Value?.Serializer?.Deserialize(stream, targetType, CancellationToken.None);
        }
    }
}
