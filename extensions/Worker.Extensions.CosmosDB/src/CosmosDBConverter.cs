// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind CosmosDB type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    internal class CosmosDBConverter : IInputConverter
    {
        private readonly IOptionsMonitor<CosmosDBBindingOptions> _cosmosOptions;
        private readonly ILogger<CosmosDBConverter> _logger;
        private static readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

        public CosmosDBConverter(IOptionsMonitor<CosmosDBBindingOptions> cosmosOptions, ILogger<CosmosDBConverter> logger)
        {
            _cosmosOptions = cosmosOptions ?? throw new ArgumentNullException(nameof(cosmosOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            return context?.Source switch
            {
                ModelBindingData binding => await ConvertFromBindingDataAsync(context, binding),
                _ => ConversionResult.Unhandled(),
            };
        }

        private async ValueTask<ConversionResult> ConvertFromBindingDataAsync(ConverterContext context, ModelBindingData modelBindingData)
        {
            try
            {
                if (modelBindingData.Source is not Constants.CosmosExtensionName)
                {
                    throw new InvalidBindingSourceException(modelBindingData.Source, Constants.CosmosExtensionName);
                }

                var cosmosAttribute = GetBindingDataContent(modelBindingData);
                object result = await ToTargetTypeAsync(context.TargetType, cosmosAttribute);

                return ConversionResult.Success(result);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private CosmosDBInputAttribute GetBindingDataContent(ModelBindingData bindingData)
        {
            if (bindingData is null)
            {
                throw new ArgumentNullException(nameof(bindingData));
            }

            return bindingData.ContentType switch
            {
                Constants.JsonContentType => bindingData.Content.ToObjectFromJson<CosmosDBInputAttribute>(),
                _ => throw new InvalidContentTypeException(bindingData.ContentType, Constants.JsonContentType)
            };
        }

        private async Task<object> ToTargetTypeAsync(Type targetType, CosmosDBInputAttribute cosmosAttribute) => targetType switch
        {
            Type _ when targetType == typeof(CosmosClient) => CreateCosmosClient<CosmosClient>(cosmosAttribute),
            Type _ when targetType == typeof(Database) => CreateCosmosClient<Database>(cosmosAttribute),
            Type _ when targetType == typeof(Container) => CreateCosmosClient<Container>(cosmosAttribute),
            _ => ConvertObject(await CreateTargetObjectAsync(targetType, cosmosAttribute), targetType)
        };

        private object ConvertObject(object data, Type targetType)
        {
            var json = JsonSerializer.Serialize(data);
            return JsonSerializer.Deserialize(json, targetType, _serializerOptions)!;
        }

        private async Task<object> CreateTargetObjectAsync(Type targetType, CosmosDBInputAttribute cosmosAttribute)
        {
            var container = CreateCosmosClient<Container>(cosmosAttribute) as Container;

            if (container is null)
            {
                throw new InvalidOperationException($"Unable to create Cosmos container client for '{cosmosAttribute.ContainerName}'.");
            }

            if (targetType.GenericTypeArguments.Any())
            {
                return await CreatePOCOCollectionAsync(container, cosmosAttribute);
            }
            else
            {
                return await CreatePOCOAsync(container, cosmosAttribute);
            }
        }

        private async Task<Stream> CreatePOCOAsync(Container container, CosmosDBInputAttribute cosmosAttribute)
        {
            if (String.IsNullOrEmpty(cosmosAttribute.Id) || String.IsNullOrEmpty(cosmosAttribute.PartitionKey))
            {
                throw new InvalidOperationException("The 'Id' and 'PartitionKey' properties of a CosmosDB single-item input binding cannot be null or empty.");
            }

            ResponseMessage item = await container.ReadItemStreamAsync(cosmosAttribute.Id, new PartitionKey(cosmosAttribute.PartitionKey));

            if (item is null || item.StatusCode is not System.Net.HttpStatusCode.OK || item.Content is null)
            {
                throw new InvalidOperationException($"Unable to retrieve document with ID '{cosmosAttribute.Id}' and PartitionKey '{cosmosAttribute.PartitionKey}'");
            }

            return item.Content;
        }

        private async Task<IList<Stream>> CreatePOCOCollectionAsync(Container container, CosmosDBInputAttribute cosmosAttribute)
        {
            QueryDefinition queryDefinition = null!;
            if (!String.IsNullOrEmpty(cosmosAttribute.SqlQuery))
            {
                queryDefinition = new QueryDefinition(cosmosAttribute.SqlQuery);
                if (cosmosAttribute.SqlQueryParameters?.Count() > 0)
                {
                    foreach (var parameter in cosmosAttribute.SqlQueryParameters)
                    {
                        queryDefinition.WithParameter(parameter.Key, parameter.Value.ToString());
                    }
                }
            }

            QueryRequestOptions queryRequestOptions = new();
            if (!String.IsNullOrEmpty(cosmosAttribute.PartitionKey))
            {
                var partitionKey = new PartitionKey(cosmosAttribute.PartitionKey);
                queryRequestOptions = new() { PartitionKey = partitionKey };
            }

            using (var iterator = container.GetItemQueryStreamIterator(queryDefinition: queryDefinition, requestOptions: queryRequestOptions))
            {
                if (iterator is null)
                {
                    throw new InvalidOperationException($"Unable to retrieve documents for container '{container.Id}'.");
                }

                return await ExtractCosmosDocumentsAsync(iterator);
            }
        }

        private async Task<IList<Stream>> ExtractCosmosDocumentsAsync(FeedIterator iterator)
        {
            var documentList = new List<Stream>();
            while (iterator.HasMoreResults)
            {
                ResponseMessage response = await iterator.ReadNextAsync();
                documentList.Add(response.Content);
            }
            return documentList;
        }

        private T CreateCosmosClient<T>(CosmosDBInputAttribute cosmosAttribute)
        {
            if (cosmosAttribute is null)
            {
                throw new ArgumentNullException(nameof(cosmosAttribute));
            }

            var cosmosDBOptions = _cosmosOptions.Get(cosmosAttribute.Connection);
            CosmosClient cosmosClient = cosmosDBOptions.GetClient(cosmosAttribute.PreferredLocations!);

            Type targetType = typeof(T);
            object cosmosReference = targetType switch
            {
                Type _ when targetType == typeof(Database) => cosmosClient.GetDatabase(cosmosAttribute.DatabaseName),
                Type _ when targetType == typeof(Container) => cosmosClient.GetContainer(cosmosAttribute.DatabaseName, cosmosAttribute.ContainerName),
                _ => cosmosClient
            };

            return (T)cosmosReference;
        }
    }
}
