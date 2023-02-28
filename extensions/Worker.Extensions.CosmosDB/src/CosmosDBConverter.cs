// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Converters;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind Cosmos DB type parameters.
    /// </summary>
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
                CollectionModelBindingData binding => await ConvertFromCollectionBindingDataAsync(context, binding), // we don't have cardinality so this never hits
                _ => ConversionResult.Unhandled(),
            };
        }

        internal virtual async ValueTask<ConversionResult> ConvertFromBindingDataAsync(ConverterContext context, ModelBindingData modelBindingData)
        {
            if (!IsCosmosExtension(modelBindingData))
            {
                return ConversionResult.Unhandled();
            }

            try
            {
                Type elementType = context.TargetType.IsArray ? context.TargetType.GetElementType() : context.TargetType.GenericTypeArguments[0];
                var cosmosAttribute = GetBindingDataContent(modelBindingData);
                object result = await ToTargetType(context.TargetType, cosmosAttribute);

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
            var cosmosCollection = new List<object>(collectionModelBindingData.ModelBindingDataArray.Length);

            if (collectionModelBindingData.ModelBindingDataArray.Any(bindingData => !IsCosmosExtension(bindingData)))
            {
                return ConversionResult.Unhandled();
            }

            try
            {
                foreach (ModelBindingData modelBindingData in collectionModelBindingData.ModelBindingDataArray)
                {
                    var cosmosAttribute = GetBindingDataContent(modelBindingData);
                    var cosmosItem = await ToTargetType(context.TargetType, cosmosAttribute);
                    if (cosmosItem is not null)
                    {
                        cosmosCollection.Add(cosmosItem);
                    }
                }

                if (cosmosCollection is not null && cosmosCollection is { Count: > 0 })
                {
                    return ConversionResult.Success(cosmosCollection);
                }
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }

            return ConversionResult.Unhandled();
        }

        internal bool IsCosmosExtension(ModelBindingData bindingData)
        {
            if (bindingData?.Source is not Constants.CosmosExtensionName)
            {
                _logger.LogTrace("Source '{source}' is not supported by {converter}", bindingData?.Source, nameof(CosmosDBConverter));
                return false;
            }

            return true;
        }

        internal CosmosDBInputAttribute GetBindingDataContent(ModelBindingData bindingData)
        {
            return bindingData?.ContentType switch
            {
                Constants.JsonContentType => bindingData.Content.ToObjectFromJson<CosmosDBInputAttribute>(),
                _ => throw new NotSupportedException($"Unexpected content-type. Currently only {Constants.JsonContentType} is supported.")
            };
        }

        private async Task<object> ToTargetType(Type targetType, CosmosDBInputAttribute cosmosAttribute) => targetType switch
        {
            Type _ when targetType == typeof(CosmosClient) => CreateCosmosClient<CosmosClient>(cosmosAttribute),
            Type _ when targetType == typeof(Database) => CreateCosmosClient<Database>(cosmosAttribute),
            Type _ when targetType == typeof(Container) => CreateCosmosClient<Container>(cosmosAttribute),
            _ => await CreateTargetObject(targetType, cosmosAttribute)
        };

        private async Task<object> CreateTargetObject(Type targetType, CosmosDBInputAttribute cosmosAttribute)
        {
            if (targetType.GenericTypeArguments.Any())
            {
                targetType = targetType.GenericTypeArguments.FirstOrDefault();
            }

            MethodInfo createPOCOFromReferenceMethod = GetType()
                                                        .GetMethod(nameof(CreatePOCOFromReference), BindingFlags.Instance | BindingFlags.NonPublic)
                                                        .MakeGenericMethod(new Type[] { targetType });

            return await (Task<object>)createPOCOFromReferenceMethod.Invoke(this, new object[] { cosmosAttribute });
        }

        // This will be for input bindings only.
        // a) If users bind to just a POCO, they need to provide the `Id` and `PartitionKey`
        //     attributes so that we know which document to pull
        // b) If they bind to IList<POCO>, we should be able to just pull every document
        //    in the container, unless they specify the the SqlQuery attribute, in which case
        //    we need to filter on that.
        private async Task<object> CreatePOCOFromReference<T>(CosmosDBInputAttribute cosmosAttribute)
        {
            var container = CreateCosmosClient<Container>(cosmosAttribute) as Container;

            if (container is null)
            {
                // use proper exception type or handle
                throw new InvalidOperationException("Houston, we have a problem");
            }

            var partitionKey = cosmosAttribute.PartitionKey == null ? PartitionKey.None : new PartitionKey(cosmosAttribute.PartitionKey);

            if (cosmosAttribute.Id is not null)
            {
                ItemResponse<T> item = await container.ReadItemAsync<T>(cosmosAttribute.Id, partitionKey);

                if (item is null || item?.StatusCode is not System.Net.HttpStatusCode.OK)
                {
                    throw new InvalidOperationException($"Unable to retrieve document with ID {cosmosAttribute.Id} and PartitionKey {cosmosAttribute.PartitionKey}");
                }

                return item.Resource;
            }

            QueryDefinition queryDefinition = null;
            if (cosmosAttribute.SqlQuery is not null)
            {
                queryDefinition = new QueryDefinition(cosmosAttribute.SqlQuery);
                if (cosmosAttribute.SqlQueryParameters != null)
                {
                    // TODO: fix SqlQueryParameters being empty
                    foreach (var parameter in cosmosAttribute.SqlQueryParameters)
                    {
                        queryDefinition.WithParameter(parameter.Item1, parameter.Item2);
                    }
                }
            }

            QueryRequestOptions queryRequestOptions = new() { PartitionKey = partitionKey };
            using (var iterator = container.GetItemQueryIterator<T>(queryDefinition: queryDefinition, requestOptions: queryRequestOptions))
            {
                return await ExtractCosmosDocuments(iterator);
            }
        }

        private async Task<IList<T>> ExtractCosmosDocuments<T>(FeedIterator<T> iterator)
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
            CosmosClientOptions cosmosClientOptions = new() { ApplicationPreferredRegions = Utilities.ParsePreferredLocations(cosmosAttribute.PreferredLocations) };
            CosmosClient cosmosClient = cosmosDBOptions.CreateClient(cosmosClientOptions);

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
