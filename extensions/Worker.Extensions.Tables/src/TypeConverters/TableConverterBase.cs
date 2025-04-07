// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Threading;
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
        protected readonly IOptionsMonitor<TablesBindingOptions> _tableOptions;
        private readonly IOptions<WorkerOptions> _workerOptions;

        public TableConverterBase(IOptions<WorkerOptions> workerOptions, IOptionsMonitor<TablesBindingOptions> tableOptions, ILogger<TableConverterBase<T>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tableOptions = tableOptions ?? throw new ArgumentNullException(nameof(tableOptions));
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
        }

        protected bool CanConvert(ConverterContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Source is not ModelBindingData bindingData)
            {
                return false;
            }

            if (bindingData.Source is not Constants.TablesExtensionName)
            {
                throw new InvalidBindingSourceException(bindingData.Source, Constants.TablesExtensionName);
            }

            return true;
        }

        public abstract ValueTask<ConversionResult> ConvertAsync(ConverterContext context);

        protected TableData GetBindingDataContent(ModelBindingData? bindingData)
        {
            if (bindingData is null)
            {
                throw new ArgumentNullException(nameof(bindingData));
            }

            return bindingData.ContentType switch
            {
                Constants.JsonContentType => bindingData.Content.ToObjectFromJson<TableData>(),
                _ => throw new InvalidContentTypeException(bindingData.ContentType, Constants.JsonContentType)
            };
        }

        protected TableClient GetTableClient(string? connection, string tableName)
        {
            var tableOptions = _tableOptions.Get(connection);
            TableServiceClient tableServiceClient = tableOptions.CreateClient();
            return tableServiceClient.GetTableClient(tableName);
        }

        protected object? DeserializeToTargetObject(Type targetType, object tableEntity)
        {
            string jsonString = JsonSerializer.Serialize(tableEntity);
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonString);

            var stream = new MemoryStream(byteArray);
            stream.Seek(0, SeekOrigin.Begin);

            return _workerOptions?.Value?.Serializer?.Deserialize(stream, targetType, CancellationToken.None);
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
