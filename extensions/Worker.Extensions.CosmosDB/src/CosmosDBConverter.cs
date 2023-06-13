// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private readonly IOptionsSnapshot<CosmosDBBindingOptions> _cosmosOptions;
        private readonly ILogger<CosmosDBConverter> _logger;

        public CosmosDBConverter(IOptionsSnapshot<CosmosDBBindingOptions> cosmosOptions, ILogger<CosmosDBConverter> logger)
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
            _ => await CreateTargetObjectAsync(targetType, cosmosAttribute)
        };

        private async Task<object> CreateTargetObjectAsync(Type targetType, CosmosDBInputAttribute cosmosAttribute)
        {
            MethodInfo createPOCOMethod;

            if (targetType.GenericTypeArguments.Any())
            {
                targetType = targetType.GenericTypeArguments.FirstOrDefault();

                createPOCOMethod = GetType()
                                    .GetMethod(nameof(CreatePOCOCollectionAsync), BindingFlags.Instance | BindingFlags.NonPublic)
                                    .MakeGenericMethod(targetType);
            }
            else
            {
                createPOCOMethod = GetType()
                                    .GetMethod(nameof(CreatePOCOAsync), BindingFlags.Instance | BindingFlags.NonPublic)
                                    .MakeGenericMethod(targetType);
            }


            var container = CreateCosmosClient<Container>(cosmosAttribute) as Container;

            if (container is null)
            {
                throw new InvalidOperationException($"Unable to create Cosmos container client for '{cosmosAttribute.ContainerName}'.");
            }

            return await (Task<object>)createPOCOMethod.Invoke(this, new object[] { container, cosmosAttribute });
        }

        private async Task<object> CreatePOCOAsync<T>(Container container, CosmosDBInputAttribute cosmosAttribute)
        {
            if (String.IsNullOrEmpty(cosmosAttribute.Id) || String.IsNullOrEmpty(cosmosAttribute.PartitionKey))
            {
                throw new InvalidOperationException("The 'Id' and 'PartitionKey' properties of a CosmosDB single-item input binding cannot be null or empty.");
            }

            ItemResponse<T> item = await container.ReadItemAsync<T>(cosmosAttribute.Id, new PartitionKey(cosmosAttribute.PartitionKey));

            if (item is null || item?.StatusCode is not System.Net.HttpStatusCode.OK || item.Resource is null)
            {
                throw new InvalidOperationException($"Unable to retrieve document with ID '{cosmosAttribute.Id}' and PartitionKey '{cosmosAttribute.PartitionKey}'");
            }

            return item.Resource;
        }

        private async Task<object> CreatePOCOCollectionAsync<T>(Container container, CosmosDBInputAttribute cosmosAttribute)
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
                var partitionKey =  new PartitionKey(cosmosAttribute.PartitionKey);
                queryRequestOptions = new() { PartitionKey = partitionKey };
            }

            using (var iterator = container.GetItemQueryIterator<T>(queryDefinition: queryDefinition, requestOptions: queryRequestOptions))
            {
                if (iterator is null)
                {
                    throw new InvalidOperationException($"Unable to retrieve documents for container '{container.Id}'.");
                }

                return await ExtractCosmosDocumentsAsync(iterator);
            }
        }

        private async Task<IList<T>> ExtractCosmosDocumentsAsync<T>(FeedIterator<T> iterator)
        {
            var documentList = new List<T>();
            while (iterator.HasMoreResults)
            {
                FeedResponse<T> response = await iterator.ReadNextAsync();
                documentList.AddRange(response.Resource);
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
