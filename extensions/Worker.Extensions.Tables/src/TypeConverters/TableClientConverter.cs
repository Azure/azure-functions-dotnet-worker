// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.Config;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Core;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.TypeConverters
{
    /// <summary>
    /// Converter to bind <see cref="TableClient" /> type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(TableClient))]
    internal class TableClientConverter: TableConverterBase<TableClient>
    {
        public TableClientConverter(IOptionsMonitor<TablesBindingOptions> tableOptions, ILogger<TableClientConverter> logger)
            : base(tableOptions, logger)
        {
        }

        public override ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            try
            {
                if (!CanConvert(context))
                {
                    return new(ConversionResult.Unhandled());
                }

                if (context.TargetType != typeof(TableClient))
                {
                    return new(ConversionResult.Unhandled());
                }

                var modelBindingData = context?.Source as ModelBindingData;
                var tableData = GetBindingDataContent(modelBindingData);
                var result = ConvertModelBindingData(tableData);

                return new(ConversionResult.Success(result));
            }
            catch (Exception ex)
            {
                return new(ConversionResult.Failed(ex));
            }
        }

        private TableClient ConvertModelBindingData(TableData content)
        {
            ThrowIfNull(content.TableName, nameof(content.TableName));

            return GetTableClient(content.Connection, content.TableName!);
        }
    }
}
