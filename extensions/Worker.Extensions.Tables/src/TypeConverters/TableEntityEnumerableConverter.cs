// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        public TableEntityEnumerableConverter(IOptionsMonitor<TablesBindingOptions> tableOptions, ILogger<TableEntityEnumerableConverter> logger)
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
                if (context.TargetType != typeof(IEnumerable<TableEntity>))
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
    }
}

