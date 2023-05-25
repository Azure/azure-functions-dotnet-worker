// Copyright (c) .NET Foundation. All rights reserved.
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

        protected TableData GetBindingDataContent(ModelBindingData? bindingData)
        {
            return bindingData?.ContentType switch
            {
                Constants.JsonContentType => bindingData.Content.ToObjectFromJson<TableData>(),
                _ => throw new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported.")
            };
        }

        protected TableClient GetTableClient(string? connection, string tableName)
        {
            var tableOptions = _tableOptions.Get(connection);
            TableServiceClient tableServiceClient = tableOptions.CreateClient();
            return tableServiceClient.GetTableClient(tableName);
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
