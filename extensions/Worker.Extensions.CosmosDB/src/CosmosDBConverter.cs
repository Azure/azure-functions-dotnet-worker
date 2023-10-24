﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
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
            _ => await CreateTargetObjectAsync(targetType, cosmosAttribute)
        };

        private async Task<object> CreateTargetObjectAsync(Type targetType, CosmosDBInputAttribute cosmosAttribute)
        {
            var container = CreateCosmosClient<Container>(cosmosAttribute) as Container;

            if (container is null)
            {
                throw new InvalidOperationException($"Unable to create Cosmos container client for '{cosmosAttribute.ContainerName}'.");
            }

            if (targetType.TryGetCollectionElementType(out Type? elementType))
            {
                if (elementType is null)
                {
                    throw new ArgumentNullException(nameof(elementType));
                }

                if (targetType.IsConcreteType() && !targetType.IsArray)
                {
                    return await CreatePocoListConcreteAsync(container, cosmosAttribute, elementType, targetType);
                }

                var listResult = await CreatePocoListAsync(container, cosmosAttribute, elementType, targetType);
                if (targetType.IsArray)
                {
                    var arrayResult = Array.CreateInstance(elementType, listResult.Count);
                    listResult.CopyTo(arrayResult, 0);
                    return arrayResult;
                }

                return listResult;
            }
            else
            {
                return await CreatePocoAsync(container, cosmosAttribute, targetType);
            }
        }

        private async Task<object> CreatePocoAsync(Container container, CosmosDBInputAttribute cosmosAttribute, Type targetType)
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

            return await JsonSerializer.DeserializeAsync(item.Content, targetType, _serializerOptions)!;
        }

        private async Task<object> CreatePocoListConcreteAsync(Container container, CosmosDBInputAttribute cosmosAttribute, Type elementType, Type targetType)
        {
            var result = Activator.CreateInstance(targetType);
            var addMethod = targetType.GetMethod("Add")
                ?? throw new InvalidOperationException($"Unable to find 'Add' method on type '{targetType.Name}'.");

            await foreach (var item in GetObjectsAsTargetTypeAsync(container, cosmosAttribute, elementType))
            {
                addMethod.Invoke(result, new[] { item });
            }

            return result;
        }

        private async Task<IList> CreatePocoListAsync(Container container, CosmosDBInputAttribute cosmosAttribute, Type elementType, Type targetType)
        {
            var resultType = typeof(List<>).MakeGenericType(elementType);
            var result = (IList)Activator.CreateInstance(resultType);

            await foreach (var item in GetObjectsAsTargetTypeAsync(container, cosmosAttribute, elementType))
            {
                result.Add(item);
            }

            return result;
        }

        private bool IsSupportedCollectionType(Type targetType)
        {
            if (targetType is null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            return targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IList<>);
        }

        private async IAsyncEnumerable<object> GetObjectsAsTargetTypeAsync(Container container, CosmosDBInputAttribute cosmosAttribute, Type target)
        {
            await foreach (var stream in this.GetCollectionStreamAsync(container, cosmosAttribute))
            {
                yield return await JsonSerializer.DeserializeAsync(stream, target, _serializerOptions)!;
            }
        }

        private async IAsyncEnumerable<Stream> GetCollectionStreamAsync(Container container, CosmosDBInputAttribute cosmosAttribute)
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

            using var iterator = container.GetItemQueryStreamIterator(queryDefinition: queryDefinition, requestOptions: queryRequestOptions)
                ?? throw new InvalidOperationException($"Unable to retrieve documents for container '{container.Id}'.");

            while (iterator.HasMoreResults)
            {
                ResponseMessage response = await iterator.ReadNextAsync();
                yield return response.Content;
            }
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
