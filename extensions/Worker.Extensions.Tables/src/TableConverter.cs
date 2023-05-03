// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.Tables;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.Config;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind Blob Storage type parameters.
    /// </summary>
    internal class TableConverter : IInputConverter
    {
        private readonly ILogger<TableConverter> _logger;
        private readonly IOptionsSnapshot<TablesBindingOptions> _tableOptions;

        public TableConverter(IOptionsSnapshot<TablesBindingOptions> tableOptions, ILogger<TableConverter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tableOptions = tableOptions ?? throw new ArgumentNullException(nameof(tableOptions));
        }

        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            return context?.Source switch
            {
                ModelBindingData binding => await ConvertFromBindingDataAsync(context, binding),
                CollectionModelBindingData binding => await ConvertFromCollectionBindingDataAsync(context, binding),
                _ => ConversionResult.Unhandled()
            };
        }

        public virtual async ValueTask<ConversionResult> ConvertFromBindingDataAsync(ConverterContext context, ModelBindingData modelBindingData)
        {
            if (!IsTableExtension(modelBindingData))
            {
                return ConversionResult.Unhandled();
            }
            try
            {
                Dictionary<string, object> content = GetBindingDataContent(modelBindingData);
                var result = await ConvertModelBindingDataAsync(content, context.TargetType, modelBindingData);

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

        internal virtual async ValueTask<ConversionResult> ConvertFromCollectionBindingDataAsync(ConverterContext context, CollectionModelBindingData collectionModelBindingData)
        {
            var tableCollection = new List<object>(collectionModelBindingData.ModelBindingDataArray.Length);
            Type elementType = context.TargetType.IsArray ? context.TargetType.GetElementType() : context.TargetType.GenericTypeArguments[0];
            
            try
            {
                foreach (ModelBindingData modelBindingData in collectionModelBindingData.ModelBindingDataArray)
                {
                    if (!IsTableExtension(modelBindingData))
                    {
                        return ConversionResult.Unhandled();
                    }

                    Dictionary<string, object> content = GetBindingDataContent(modelBindingData);
                    var element = await ConvertModelBindingDataAsync(content, elementType, modelBindingData);

                    if (element is not null)
                    {
                        tableCollection.Add(element);
                    }
                }

                var result = ToTargetTypeCollection(tableCollection, "CloneToEnumerable", elementType);

                return ConversionResult.Success(result);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }

        }

        internal bool IsTableExtension(ModelBindingData bindingData)
        {
            if (bindingData?.Source is not Constants.TablesExtensionName)
            {
                _logger.LogTrace("Source '{source}' is not supported by {converter}", bindingData?.Source, nameof(TableConverter));
                return false;
            }

            return true;
        }

        public virtual async Task<object?> ConvertModelBindingDataAsync(IDictionary<string, object> content, Type targetType, ModelBindingData bindingData)
        {
            content.TryGetValue(Constants.Connection, out var connection);
            content.TryGetValue(Constants.TableName, out var tableName);
            content.TryGetValue(Constants.PartitionKey, out var partitionKey);
            content.TryGetValue(Constants.RowKey, out var rowKey);


            if (string.IsNullOrEmpty(tableName?.ToString()))
            {
                throw new ArgumentNullException("'TableName' cannot be null or empty");
            }

            return await ToTargetTypeAsync(targetType, connection?.ToString(), tableName.ToString(), partitionKey?.ToString(), rowKey?.ToString());
        }

        internal Dictionary<string, object> GetBindingDataContent(ModelBindingData bindingData)
        {
            return bindingData?.ContentType switch
            {
                Constants.JsonContentType => new Dictionary<string, object>(bindingData?.Content?.ToObjectFromJson<Dictionary<string, object>>(), StringComparer.OrdinalIgnoreCase),
                _ => throw new NotSupportedException($"Unexpected content-type. Currently only '{Constants.JsonContentType}' is supported.")
            };
        }

        internal virtual async Task<object?> ToTargetTypeAsync(Type targetType, string connection, string tableName, string partitionKey, string rowKey) => targetType switch
        {
            Type _ when targetType == typeof(TableClient) => GetTableClient(connection, tableName),
            Type _ when targetType == typeof(TableEntity) => GetTableEntity(connection, tableName, partitionKey, rowKey),
            _ => null
        };

        internal object? ToTargetTypeCollection(IEnumerable<object> tableCollection, string methodName, Type type)
        {
            tableCollection = tableCollection.Select(b => Convert.ChangeType(b, type));
            MethodInfo method = typeof(TableConverter).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo genericMethod = method.MakeGenericMethod(type);

            return genericMethod.Invoke(null, new[] { tableCollection.ToList() });
        }

        internal virtual TableClient GetTableClient(string connection, string tableName)
        {
            var tableOptions = _tableOptions.Get(connection);
            TableServiceClient tableServiceClient = tableOptions.CreateClient();
            return tableServiceClient.GetTableClient(tableName);
        }

        internal virtual TableEntity GetTableEntity(string connection, string tableName, string partitionKey, string rowKey)
        {
            var tableOptions = _tableOptions.Get(connection);
            TableServiceClient tableServiceClient = tableOptions.CreateClient();
            var tableClient = tableServiceClient.GetTableClient(tableName);
            return tableClient.GetEntity<TableEntity>(partitionKey, rowKey);
        }

        internal static IEnumerable<T> CloneToEnumerable<T>(IEnumerable<object> source)
        {
            return source.Cast<T>();
        }
    }
}

