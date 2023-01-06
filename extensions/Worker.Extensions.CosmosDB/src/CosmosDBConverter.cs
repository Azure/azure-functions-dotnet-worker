// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Converters;
using System.Collections.Generic;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind Blob Storage type parameters.
    /// </summary>
    internal class CosmosDBConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.Source is not ModelBindingData ||
                context.Source is not CollectionModelBindingData)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            if (context.Source.Source is not Constants.ExtensionName)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            try
            {
                object result = context.Source switch
                {
                    ModelBindingData => ToTargetType(context.TargetType, context.Source.Content),
                    CollectionModelBindingData => ToTargetTypeCollection(context.TargetType, context.Source),
                    _ => null
                };

                if (result is not null)
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(result));
                }
            }
            catch (Exception ex)
            {
                // TODO: DeserializeObject could throw
                Console.WriteLine(ex);
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        // v4: CosmosDatabase instead of Database and CosmosContainer instead of Container
        private object? ToTargetType(Type targetType, BinaryData content) => targetType switch
        {
            Type _ when targetType == typeof(CosmosClient)        => CreateCosmosReference<CosmosClient>(content),
            Type _ when targetType == typeof(Database)            => CreateCosmosReference<Database>(content),
            Type _ when targetType == typeof(Container)           => CreateCosmosReference<Container>(content),
            _ => content.ToObjectFromJson(targetType)
        };

        private IEnumerable<object> ToTargetTypeCollection(ConverterContext context, Type targetType, CollectionModelBindingData collectionModelBindingData)
        {
            var collectionBlob = new List<object>(collectionModelBindingData.ModelBindingDataArray.Length);

            foreach (ModelBindingData modelBindingData in collectionModelBindingData.ModelBindingDataArray)
            {
                var element = ToTargetType(targetType, modelBindingData.Content);
                if (element != null)
                {
                    collectionBlob.Add(element);
                }
            }

            return collectionBlob;
        }

        private object CreateCosmosReference<T>(BinaryData content)
        {
            var cosmosAttribute = content.ToObjectFromJson<CosmosDBInputAttribute>();
            var connectionString = Environment.GetEnvironmentVariable(cosmosAttribute?.Connection);

            CosmosClient cosmosClient;

            if (connectionString is null)
            {
                var tenantId = Environment.GetEnvironmentVariable(Constants.ConnectionTenantId);
                var clientId = Environment.GetEnvironmentVariable(Constants.ConnectionClientId);
                var clientSecret = Environment.GetEnvironmentVariable(Constants.ConnectionClientSecret);
                var accountName = Environment.GetEnvironmentVariable(Constants.ConnectionAccountName);

                TokenCredential credential = !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(tenantId)
                                                ? new ClientSecretCredential(tenantId, clientId, clientSecret)
                                                : new ChainedTokenCredential(new ManagedIdentityCredential(clientId), new ManagedIdentityCredential());


                cosmosClient = new($"https://{accountName}.documents.azure.com:443/", credential);
            }
            else
            {
                cosmosClient = new(connectionString);
            }


            Type targetType = typeof(T);
            object cosmosReference = targetType switch {
                Type _ when targetType == typeof(Database)    => cosmosClient.GetDatabase(cosmosAttribute?.DatabaseName),
                Type _ when targetType == typeof(Container)   => cosmosClient.GetContainer(cosmosAttribute?.DatabaseName, cosmosAttribute?.ContainerName),
                _ => cosmosClient
            };

            return (T)cosmosReference;
        }
    }
}
