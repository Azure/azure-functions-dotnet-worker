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

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind Cosmos DB type parameters.
    /// </summary>
    internal class CosmosDBConverter : IInputConverter
    {
        private readonly ICosmosDBServiceFactory _cosmosDBServiceFactory;

        public CosmosDBConverter(ICosmosDBServiceFactory cosmosDBServiceFactory)
        {
            _cosmosDBServiceFactory = cosmosDBServiceFactory ?? throw new ArgumentNullException(nameof(cosmosDBServiceFactory));
        }

        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.Source is ModelBindingData modelBindingData)
            {
                if (modelBindingData.Source is not Constants.ExtensionName)
                {
                    return ConversionResult.Unhandled();
                }

                try
                {
                    var cosmosAttribute = modelBindingData.Content.ToObjectFromJson<CosmosDBInputAttribute>();
                    object result = await ToTargetType(context.TargetType, cosmosAttribute);

                    if (result is not null)
                    {
                        return ConversionResult.Success(result);
                    }
                }
                catch (Exception ex)
                {
                    // What do we want to do for error handling?
                    Console.WriteLine(ex);

                    if (ex is CosmosException docEx)
                    {
                        throw;
                    }
                }
            }

            if (context.Source is CollectionModelBindingData collectionModelBindingData)
            {
                if (collectionModelBindingData.ModelBindingDataArray.Any(x => x.Source is Constants.ExtensionName))
                {
                    try
                    {
                        var collectionResult = await ToTargetTypeCollection(context, context.TargetType, collectionModelBindingData);

                        if (collectionResult is not null && collectionResult is { Count: > 0 })
                        {
                            return ConversionResult.Success(collectionResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: DeserializeObject could throw
                        Console.WriteLine(ex);
                    }
                }
            }

            return ConversionResult.Unhandled();
        }

        private async Task<object> ToTargetType(Type targetType, CosmosDBInputAttribute cosmosAttribute) => targetType switch
        {
            Type _ when targetType == typeof(CosmosClient) => CreateCosmosClient<CosmosClient>(cosmosAttribute),
            Type _ when targetType == typeof(Database) => CreateCosmosClient<Database>(cosmosAttribute),
            Type _ when targetType == typeof(Container) => CreateCosmosClient<Container>(cosmosAttribute),
            _ => await CreateTargetObject(targetType, cosmosAttribute)
        };

        private async Task<List<object>> ToTargetTypeCollection(ConverterContext context, Type targetType, CollectionModelBindingData collectionModelBindingData)
        {
            var collectionCosmosItems = new List<object>(collectionModelBindingData.ModelBindingDataArray.Length);

            foreach (ModelBindingData modelBindingData in collectionModelBindingData.ModelBindingDataArray)
            {
                var cosmosAttribute = modelBindingData.Content.ToObjectFromJson<CosmosDBInputAttribute>();
                var cosmosItem = await ToTargetType(targetType, cosmosAttribute);
                if (cosmosItem is not null)
                {
                    collectionCosmosItems.Add(cosmosItem);
                }
            }

            return collectionCosmosItems;
        }

        private async Task<object> CreateTargetObject(Type targetType, CosmosDBInputAttribute cosmosAttribute)
        {
            // if target type is a collection and NOT of type IList, should we handle this early
            // and let users know we only support IList types?

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
                // What do?
                throw new InvalidOperationException("Cosmos attribute cannot be null");
            }

            CosmosClientOptions cosmosClientOptions = new() { ApplicationPreferredRegions = Utilities.ParsePreferredLocations(cosmosAttribute.PreferredLocations) };
            CosmosClient cosmosClient = _cosmosDBServiceFactory.CreateService(cosmosAttribute.Connection, cosmosClientOptions);

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
