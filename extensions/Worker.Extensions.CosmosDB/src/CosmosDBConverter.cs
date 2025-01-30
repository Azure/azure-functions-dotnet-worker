// Copyright (c) .NET Foundation. All rights reserved.
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
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

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
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return ConversionResult.Success(null);
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
            if (CreateCosmosClient<Container>(cosmosAttribute) is not Container container)
            {
                throw new InvalidOperationException($"Unable to create Cosmos container client for '{cosmosAttribute.ContainerName}'.");
            }

            if (targetType.IsCollectionType())
            {
                return await ParameterBinder.BindCollectionAsync(
                    elementType => GetDocumentsAsync(container, cosmosAttribute, elementType), targetType);
            }
            else
            {
                return await CreatePocoAsync(container, cosmosAttribute, targetType);
            }
        }

        private async Task<object> CreatePocoAsync(Container container, CosmosDBInputAttribute cosmosAttribute, Type targetType)
        {
            if (string.IsNullOrEmpty(cosmosAttribute.Id) || string.IsNullOrEmpty(cosmosAttribute.PartitionKey))
            {
                throw new InvalidOperationException("The 'Id' and 'PartitionKey' properties of a CosmosDB single-item input binding cannot be null or empty.");
            }

            ResponseMessage item = await container.ReadItemStreamAsync(cosmosAttribute.Id, new PartitionKey(cosmosAttribute.PartitionKey));
            item.EnsureSuccessStatusCode();
            return (await JsonSerializer.DeserializeAsync(item.Content, targetType, _serializerOptions))!;
        }

        private async IAsyncEnumerable<object> GetDocumentsAsync(Container container, CosmosDBInputAttribute cosmosAttribute, Type elementType)
        {
            await foreach (var stream in GetDocumentsStreamAsync(container, cosmosAttribute))
            {
                // Cosmos returns a stream of JSON which represents a paged response. The contents are in a property called "Documents".
                // Deserializing into CosmosStream<T> will extract these documents.
                Type target = typeof(CosmosStream<>).MakeGenericType(elementType);
                CosmosStream page = (CosmosStream)(await JsonSerializer.DeserializeAsync(stream!, target, _serializerOptions))!;
                foreach (var item in page.GetDocuments())
                {
                    yield return item;
                }
            }
        }

        private async IAsyncEnumerable<Stream> GetDocumentsStreamAsync(Container container, CosmosDBInputAttribute cosmosAttribute)
        {
            QueryDefinition queryDefinition = null!;
            if (!string.IsNullOrEmpty(cosmosAttribute.SqlQuery))
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
            if (!string.IsNullOrEmpty(cosmosAttribute.PartitionKey))
            {
                var partitionKey = new PartitionKey(cosmosAttribute.PartitionKey);
                queryRequestOptions = new() { PartitionKey = partitionKey };
            }

            using FeedIterator iterator = container.GetItemQueryStreamIterator(queryDefinition: queryDefinition, requestOptions: queryRequestOptions)
                ?? throw new InvalidOperationException($"Unable to retrieve documents for container '{container.Id}'.");

            while (iterator.HasMoreResults)
            {
                using ResponseMessage response = await iterator.ReadNextAsync();
                response.EnsureSuccessStatusCode();
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

        // Need a non-generic type to cast to, and can't use IEnumerable directly (breaks json deserialization).
        private abstract class CosmosStream
        {
            public abstract IEnumerable GetDocuments();
        }

        private class CosmosStream<T> : CosmosStream
        {
            public IEnumerable<T>? Documents { get; set; }

            public override IEnumerable GetDocuments() => Documents!;
        }
    }
}
