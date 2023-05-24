// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    /// Converter to bind Table client parameter.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedConverterType(typeof(TableClient))]
    internal class TableClientConverter: TableConverterBase<TableClient>
    {
        public TableClientConverter(IOptionsSnapshot<TablesBindingOptions> tableOptions, ILogger<TableClientConverter> logger)
            : base(tableOptions, logger)
        {
        }

        public override async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (!CanConvert(context))
            {
                return ConversionResult.Unhandled();
            }
            try
            {
                var modelBindingData = context?.Source as ModelBindingData;
                Dictionary<string, object> content = GetBindingDataContent(modelBindingData);

                var tableData = GetTableData(content);

                var result = await ConvertModelBindingData(tableData);

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

        private Task<TableClient> ConvertModelBindingData(TableData content)
        {
            if (string.IsNullOrEmpty(content.TableName))
            {
                throw new ArgumentNullException(nameof(content.TableName));
            }

            return Task.FromResult(GetTableClient(content.Connection, content.TableName!));
        }
    }
}
